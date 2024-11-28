using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Isam.Esent.Interop;

namespace ditjson
{
    internal static class NtdsSchema
    {
        internal static void ExportSchema(Session session, JET_DBID dbid, List<string> tables)
        {
            var csv = GenerateSchemaCsv(session, dbid, tables);

            File.WriteAllText("schema.csv", csv);
        }

        private static string GenerateSchemaCsv(Session session, JET_DBID dbid, List<string> tables)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Table,Column Name,Column Type,Is Multivalue");

            // Get all tables and export
            foreach (var table in tables)
            {
                NtdsSchema.PopulateCsv(session, dbid, table, sb);
            }
            return sb.ToString();
        }

        /// <summary>
        ///     Populates the <see cref="StringBuilder" /> instance with column data.
        /// </summary>
        /// <param name="session">
        ///     ESENT Session
        /// </param>
        /// <param name="dbid">
        ///     Handle to the database
        /// </param>
        /// <returns>
        ///     A <see cref="List{Dictionary{string, object}}" /> containing table data
        /// </returns>
        /// <exception cref="NtdsException">
        /// </exception>
        /// <exception cref="FormatException">
        /// </exception>
        /// <exception cref="OverflowException">
        /// </exception>
        private static void PopulateCsv(Session session, JET_DBID dbid, string tableName, StringBuilder sb)
        {
            var columns = new List<ColumnInfo>(Api.GetTableColumns(session, dbid, tableName));

            using var table = new Table(session, dbid, tableName, OpenTableGrbit.ReadOnly);
            Api.JetSetTableSequential(session, table, SetTableSequentialGrbit.None);
            Api.MoveBeforeFirst(session, table);

            while (Api.TryMoveNext(session, table))
            {
                foreach (var column in columns)
                {
                    sb
                        .Append(table.Name).Append(',')
                        .Append(column.Name).Append(',')
                        .Append(column.Coltyp).Append(',')
                        .AppendLine(column.Grbit.HasFlag(ColumndefGrbit.ColumnMultiValued).ToString());
                }
            }

            Api.JetResetTableSequential(session, table, ResetTableSequentialGrbit.None);
        }
    }
}