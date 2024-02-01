using System.Collections.Generic;
using CommandLine;

namespace ditjson
{
    /// <summary>
    /// Internal class used for the command line parsing
    /// </summary>
    internal class Options
    {
        [Option('n', "ntds", Required = true, Default = "", HelpText = "Path to ntds.dit file")]
        public string Ntds { get; set; }

        [Option('t', "tables", Required = false, Default = "", HelpText = "ntds.dit tables to include. Default: datatable, link_table")]
        public IEnumerable<string> Tables { get; set; }
    }
}
