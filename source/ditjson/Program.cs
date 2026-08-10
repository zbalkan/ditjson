using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using CommandLine;
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

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Options))]
        [RequiresUnreferencedCode("Calls ditjson.Program.RunOptions(Options)")]
        public static int Main(string[] args)
        {
            args = args.Select(arg => arg switch
            {
                "-h" => "--help",
                "-v" => "--version",
                _ => arg
            }).ToArray();

            var parser = new Parser(settings =>
            {
                // stdout is reserved for the JSON document.
                settings.HelpWriter = Console.Error;
                settings.AutoVersion = true;
            });

            return parser.ParseArguments<Options>(args)
                .MapResult(RunOptions, errors =>
                    errors.Any(error => error is HelpRequestedError or VersionRequestedError) ? 0 : 2);
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
                Console.Error.WriteLine($"ditjson: extraction failed: {ex.Message}");
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
            Api.JetSetSystemParameter(JET_INSTANCE.Nil, JET_SESID.Nil, JET_param.DatabasePageSize, 8192, null);

            using var instance = new Instance("ditjson");
            instance.Parameters.Recovery = false;
            instance.Init();

            using var session = new Session(instance);
            Api.JetAttachDatabase(session, opts.Ntds, AttachDatabaseGrbit.ReadOnly);
            Api.JetOpenDatabase(session, opts.Ntds, null, out var dbid, OpenDatabaseGrbit.ReadOnly);

            Console.Error.WriteLine("[*] Extracting structured objects (users, groups, computers)...");
            var selectedTables = FilterTables(session, dbid);
            var filterOptions = new ObjectFilter.FilterOptions
            {
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
                    PasswordHashDecryptor.DecryptPasswordHashes(session, dbid, users, computers, opts.System);
                    PasswordHistoryExtractor.ExtractPasswordHistory(session, dbid, users, bootkey);
                    SupplementalCredentialsParser.ParseSupplementalCredentials(session, dbid, users, computers);
                }
            }

            return JsonOutputFormatter.FormatStructuredOutput(users, groups, computers);
        }

        private static List<string> FilterTables(Session session, JET_DBID dbid)
        {
            var tablesInDb = new HashSet<string>(Api.GetTableNames(session, dbid), StringComparer.Ordinal);
            var selected = new List<string>();
            foreach (var table in StructuredTables)
            {
                if (tablesInDb.Contains(table))
                    selected.Add(table);
            }

            return selected;
        }
    }
}
