ditjson
========

# Background

`ditjson` is a fork of [dumpntds](https://github.com/bsi-group/dumpntds). Unlike the original tool, the purpose it to generate JSON files in order to help integration with other tools. 

This fork updates the underlying framework to .NET 8.0 and uses NuGet packages rather than deploying dependencies as a part of repository. It is possible to publish as a single-file. The trimmed version's size is around 20MB.

The output is a single-file JSON export. The JSON export is opinionated and ignores null values to minimize the exported JSON file size.

**N.B.** Thanks to dotnet 8.0, it should run cross-platform. But it has not been tested on Linux and MacOS platforms. 

# Usage

## Export JSON

Extract the ntds.dit file from the host and run using the following:

```
ditjson -n path\to\ntds.dit\file
```

This will process only `datatable` and `link_table`.

Once the process has been completed it will have generated two output files in the application directory:

- ntds.json


## Export JSON  for specific tables

Extract the ntds.dit file from the host and run using the following:

```
ditjson -n path\to\ntds.dit\file -t datatable
```

This will process only `datatable`.


## Export JSON  for *all* tables

Extract the ntds.dit file from the host and run using the following:

```
ditjson -n path\to\ntds.dit\file -t *
```

This will process all tables inside `ntds.dit` file. Beware of the size.


# Dependencies

- [Commandline](https://github.com/commandlineparser/commandline)
- [ManagedEsent](https://github.com/microsoft/ManagedEsent)