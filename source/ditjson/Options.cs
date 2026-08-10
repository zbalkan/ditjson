using CommandLine;

namespace ditjson
{
    /// <summary>
    ///     Inputs and output destination for a complete NTDS extraction.
    /// </summary>
    internal sealed class Options
    {
        [Value(0, MetaName = "ntds.dit", Required = true, HelpText = "Path to the NTDS.dit database")]
        public string Ntds { get; set; } = string.Empty;

        [Value(1, MetaName = "SYSTEM", Required = false, HelpText = "Optional matching SYSTEM registry hive")]
        public string? System { get; set; }

        [Option('o', "output", Required = false, HelpText = "Write JSON to a file instead of stdout")]
        public string? Output { get; set; }
    }
}
