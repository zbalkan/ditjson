using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using CommandLine;
using Microsoft.Isam.Esent.Interop;

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