ditjson
========

# Background

`ditjson` is a fork of [dumpntds](https://github.com/bsi-group/dumpntds). Unlike the original tool, the purpose it to generate JSON files in order to help integration with other tools. 

This fork updates the underlying framework to .NET 8.0 and uses NuGet packages rather than deploying dependencies as a part of repository. It is possible to publish as a single-file. The trimmed version's size is around 20MB.

The output is a single-file JSON export. The JSON export is opinionated and ignores null values to minimize the exported JSON file size.

**N.B.** Thanks to dotnet 8.0, it should run cross-platform. But it has not been tested on Linux and MacOS platforms. 

# Usage

```bash
ditjson 1.0.0+cda090ef84d50d5f2829883b84d77e9f3b0e64f6
Copyright © Info-Assure 2015, © Zafer Balkan 2023

  -n, --ntds      Required. (Default: ) Path to ntds.dit file

  -t, --tables    (Default: datatable link_table) ntds.dit tables to include.

  -s, --schema    (Default: false) Export schema from ntds.dit file. When provided, -t parameter is ignored.

  --help          Display this help screen.

  --version       Display version information.
```

## Export JSON

Extract the ntds.dit file from the host and run using the following:

```
ditjson -n path\to\ntds.dit\file
```

This will process only `datatable` and `link_table`.

Once the process has been completed it will have generated two output files in the application directory:

- ntds.json


## Export JSON for specific tables

Extract the ntds.dit file from the host and run using the following:

```
ditjson -n path\to\ntds.dit\file -t datatable link_table sd_table
```

This will process `datatable`, `link_table` and `sd_table`. Mind that it is space-separated.


## Export JSON for *all* tables

Extract the ntds.dit file from the host and run using the following:

```
ditjson -n path\to\ntds.dit\file -t *
```

This will process all tables inside `ntds.dit` file. Beware of the size.


## Export NTDS schema as CSV

Extract the schema of ntds.dit file from the host and run using the following:

```
ditjson -n path\to\ntds.dit\file -e
```

This will process all tables inside `ntds.dit` file and export columns: `Table`, `Column Name`, `Column Type`, `Is Multivalue`


# Dependencies

- [Commandline](https://github.com/commandlineparser/commandline)
- [ManagedEsent](https://github.com/microsoft/ManagedEsent)