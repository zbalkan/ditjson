using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Isam.Esent.Interop;

namespace ditjson
{
    internal static class NtdsData
    {
        private static readonly JsonSerializerOptions options = new()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.Strict,
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        internal static string TablesToJson(Session session, JET_DBID dbid, List<string> tables)
        {
            var ntdsDictionary = new Dictionary<string, object>();
            foreach (var table in tables)
            {
                ntdsDictionary.Add(table, TableToList(session, dbid, table));
            }

            string json;
            try
            {
                json = JsonSerializer.Serialize(ntdsDictionary, options);
            }
            catch (NotSupportedException ex)
            {
                throw new NtdsException("Failed to serialize to JSON.", ex);
            }

            return json;
        }

        /// <summary>
        ///     Export table as a <see cref="List{Dictionary{string, object}}" />
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
        internal static List<IDictionary<string, object>> TableToList(Session session, JET_DBID dbid, string tableName)
        {
            var linktableValues = new List<IDictionary<string, object>>();
            var columns = new List<ColumnInfo>(Api.GetTableColumns(session, dbid, tableName));

            using (var table = new Table(session, dbid, tableName, OpenTableGrbit.ReadOnly))
            {
                Api.JetSetTableSequential(session, table, SetTableSequentialGrbit.None);
                Api.MoveBeforeFirst(session, table);

                var columnids = Api.GetColumnDictionary(session, table);
                var formattedData = string.Empty;
                while (Api.TryMoveNext(session, table))
                {
                    var obj = new Dictionary<string, object>();
                    foreach (var column in columns)
                    {
                        var test = columnids[column.Name];
                        formattedData = NtdsDataTypes.GetFormattedValue(session, table, column);
                        var cellValue = formattedData;

                        // Ignore emptry or null values
                        if (!string.IsNullOrEmpty(cellValue))
                        {
                            if (NtdsMetadata.AttributeMapping.TryGetValue(column.Name, out var mappedValue))
                            {
                                obj.Add(mappedValue, cellValue);
                            }
                            else
                            {
                                obj.Add(column.Name, cellValue);
                            }
                        }
                    }

                    linktableValues.Add(obj);
                }

                Api.JetResetTableSequential(session, table, ResetTableSequentialGrbit.None);
            }

            return linktableValues;
        }
    }
}