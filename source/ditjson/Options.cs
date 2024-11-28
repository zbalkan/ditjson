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
    }
}