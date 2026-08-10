# ditjson - NTDS.dit to JSON Converter

A comprehensive tool for extracting and converting Active Directory data from NTDS.dit (Active Directory database) into structured JSON format with support for relationship mapping, password hash decryption, and credential extraction.

## Features

### Core Capabilities
- **Structured Object Extraction**: Extract users, groups, and computers with 45+ decoded properties
- **Binary Field Decoding**: Automatic conversion of Windows-format data:
  - FILETIME timestamps to ISO8601 UTC
  - Binary SIDs to S-1-5-... format
  - Binary GUIDs to standard UUID format
  - Bit-flag fields (UserAccountControl, SAM account types)
- **Relationship Mapping**: Extract group memberships and member lists from link_table
- **Data Filtering**: Granular control over exported data with 6 filtering options
- **Early Data Reduction**: Minimize JSON file size by dropping unnecessary data during extraction

### Advanced Credential Extraction
- **Password Hash Decryption**: Decrypt NT and LM hashes using SYSTEM registry hive bootkey
- **Password History**: Extract and decrypt historical password hashes
- **Supplemental Credentials**: Parse Kerberos keys (DES, AES128, AES256, RC4-HMAC) and cleartext passwords
- **BitLocker Recovery Keys**: Extract volume recovery passwords

### Quality Assurance
- **59 Unit Tests**: Comprehensive test coverage for decoders, filters, and cryptography
- **CI/CD Workflows**: Automated testing on PR/push and manual release pipeline
- **Multi-Platform Release**: Build and release for Linux, Windows, and macOS

## Installation

### Requirements
- .NET 8.0 or later
- NTDS.dit file (Active Directory database)
- Optional: SYSTEM registry hive file (for hash decryption)

### Build from Source
```bash
git clone https://github.com/zbalkan/ditjson.git
cd ditjson
dotnet build source/ditjson.sln --configuration Release
```

### Publish Self-Contained Binary
```bash
# Linux
dotnet publish source/ditjson/ditjson.csproj -c Release -r linux-x64 --self-contained

# Windows
dotnet publish source/ditjson/ditjson.csproj -c Release -r win-x64 --self-contained

# macOS
dotnet publish source/ditjson/ditjson.csproj -c Release -r osx-x64 --self-contained
```

## Usage

### Basic Usage
Extract all users, groups, and computers to JSON:
```bash
ditjson --ntds /path/to/ntds.dit --structured
```

**Output**: `ntds.json` with metadata and all objects

### Example 1: Exclude Disabled and Deleted Accounts
```bash
ditjson --ntds /path/to/ntds.dit --structured \
  --exclude-disabled \
  --exclude-locked
```

**Output**: Only active, enabled accounts in JSON

### Example 2: Exclude Computer Objects
```bash
ditjson --ntds /path/to/ntds.dit --structured \
  --exclude-computers \
  --exclude-groups
```

**Output**: User accounts only, no computers or groups

### Example 3: Include Empty Collections
```bash
ditjson --ntds /path/to/ntds.dit --structured \
  --include-empty-collections
```

**Output**: All objects with empty groups, ancestors, and member lists included

### Example 4: Full Credential Extraction
Extract everything including password hashes and Kerberos keys:
```bash
ditjson --ntds /path/to/ntds.dit --structured \
  --system-hive /path/to/SYSTEM \
  --extract-hashes \
  --extract-history \
  --extract-supplemental
```

**Output**: Complete NTDS dump with:
- Decrypted NT and LM password hashes
- Historical password hashes (previous passwords)
- Kerberos keys for each algorithm
- Cleartext passwords (if stored)

### Example 5: Advanced Filtering and Credential Extraction
```bash
ditjson --ntds /path/to/ntds.dit --structured \
  --exclude-disabled \
  --exclude-locked \
  --exclude-computers \
  --system-hive /path/to/SYSTEM \
  --extract-hashes \
  --extract-supplemental
```

**Output**: Active users only with decrypted passwords and Kerberos keys

## JSON Output Format

### Metadata Section
```json
{
  "metadata": {
    "exportDate": "2024-07-22T15:30:00Z",
    "ditjsonVersion": "1.0.2",
    "totalUsers": 250,
    "totalGroups": 45,
    "totalComputers": 120
  },
  "users": [...],
  "groups": [...],
  "computers": [...]
}
```

### User Object (Decoded Fields)
```json
{
  "recordId": 1234,
  "name": "John Doe",
  "objectClass": "Person",
  "objectGuid": "550e8400-e29b-41d4-a716-446655440000",
  "objectSid": "S-1-5-21-3623811015-3361044348-30300510-1234",
  "samAccountName": "jdoe",
  "userPrincipalName": "jdoe@example.com",
  "sAmAccountType": "SAM_USER_OBJECT",
  "userAccountControl": ["SCRIPT", "ACCOUNTDISABLE", "DONT_EXPIRE_PASSWORD"],
  "primaryGroupId": 513,
  "logonCount": 42,
  "badPwdCount": 0,
  "lastLogon": "2024-07-20T10:30:00Z",
  "passwordLastSet": "2024-01-15T08:45:00Z",
  "accountExpires": null,
  "dialInAccessPermission": 0,
  "whenCreated": "2020-03-10T14:22:00Z",
  "whenChanged": "2024-07-22T09:15:00Z",
  "isDeleted": false,
  
  "passwordHashes": {
    "ntHash": "8846F7EAEE8FB117AD06BDD830B7586C",
    "lmHash": "AAD3B435B51404EEAAD3B435B51404EE"
  },
  
  "passwordHistory": [
    "5D41402ABC4B2A76B9719D911017C592",
    "6512BD43D9CAA6E02C990B0A82652DCA",
    "C20AD4D76FE97759AA27A0C99BFF6710"
  ],
  
  "supplementalCredentials": {
    "clearTextPassword": "P@ssw0rd123!",
    "kerberosKeys": [
      {
        "algorithm": "AES256_CTS_HMAC_SHA1_96",
        "key": "8846F7EAEE8FB117AD06BDD830B7586C1234567890ABCDEF1234567890ABCDEF"
      },
      {
        "algorithm": "RC4_HMAC_MD5",
        "key": "8846F7EAEE8FB117AD06BDD830B7586C"
      }
    ]
  },
  
  "ancestors": [
    {
      "recordId": 100,
      "name": "Domain Admins",
      "objectGuid": "550e8400-e29b-41d4-a716-446655440001",
      "objectSid": "S-1-5-21-3623811015-3361044348-30300510-512"
    }
  ],
  
  "memberOf": [
    {
      "recordId": 200,
      "name": "IT Staff",
      "objectGuid": "550e8400-e29b-41d4-a716-446655440002",
      "objectSid": "S-1-5-21-3623811015-3361044348-30300510-1100",
      "isPrimaryGroup": false,
      "deletedTime": null
    }
  ]
}
```

### Group Object
```json
{
  "recordId": 200,
  "name": "IT Staff",
  "objectClass": "Group",
  "objectGuid": "550e8400-e29b-41d4-a716-446655440002",
  "objectSid": "S-1-5-21-3623811015-3361044348-30300510-1100",
  "samAccountName": "itstaff",
  "groupType": "GROUP_TYPE_SECURITY_GLOBAL",
  "whenCreated": "2020-06-15T10:00:00Z",
  "whenChanged": "2024-07-22T09:00:00Z",
  "isDeleted": false,
  
  "members": [
    {
      "recordId": 1234,
      "name": "John Doe",
      "objectGuid": "550e8400-e29b-41d4-a716-446655440000",
      "objectClass": "Person",
      "isPrimaryGroup": false,
      "deletedTime": null
    },
    {
      "recordId": 1235,
      "name": "Jane Smith",
      "objectGuid": "550e8400-e29b-41d4-a716-446655440003",
      "objectClass": "Person",
      "isPrimaryGroup": false,
      "deletedTime": null
    }
  ]
}
```

### Computer Object
```json
{
  "recordId": 5000,
  "name": "WORKSTATION01",
  "objectClass": "Computer",
  "objectGuid": "550e8400-e29b-41d4-a716-446655445000",
  "objectSid": "S-1-5-21-3623811015-3361044348-30300510-5000",
  "samAccountName": "WORKSTATION01$",
  "dnsHostName": "workstation01.example.com",
  "operatingSystem": "Windows 11 Enterprise",
  "operatingSystemVersion": "10.0 (22631)",
  "passwordLastSet": "2024-07-10T14:30:00Z",
  "dialInAccessPermission": 0,
  "whenCreated": "2023-01-20T09:00:00Z",
  "whenChanged": "2024-07-22T08:30:00Z",
  "isDeleted": false,
  
  "passwordHashes": {
    "ntHash": "AAD3B435B51404EEAAD3B435B51404EE",
    "lmHash": null
  },
  
  "memberOf": [
    {
      "recordId": 300,
      "name": "Workstations",
      "objectGuid": "550e8400-e29b-41d4-a716-446655440004",
      "objectSid": "S-1-5-21-3623811015-3361044348-30300510-1200",
      "isPrimaryGroup": false,
      "deletedTime": null
    }
  ]
}
```

## Command-Line Options

```
-n, --ntds              Path to ntds.dit file (required)
-t, --tables            Tables to extract (default: datatable, link_table, sd_table)
-s, --schema            Export schema only
--structured            Export structured objects (users, groups, computers)
--include-deleted       Include deleted objects
--exclude-disabled      Exclude disabled user accounts
--exclude-locked        Exclude locked out user accounts
--exclude-computers     Exclude computer objects
--exclude-groups        Exclude group objects
--include-empty-collections  Include empty collections in output
--system-hive           Path to SYSTEM registry hive for hash decryption
--extract-hashes        Decrypt and extract password hashes
--extract-history       Extract password history
--extract-supplemental  Extract supplemental credentials (Kerberos keys)
```

## Data Extraction Flow

1. **Read NTDS.dit** - Opens ESENT database with ManagedEsent library
2. **Extract Objects** - Iterates through datatable, classifies by objectClass
3. **Decode Fields** - Converts binary formats (FILETIME, SID, GUID, flags)
4. **Extract Relationships** - Reads link_table for group memberships
5. **Apply Filters** - Removes objects based on filter flags
6. **Clean Fields** - Normalizes values (null strings, negative integers)
7. **Extract Credentials** (optional):
   - Extract bootkey from SYSTEM registry hive
   - Decrypt NT/LM password hashes using RC4
   - Parse password history
   - Parse supplemental credentials blob
8. **Serialize to JSON** - Outputs with metadata and null value exclusion

## Decoding Details

### FILETIME Conversion
- Converts Windows 64-bit ticks (100-nanosecond intervals since 1601-01-01)
- Outputs ISO8601 UTC format
- Special cases: 0 = never set, max value = never expires

### SID Conversion
- Parses binary SID structure (revision + authority + sub-authorities)
- Outputs standard S-1-5-21-... format
- Uses Windows SecurityIdentifier class for validation

### GUID Conversion
- Converts 16-byte binary GUID to standard UUID format
- Outputs with hyphens: XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX

### UAC Flags
Decodes 19 account control flags:
- SCRIPT, ACCOUNTDISABLE, HOMEDIR_REQUIRED, LOCKOUT
- PASSWD_NOTREQD, PASSWD_CANT_CHANGE, ENCRYPTED_TEXT_PWD_ALLOWED
- TEMP_DUPLICATE_ACCOUNT, NORMAL_ACCOUNT, INTERDOMAIN_TRUST_ACCOUNT
- WORKSTATION_TRUST_ACCOUNT, SERVER_TRUST_ACCOUNT, DONT_EXPIRE_PASSWORD
- SMARTCARD_REQUIRED, TRUSTED_FOR_DELEGATION, NOT_DELEGATED
- USE_DES_KEY_ONLY, DONT_REQ_PREAUTH, PREAUTH_REQUIRED

### SAM Account Types
- `SAM_USER_OBJECT`: Regular user account
- `SAM_MACHINE_ACCOUNT`: Computer account
- `SAM_TRUST_ACCOUNT`: Domain trust account
- `SAM_GROUP_OBJECT`: Security group

## Unit Tests

Run tests:
```bash
dotnet test source/ditjson.Tests/ditjson.Tests.csproj
```

**Test Coverage** (59 passing tests):
- Timestamp decoding
- SID parsing
- GUID conversion
- Flag decoding (UAC, SAM types, groups)
- Field cleaning and normalization
- Object filtering and cleanup
- RC4 cryptography

## CI/CD Pipeline

### Automated Workflows
- **test.yml**: Unit tests on every PR and commit to main/master
- **build.yml**: Build verification and code analysis
- **release.yml**: Manual release workflow (workflow_dispatch)

### Release Process
Trigger release via GitHub CLI:
```bash
gh workflow run release.yml -f version=1.0.3
```

Creates releases for:
- Linux x64
- Windows x64
- macOS x64

## Architecture

### Core Components
- **Models**: NtdsObject base class with User, Group, Computer subclasses
- **Decoders**: Specialized classes for FILETIME, SID, GUID, and flag conversions
- **Extractors**: Per-object-type extractors with relationship and credential support
- **Filters**: Early data reduction with FieldCleaner and ObjectFilter
- **Cryptography**: RC4 cipher for hash and credential decryption
- **Output**: JSON formatter with metadata and null value optimization

### Namespace Organization
```
ditjson/
├── Models/              Object definitions
├── Decoders/            Binary format conversions
├── Extractors/          Data extraction and decryption
├── Filtering/           Data filtering and cleaning
├── Output/              JSON serialization
└── Program.cs           CLI entry point
```

## Performance Considerations

- **Early Filtering**: Filters applied during extraction, not after serialization
- **Streaming**: Records processed one at a time to minimize memory usage
- **Lazy Evaluation**: Relationships only extracted if link_table is included
- **Null Value Exclusion**: JSON excludes null fields to minimize file size

## Security Notes

- **Hash Decryption**: Requires access to both ntds.dit and SYSTEM registry hive
- **Cleartext Passwords**: Extraction of cleartext passwords requires valid supplemental credentials
- **Registry Access**: SYSTEM hive reading via file I/O (no registry API calls)
- **RC4 Cipher**: Standard implementation used in Windows for NTDS encryption

## Troubleshooting

### File Not Found Errors
- Ensure ntds.dit path is correct and accessible
- Verify SYSTEM hive path if using hash decryption

### Null Reference Exceptions
- Gracefully handled per record; extraction continues on individual errors
- Check output logs for "[!] Error processing record" messages

### Decryption Failures
- Verify SYSTEM hive is from the same domain
- Bootkey extraction only works with valid SYSTEM files
- Password hashes may not decrypt if historical keys are unavailable

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make changes with accompanying tests
4. Run full test suite: `dotnet test source/ditjson.Tests/ditjson.Tests.csproj`
5. Submit pull request

## License

Copyright © Info-Assure 2015, © Zafer Balkan 2023-2024

## References

- ntdsxtract - Original Python implementation reference
- ManagedEsent - .NET ESENT database access
- System.Security.Principal - Windows SID handling
- RFC 2630 - Cryptographic Message Syntax (for supplemental credentials)

## Version History

### 1.0.2 (Current)
- Full implementation of Phases 1-3
- Unit tests with 59 passing tests
- CI/CD workflows (test, build, release)
- Comprehensive documentation

### Features Added in This Release
- Structured JSON extraction with 45+ decoded properties per user
- Relationship mapping from link_table
- Password hash decryption with bootkey extraction
- Password history extraction
- Supplemental credentials parsing (Kerberos keys)
- 6 filtering options for data reduction
- Multi-platform release binaries
- Automated testing on PR/push
