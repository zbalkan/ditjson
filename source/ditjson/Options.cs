using System.Collections.Generic;
using CommandLine;

namespace ditjson
{
    /// <summary>
    ///     Internal class used for the command line parsing
    /// </summary>
    internal class Options
    {
        [Option('n', "ntds", Required = true, Default = "", HelpText = "Path to ntds.dit file")]
        public string Ntds { get; set; }

        [Option('t', "tables", Required = false, Default = new[] { "datatable", "link_table", "sd_table" }, HelpText = "ntds.dit tables to include.")]
        public IEnumerable<string> Tables { get; set; }

        [Option('s', "schema", Required = false, Default = false, HelpText = "Export schema from ntds.dit file. When provided, -t parameter is ignored.")]
        public bool Schema { get; set; }

        [Option("structured", Required = false, Default = false, HelpText = "Export structured objects (users, groups, computers) with decoded fields instead of raw table export.")]
        public bool Structured { get; set; }

        [Option("include-deleted", Required = false, Default = false, HelpText = "Include deleted objects in export.")]
        public bool IncludeDeleted { get; set; }

        [Option("exclude-disabled", Required = false, Default = false, HelpText = "Exclude disabled user accounts from export.")]
        public bool ExcludeDisabled { get; set; }

        [Option("exclude-locked", Required = false, Default = false, HelpText = "Exclude locked out user accounts from export.")]
        public bool ExcludeLockedOut { get; set; }

        [Option("exclude-computers", Required = false, Default = false, HelpText = "Exclude computer objects from export.")]
        public bool ExcludeComputers { get; set; }

        [Option("exclude-groups", Required = false, Default = false, HelpText = "Exclude group objects from export.")]
        public bool ExcludeGroups { get; set; }

        [Option("include-empty-collections", Required = false, Default = false, HelpText = "Include empty collections in output.")]
        public bool IncludeEmptyCollections { get; set; }

        [Option("system-hive", Required = false, Default = "", HelpText = "Path to SYSTEM registry hive file for hash decryption.")]
        public string SystemHive { get; set; }

        [Option("extract-hashes", Required = false, Default = false, HelpText = "Extract and decrypt password hashes (requires --system-hive).")]
        public bool ExtractHashes { get; set; }

        [Option("extract-history", Required = false, Default = false, HelpText = "Extract password history (requires --system-hive).")]
        public bool ExtractHistory { get; set; }

        [Option("extract-supplemental", Required = false, Default = false, HelpText = "Extract supplemental credentials and Kerberos keys.")]
        public bool ExtractSupplemental { get; set; }
    }
}