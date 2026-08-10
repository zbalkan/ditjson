namespace ditjson
{
    /// <summary>
    ///     Inputs and output destination for a complete NTDS extraction.
    /// </summary>
    internal sealed class Options
    {
        public string Ntds { get; set; } = string.Empty;

        public string? Output { get; set; }

        public string? System { get; set; }

        public bool Timeline { get; set; }
    }
}
