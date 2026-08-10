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

            foreach (var tableName in tables)
            {
                var columns = new List<ColumnInfo>(Api.GetTableColumns(session, dbid, tableName));
                foreach (var column in columns)
                {
                    sb
                        .Append(tableName).Append(',')
                        .Append(column.Name).Append(',')
                        .Append(column.Coltyp).Append(',')
                        .AppendLine(column.Grbit.HasFlag(ColumndefGrbit.ColumnMultiValued).ToString());
                }
            }
            return sb.ToString();
        }
    }
}
