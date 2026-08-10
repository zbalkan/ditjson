using System;
using System.IO;

namespace ditjson
{
    internal enum CliParseResult
    {
        Success,
        Help,
        Version,
        Error
    }

    internal static class CliArguments
    {
        internal static CliParseResult Parse(string[] args, out Options? options, out string? error)
        {
            options = null;
            error = null;

            var parsed = new Options();
            var ntdsSet = false;
            var systemSet = false;

            for (var index = 0; index < args.Length; index++)
            {
                var arg = args[index];

                if (string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase))
                {
                    return CliParseResult.Help;
                }

                if (string.Equals(arg, "-v", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, "--version", StringComparison.OrdinalIgnoreCase))
                {
                    return CliParseResult.Version;
                }

                if (string.Equals(arg, "-t", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, "--timeline", StringComparison.OrdinalIgnoreCase))
                {
                    parsed.Timeline = true;
                    continue;
                }

                if (string.Equals(arg, "-o", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, "--output", StringComparison.OrdinalIgnoreCase))
                {
                    if (index + 1 >= args.Length)
                    {
                        error = "Missing value for --output.";
                        return CliParseResult.Error;
                    }

                    parsed.Output = args[++index];
                    continue;
                }

                if (arg.StartsWith("--output=", StringComparison.OrdinalIgnoreCase))
                {
                    parsed.Output = arg["--output=".Length..];
                    continue;
                }

                if (arg.StartsWith("-o=", StringComparison.OrdinalIgnoreCase))
                {
                    parsed.Output = arg["-o=".Length..];
                    continue;
                }

                if (arg.StartsWith("-", StringComparison.Ordinal))
                {
                    error = $"Unknown option: {arg}";
                    return CliParseResult.Error;
                }

                if (!ntdsSet)
                {
                    parsed.Ntds = arg;
                    ntdsSet = true;
                    continue;
                }

                if (!systemSet)
                {
                    parsed.System = arg;
                    systemSet = true;
                    continue;
                }

                error = $"Unexpected argument: {arg}";
                return CliParseResult.Error;
            }

            if (!ntdsSet)
            {
                error = "The NTDS.dit path is required.";
                return CliParseResult.Error;
            }

            options = parsed;
            return CliParseResult.Success;
        }

        internal static void WriteHelp(TextWriter writer)
        {
            writer.WriteLine("Usage: ditjson [options] <ntds.dit> [SYSTEM]");
            writer.WriteLine();
            writer.WriteLine("Options:");
            writer.WriteLine("  -o, --output <file>   Write JSON to a file instead of stdout");
            writer.WriteLine("  -t, --timeline        Write a chronological JSON timeline instead of structured objects");
            writer.WriteLine("  -h, --help            Show help and exit");
            writer.WriteLine("  -v, --version         Show version and exit");
        }

        internal static void WriteVersion(TextWriter writer)
        {
            writer.WriteLine($"ditjson {typeof(CliArguments).Assembly.GetName().Version}");
        }
    }
}
