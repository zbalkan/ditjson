# ditjson

`ditjson` extracts useful Active Directory information from an offline `NTDS.dit` database and writes it as JSON. It is Windows-only, since it relies on `Microsoft.Database.ManagedEsent` to read the native Windows ESE engine (`esent.dll`).

It is a single-tool replacement for the older multi-stage workflow: there are no Python post-processing scripts and no CSV or table exports as intermediate files. `ditjson` reads the ESE database, identifies directory objects and their relationships, and emits one structured JSON document.  A full raw database dump is also available when it is needed for investigation.

## Features

- Extracts users, groups, computers, decoded attributes, and relationships.
- Reads NTDS tagged values explicitly (including every stored user certificate) within a consistent read transaction.
- Produces one output format: JSON, either on standard output or in a file.
- Reimplements the relevant `ntdsxtract` processing in the executable itself.
- Decrypts NT and LM password hashes when the matching `SYSTEM` registry hive is supplied.
- Extracts password history and supported supplemental credentials, including Kerberos keys and recoverable cleartext passwords.
- Can produce either a structured directory export or a chronological event timeline.
- Can dump every table and column with `--all`.
- Writes progress and diagnostics to standard error, keeping standard output machine-readable.

> [!WARNING]
> `NTDS.dit`, the `SYSTEM` hive, and extracted JSON can contain reusable credentials and other sensitive directory data. Handle them as secrets and only use this tool on systems and data you are authorized to examine.

## Requirements

- Windows (ManagedEsent uses the Windows ESE API)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) only when building or running from source; release artifacts are self-contained
- An offline `NTDS.dit` database
- Its matching `SYSTEM` registry hive when credential decryption is required

## Build

```powershell
git clone https://github.com/zbalkan/ditjson.git
cd ditjson
dotnet build src/ditjson.slnx --configuration Release
```

## Usage

```text
Usage: ditjson [options] <ntds.dit> [SYSTEM]

Options:
  -o, --output <file>   Write JSON to a file instead of stdout
  -t, --timeline        Write a chronological JSON timeline instead of structured objects
      --all             Dump every table and column from NTDS.dit as JSON
  -h, --help            Show help and exit
  -v, --version         Show version and exit
```

`<ntds.dit>` is the required path to the offline directory database. The optional second positional argument, `[SYSTEM]`, is the path to its matching SYSTEM registry hive. Options may appear before, between, or after the positional arguments. Option names are case-insensitive, and the output option
also accepts `--output=<file>` and `-o=<file>`.

Help, version information, diagnostics, and progress are written to stderr so that stdout remains reserved for JSON. Run `ditjson --help` to display the command-line reference, or `ditjson --version` to display the installed version.

Extract directory information without decrypting credentials:

```powershell
ditjson C:\evidence\ntds.dit -o domain.json
```

Supply the matching hive to enable credential extraction:

```powershell
ditjson C:\evidence\ntds.dit C:\evidence\SYSTEM -o domain.json
```

Options can also precede the input paths:

```powershell
ditjson --output=domain.json C:\evidence\ntds.dit C:\evidence\SYSTEM
```

Write JSON to standard output for use in a pipeline:

```powershell
ditjson C:\evidence\ntds.dit C:\evidence\SYSTEM | jq .
```

Create a chronological timeline instead of the structured export:

```powershell
ditjson C:\evidence\ntds.dit --timeline -o timeline.json
```

Dump the complete ESE database, including every table and column, to a JSON file. The result retains the normal structured users, groups, computers, metadata, and credential enrichment, and adds a top-level `tables` object for the raw data. Raw binary values are encoded as lowercase hexadecimal strings, and raw column names are retained:

```powershell
ditjson C:\evidence\ntds.dit --all -o ntds-full.json
```

`--all` and `--timeline` are mutually exclusive. Supplying the optional matching SYSTEM hive enriches the structured users and computers in the full dump with decrypted hashes and other supported credentials. Values under `tables` remain an unmodified representation of the stored ESE data.

No object-selection switches are required. The structured export always extracts the supported users, groups, and computers; supplying `SYSTEM` enables the boot-key-dependent credential processing.

## Query user and computer names with hashes

Credential hashes require an `NTDS.dit` file and `SYSTEM` hive from the same system. The following `jq` query combines users and computers, shows their account names and recovered hashes, and omits accounts for which neither hash was recovered:

```powershell
ditjson C:\evidence\ntds.dit C:\evidence\SYSTEM |
  jq '(
    .users[] |
    {type: "user", name: (.SamAccountName // .Name), ntHash: .passwordHashes.ntHash, lmHash: .passwordHashes.lmHash}
  ), (
    .computers[] |
    {type: "computer", name: (.SamAccountName // .Name), ntHash: .passwordHashes.ntHash, lmHash: .passwordHashes.lmHash}
  ) | select(.ntHash != null or .lmHash != null)'
```

The same query can be run against a saved export without invoking `ditjson`:

```powershell
jq '(
  .users[] |
  {type: "user", name: (.SamAccountName // .Name), ntHash: .passwordHashes.ntHash, lmHash: .passwordHashes.lmHash}
), (
  .computers[] |
  {type: "computer", name: (.SamAccountName // .Name), ntHash: .passwordHashes.ntHash, lmHash: .passwordHashes.lmHash}
) | select(.ntHash != null or .lmHash != null)' domain.json
```

## Output

The default result is a single JSON object containing extraction metadata and collections of users, groups, and computers:

```json
{
  "metadata": {
    "database": {
      "attachTime": "2026-08-10T11:42:17.0000000Z",
      "consistentTime": "2026-08-10T11:41:53.0000000Z",
      "creationTime": "2024-02-06T09:15:31.0000000Z",
      "databaseTime": "0x00000000000A4E21",
      "detachTime": "2026-08-10T11:44:02.0000000Z",
      "fileFormatVersion": "0x00000620",
      "fileType": "0x00000001",
      "headerChecksum": "0x8A12BC34",
      "isDirty": false,
      "pageSize": 8192,
      "recoveryTime": "2026-08-10T11:43:48.0000000Z",
      "signature": "0x89ABCDEF",
      "windowsVersion": "10.0 (20348) Service Pack 0"
    },
    "ditjsonVersion": "2.0.0",
    "exportDate": "2026-08-10T12:00:00.0000000Z",
    "totalComputers": 120,
    "totalGroups": 45,
    "totalUsers": 250
  },
  "users": [],
  "groups": [],
  "computers": []
}
```

The `metadata.database` object is populated directly from the ESE database header. It records file identity and format values, page size, database state, the Windows version recorded in the header, and available creation, attach, detach, consistency, and recovery timestamps. Timestamp properties that are not present or valid in the header are omitted from the JSON.

The database contents determine which optional fields are present. Credential properties are populated only when the attributes exist, are supported, and can be decrypted. The password encryption keys are used internally and are not exported.

Without `--output`, stdout contains only the JSON document. Status messages, warnings, and errors go to stderr, so the two streams can be redirected independently:

```powershell
ditjson C:\evidence\ntds.dit C:\evidence\SYSTEM 2> extraction.log > domain.json
```

## Exit codes

- `0` — extraction completed successfully
- `1` — an input, extraction, or runtime error occurred
- `2` — command-line usage was invalid

Fatal errors do not write a partial JSON document to stdout.

## Development

Run the automated tests from the repository root:

```powershell
dotnet test src/ditjson.slnx
```

Guidance for validating credential extraction against a disposable Active Directory fixture is available in [`docs/testing.md`](docs/testing.md).

## Acknowledgments and history

`ditjson` is a fork and continuation of BSI's original [`dumpntds`](https://github.com/bsi-group/dumpntds) repository. The original project improved on full ESE exports by selecting the small subset of `datatable` columns required for analysis, then writing `datatable.csv` and `linktable.csv` for downstream Python scripts.

This fork removes those intermediate tables and scripts. It also recreates the relevant functionality of Csaba Barta's [`ntdsxtract`](https://github.com/csababarta/ntdsxtract), so object discovery, relationship parsing, and credential extraction happen directly in one tool with one JSON output. We gratefully acknowledge the authors and contributors of `dumpntds`, `ntdsxtract`, and [`libesedb`](https://github.com/libyal/libesedb), whose work established the original extraction workflow and foundations on which this project builds.
