using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using CommandLine;
using Microsoft.Isam.Esent.Interop;
using ditjson.Extractors;
using ditjson.Filtering;
using ditjson.Output;
using ditjson.Querying;

[assembly: InternalsVisibleTo("ditjson.Tests")]

namespace ditjson
{
    internal static class Program
    {
        /// <summary>
        ///     Application entry point
        /// </summary>
        /// <param name="args">
        /// </param>
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Options))]
        [RequiresUnreferencedCode("Calls ditjson.Program.RunOptions(Options)")]
        public static void Main(string[] args) => _ = Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunOptions)
                .WithNotParsed(HandleParseError);

        /// <summary>
        ///     Runs the incorrect parameter actions
        /// </summary>
        /// <param name="errs">
        /// </param>
        internal static void HandleParseError(IEnumerable<Error> errs) => Console.WriteLine("Check the parameters and retry.");

        /// <summary>
        ///     Runs the happy path code here.
        /// </summary>
        /// <param name="opts">
        ///     Parameters as Options
        /// </param>
        /// <exception cref="NtdsException">
        /// </exception>
        /// <exception cref="FormatException">
        /// </exception>
        /// <exception cref="OverflowException">
        /// </exception>
        [RequiresUnreferencedCode("Calls ditjson.Program.ExportJson(Session, JET_DBID)")]
        internal static void RunOptions(Options opts)
        {
            if (!File.Exists(opts.Ntds))
            {
                Console.WriteLine($"ntds.dit file does not exist in the path {opts.Ntds}");
                return;
            }

            Api.JetSetSystemParameter(JET_INSTANCE.Nil, JET_SESID.Nil, JET_param.DatabasePageSize, 8192, null);

            using var instance = new Instance("ditjson");
            instance.Parameters.Recovery = false;
            instance.Init();

            using var session = new Session(instance);
            Api.JetAttachDatabase(session, opts.Ntds, AttachDatabaseGrbit.ReadOnly);
            Api.JetOpenDatabase(session, opts.Ntds, null, out var dbid, OpenDatabaseGrbit.ReadOnly);

            if (opts.Schema)
            {
                var allTables = FilterTables(["*"], session, dbid);
                NtdsSchema.ExportSchema(session, dbid, allTables);
            }
            else if (opts.Structured)
            {
                Console.WriteLine("[*] Extracting structured objects (users, groups, computers)...");
                var selectedTables = FilterTables(opts.Tables, session, dbid);

                var filterOptions = new ObjectFilter.FilterOptions
                {
                    IncludeDeleted = opts.IncludeDeleted,
                    ExcludeDisabled = opts.ExcludeDisabled,
                    ExcludeLockedOut = opts.ExcludeLockedOut,
                    ExcludeComputers = opts.ExcludeComputers,
                    ExcludeGroups = opts.ExcludeGroups,
                    IncludeEmptyCollections = opts.IncludeEmptyCollections
                };

                var (users, groups, computers) = ObjectExtractor.ExtractStructuredObjects(session, dbid, selectedTables, filterOptions);

                Console.WriteLine($"[+] Extracted {users.Count} users, {groups.Count} groups, {computers.Count} computers");

                // Extract supplemental credentials if requested
                if (opts.ExtractSupplemental)
                {
                    SupplementalCredentialsParser.ParseSupplementalCredentials(session, dbid, users, computers);
                }

                // Extract password hashes and history if SYSTEM hive is provided
                if ((opts.ExtractHashes || opts.ExtractHistory) && !string.IsNullOrEmpty(opts.SystemHive))
                {
                    var bootkey = RegistryDecryptor.ExtractBootkey(opts.SystemHive);
                    if (bootkey != null && bootkey.Length > 0)
                    {
                        if (opts.ExtractHashes)
                        {
                            Console.WriteLine("[*] Extracting password hashes...");
                            PasswordHashDecryptor.DecryptPasswordHashes(session, dbid, users, computers, opts.SystemHive);
                        }

                        if (opts.ExtractHistory)
                        {
                            PasswordHistoryExtractor.ExtractPasswordHistory(session, dbid, users, bootkey);
                        }
                    }
                    else if (opts.ExtractHashes || opts.ExtractHistory)
                    {
                        Console.WriteLine("[!] Failed to extract bootkey from SYSTEM hive");
                    }
                }

                var json = JsonOutputFormatter.FormatStructuredOutput(users, groups, computers);

                // Filtering is opt-in so a successful extraction can never silently
                // produce an empty export merely because no crown-jewel query matched.
                if (ShouldApplyCrownJewels(opts))
                {
                    try
                    {
                        Console.WriteLine("[*] Applying crown jewels queries (optimized single-pass)...");
                        json = CrownJewelsFilterOptimized.ApplyCrownJewels(json);
                        Console.WriteLine("[+] Filtering complete");
                    }
                    catch (Exception ex)
                    {
                        throw new NtdsException("Crown jewels filtering failed. Use --all-data to skip filtering.", ex);
                    }
                }
                else if (!string.IsNullOrEmpty(opts.JqQuery))
                {
                    Console.WriteLine($"[!] Custom JQ queries not yet implemented. Use standard jq: ditjson ... | jq '{opts.JqQuery}'");
                }
                else
                {
                    Console.WriteLine("[*] Exporting all structured data without crown jewels filtering");
                }

                try
                {
                    File.WriteAllText("ntds.json", json);
                    Console.WriteLine("[+] Structured JSON export complete: ntds.json");
                }
                catch (Exception ex)
                {
                    throw new NtdsException("Failed to write to JSON to file.", ex);
                }
            }
            else
            {
                var selectedTables = FilterTables(opts.Tables, session, dbid);
                var json = NtdsData.TablesToJson(session, dbid, selectedTables);

                try
                {
                    File.WriteAllText("ntds.json", json);
                }
                catch (Exception ex)
                {
                    throw new NtdsException("Failed to write to JSON to file.", ex);
                }
            }
        }

        internal static bool ShouldApplyCrownJewels(Options opts) =>
            opts.CrownJewels && !opts.AllData && string.IsNullOrEmpty(opts.JqQuery);

        private static List<string> FilterTables(IEnumerable<string> tablesInOptions, Session session, JET_DBID dbid)
        {
            var tablesInDb = Api.GetTableNames(session, dbid);

            // If user asks all
            if (tablesInOptions.Count() == 1 && tablesInOptions.First().Equals("*", StringComparison.Ordinal))
            {
                return new List<string>(tablesInDb);
            }
            else
            {
                // if user asks oly specific tables
                return new List<string>(tablesInOptions.Where(t => tablesInDb.Contains(t)));
            }
        }
    }
}
