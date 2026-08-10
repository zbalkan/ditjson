using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using ditjson.Extractors;
using ditjson.Filtering;
using ditjson.Output;
using Microsoft.Isam.Esent.Interop;

[assembly: InternalsVisibleTo("ditjson.Tests")]

namespace ditjson
{
    internal static class Program
    {
        private static readonly string[] StructuredTables = ["datatable", "link_table", "sd_table"];

        public static int Main(string[] args)
        {
            switch (CliArguments.Parse(args, out var options, out var error))
            {
                case CliParseResult.Success:
                    return RunOptions(options!);
                case CliParseResult.Help:
                    CliArguments.WriteHelp(Console.Error);
                    return 0;
                case CliParseResult.Version:
                    CliArguments.WriteVersion(Console.Error);
                    return 0;
                default:
                    Console.Error.WriteLine($"ditjson: {error}");
                    CliArguments.WriteHelp(Console.Error);
                    return 2;
            }
        }

        internal static void ReportCredentialResults(List<Models.User> users,
            List<Models.Computer> computers)
        {
            var userHashes = users.Count(user => user.PasswordHashes?.NtHash != null ||
                                                 user.PasswordHashes?.LmHash != null);
            var computerHashes = computers.Count(computer => computer.PasswordHashes?.NtHash != null ||
                                                             computer.PasswordHashes?.LmHash != null);
            var historyHashes = users.Sum(user => (user.PasswordHistory?.Count ?? 0) +
                                                   (user.LmPasswordHistory?.Count ?? 0));
            var kerberosKeys = users.Sum(user => user.SupplementalCredentials?.KerberosKeys?.Count ?? 0) +
                               computers.Sum(computer =>
                                   computer.SupplementalCredentials?.KerberosKeys?.Count ?? 0);
            var clearTextPasswords = users.Count(user =>
                                         !string.IsNullOrEmpty(user.SupplementalCredentials?.ClearTextPassword)) +
                                     computers.Count(computer =>
                                         !string.IsNullOrEmpty(computer.SupplementalCredentials?.ClearTextPassword));

            Console.Error.WriteLine($"[+] Recovered credentials: {userHashes} user hash set(s), " +
                                    $"{computerHashes} computer hash set(s), {historyHashes} history hash(es), " +
                                    $"{kerberosKeys} Kerberos key(s), {clearTextPasswords} cleartext password(s)");
            Console.Error.WriteLine("[*] Password encryption keys are used internally and are not exported to JSON");

            if (userHashes + computerHashes == 0)
            {
                Console.Error.WriteLine("[!] No account password hashes were recovered; verify that the NTDS.dit " +
                                        "and SYSTEM hive are a matching pair and contain credential attributes");
            }
        }

        [RequiresUnreferencedCode("Calls ditjson.Output.JsonOutputFormatter.FormatStructuredOutput")]
        internal static int RunOptions(Options opts)
        {
            if (!File.Exists(opts.Ntds))
            {
                Console.Error.WriteLine($"ditjson: NTDS file not found: {opts.Ntds}");
                return 1;
            }

            if (!string.IsNullOrEmpty(opts.System) && !File.Exists(opts.System))
            {
                Console.Error.WriteLine($"ditjson: SYSTEM hive not found: {opts.System}");
                return 1;
            }

            try
            {
                var json = Extract(opts);
                WriteOutput(json, opts.Output);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ditjson: extraction failed: {ex.GetType().Name}: {ex.Message}");
                return 1;
            }
        }

        internal static void WriteOutput(string json, string? output)
        {
            if (string.IsNullOrEmpty(output))
            {
                Console.Out.Write(json);
                return;
            }

            File.WriteAllText(output, json);
            Console.Error.WriteLine($"[+] JSON export complete: {output}");
        }

        [RequiresUnreferencedCode("Calls ditjson.Output.JsonOutputFormatter.FormatStructuredOutput")]
        private static string Extract(Options opts)
        {
            var databaseMetadata = DatabaseFileMetadata.Read(opts.Ntds);
            Api.JetSetSystemParameter(JET_INSTANCE.Nil, JET_SESID.Nil, JET_param.DatabasePageSize, 8192, null);

            using var instance = new Instance("ditjson");
            instance.Parameters.Recovery = false;
            instance.Init();

            using var session = new Session(instance);
            Api.JetAttachDatabase(session, opts.Ntds, AttachDatabaseGrbit.ReadOnly);
            Api.JetOpenDatabase(session, opts.Ntds, null, out var dbid, OpenDatabaseGrbit.ReadOnly);

            Console.Error.WriteLine("[*] Extracting structured objects (users, groups, computers)...");
            var selectedTables = FilterTables(session, dbid);
            var filterOptions = new ObjectFilter.FilterOptions {
                IncludeDeleted = true,
                IncludeEmptyCollections = true
            };

            var (users, groups, computers) = ObjectExtractor.ExtractStructuredObjects(
                session, dbid, selectedTables, filterOptions);
            Console.Error.WriteLine($"[+] Extracted {users.Count} users, {groups.Count} groups, {computers.Count} computers");

            if (!string.IsNullOrEmpty(opts.System))
            {
                Console.Error.WriteLine("[*] Extracting boot-key-dependent credentials...");
                var bootkey = RegistryDecryptor.ExtractBootkey(opts.System);
                if (bootkey == null || bootkey.Length == 0)
                {
                    Console.Error.WriteLine("[!] Failed to extract boot key from SYSTEM hive");
                }
                else
                {
                    var peks = PekListExtractor.Extract(session, dbid, bootkey);
                    Console.Error.WriteLine($"[+] Decrypted {peks.Count} password encryption key(s)");
                    PasswordHashDecryptor.DecryptPasswordHashes(session, dbid, users, computers, peks);
                    PasswordHistoryExtractor.ExtractPasswordHistory(session, dbid, users, peks);
                    SupplementalCredentialsParser.ParseSupplementalCredentials(session, dbid, users, computers, peks);
                    ReportCredentialResults(users, computers);
                }
            }

            return opts.Timeline
                ? JsonOutputFormatter.FormatTimeline(users, groups, computers)
                : JsonOutputFormatter.FormatStructuredOutput(users, groups, computers, databaseMetadata);
        }

        private static List<string> FilterTables(Session session, JET_DBID dbid)
        {
            var tablesInDb = new HashSet<string>(Api.GetTableNames(session, dbid), StringComparer.Ordinal);
            return StructuredTables.Where(tablesInDb.Contains).ToList();
        }
    }
}
