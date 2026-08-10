# ditjson - NTDS.dit JSON extractor

`ditjson` extracts a complete, structured Active Directory dataset from an offline
`NTDS.dit` database. It decodes directory objects and relationships automatically;
when the matching `SYSTEM` registry hive is supplied, it also attempts every
supported boot-key-dependent credential extraction.

## Features

- Extracts users, computers, groups, decoded attributes, and relationships.
- Includes all available objects; filtering and analysis belong in downstream tools.
- Decrypts NT and LM hashes, password history, and supported supplemental
  credentials when a matching `SYSTEM` hive is available.
- Emits one structured JSON document suitable for redirection or pipelines.
- Keeps progress, warnings, and errors on standard error so standard output remains
  machine-readable.

## Requirements and installation

- .NET 8.0 or later
- An offline `NTDS.dit` database
- Optionally, its matching `SYSTEM` registry hive

Build from source:

```bash
git clone https://github.com/zbalkan/ditjson.git
cd ditjson
dotnet build source/ditjson.sln --configuration Release
```

## Usage

```text
ditjson <ntds.dit> [SYSTEM] [-o <file>]
```

Extract everything available without a boot key and write JSON to standard output:

```bash
ditjson ntds.dit
```

Supply the matching hive to enable all supported boot-key-dependent extraction:

```bash
ditjson ntds.dit SYSTEM
```

Write the JSON document to a named file instead of standard output:

```bash
ditjson ntds.dit SYSTEM -o domain.json
```

The complete public option set is:

```text
Arguments:
  ntds.dit            Path to the NTDS.dit database
  SYSTEM              Optional matching SYSTEM registry hive

Options:
  -o, --output <file> Write JSON to a file instead of stdout
  -h, --help          Show help
  -v, --version       Show version
```

No extraction switches are needed. The supplied input determines the available
capabilities, and structured extraction is always performed.

## Pipelines and diagnostics

Without `-o`, stdout contains only JSON. Operational messages are written to stderr,
so the result can be queried directly:

```bash
ditjson ntds.dit SYSTEM | jq '.users[]'
```

Redirection produces a clean JSON document while leaving diagnostics visible:

```bash
ditjson ntds.dit SYSTEM > domain.json
```

Diagnostics can be captured separately:

```bash
ditjson ntds.dit SYSTEM 2> extraction.log | jq .
```

Filtering is intentionally delegated to tools such as `jq` or DuckDB. For example:

```bash
ditjson ntds.dit SYSTEM |
  jq '.users[] | select(.samAccountName == "Administrator")'
```

## Output

The result is a single JSON object with metadata and collections for extracted
users, groups, and computers:

```json
{
  "metadata": {
    "exportDate": "2026-08-10T12:00:00Z",
    "ditjsonVersion": "1.0.2",
    "totalUsers": 250,
    "totalGroups": 45,
    "totalComputers": 120
  },
  "users": [],
  "groups": [],
  "computers": []
}
```

Credential properties are populated when they are present and can be interpreted.
A matching `SYSTEM` hive enables boot-key derivation, password hash decryption,
password-history extraction, and supplemental-credential parsing. Recoverable
record-level problems are reported to stderr without adding text to the JSON stream.

## Exit codes

- `0`: successful extraction
- `1`: extraction or runtime failure
- `2`: command-line usage error

Fatal errors do not emit a partial JSON document to stdout.

## Development

Run the test suite:

```bash
dotnet test source/ditjson.sln
```

The source is organized into models, decoders, extractors, filtering helpers, and
JSON output formatting under `source/ditjson`.
