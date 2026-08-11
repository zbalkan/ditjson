using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Isam.Esent.Interop;

namespace ditjson.Extractors
{
    internal static class ColumnExtractor
    {
        private const int FirstValueSequence = 1;

        internal static byte[]? GetBinary(Session session, JET_TABLEID table,
            IDictionary<string, JET_COLUMNID> columns, string fieldName) =>
            columns.TryGetValue(fieldName, out var columnId)
                ? Retrieve<BytesColumnValue>(session, table, columnId, fieldName).Value
                : null;

        internal static List<byte[]> GetBinaries(Session session, JET_TABLEID table,
            IDictionary<string, JET_COLUMNID> columns, string fieldName)
        {
            if (!columns.TryGetValue(fieldName, out var columnId))
            {
                return [];
            }

            var count = GetValueCount(session, table, columnId, fieldName);
            var values = new List<byte[]>(count);
            for (var index = 0; index < count; index++)
            {
                var sequence = index + FirstValueSequence;
                var value = Retrieve<BytesColumnValue>(session, table, columnId, fieldName, sequence).Value;
                if (value != null)
                {
                    values.Add(value);
                }
            }
            return values;
        }

        internal static int GetInt32(Session session, JET_TABLEID table,
            IDictionary<string, JET_COLUMNID> columns, string fieldName) =>
            columns.TryGetValue(fieldName, out var columnId)
                ? Retrieve<Int32ColumnValue>(session, table, columnId, fieldName).Value ?? 0
                : 0;

        internal static long GetInt64(Session session, JET_TABLEID table,
            IDictionary<string, JET_COLUMNID> columns, string fieldName) =>
            columns.TryGetValue(fieldName, out var columnId)
                ? Retrieve<Int64ColumnValue>(session, table, columnId, fieldName).Value ?? 0
                : 0;

        internal static int GetRecordId(Session session, JET_TABLEID table,
            IDictionary<string, JET_COLUMNID> columns, int fallback) =>
            columns.ContainsKey("DNT_col") ? GetInt32(session, table, columns, "DNT_col") : fallback;

        internal static string? GetString(Session session, JET_TABLEID table,
            IDictionary<string, JET_COLUMNID> columns, string fieldName)
        {
            if (!columns.TryGetValue(fieldName, out var columnId))
            {
                return null;
            }

            var value = Retrieve<StringColumnValue>(session, table, columnId, fieldName).Value;
            return string.IsNullOrEmpty(value) ? null : value;
        }

        internal static bool HasColumn(Dictionary<string, JET_COLUMNID> columns, string fieldName) =>
            columns.ContainsKey(fieldName);

        private static T Retrieve<T>(Session session, JET_TABLEID table, JET_COLUMNID columnId,
            string fieldName, int sequence = FirstValueSequence) where T : ColumnValue, new()
        {
            var value = new T { Columnid = columnId, ItagSequence = sequence };
            try
            {
                Api.RetrieveColumns(session, table, value);
                return value;
            }
            catch (EsentErrorException ex)
            {
                throw RetrievalError(fieldName, sequence, ex);
            }
        }

        private static int GetValueCount(Session session, JET_TABLEID table, JET_COLUMNID columnId,
            string fieldName)
        {
            var columns = new[] { new JET_RETRIEVECOLUMN { columnid = columnId, itagSequence = 0 } };
            try
            {
                Api.JetRetrieveColumns(session, table, columns, 1);
            }
            catch (EsentErrorException ex)
            {
                throw RetrievalError(fieldName, null, ex);
            }

            return columns[0].itagSequence >= 0
                ? columns[0].itagSequence
                : throw RetrievalError(fieldName, null,
                    new InvalidDataException($"ESE returned an invalid tagged-value count of {columns[0].itagSequence}"));
        }

        private static InvalidDataException RetrievalError(string fieldName, int? sequence,
            Exception innerException) => new(
                $"Failed to retrieve column '{fieldName}' tagged value{(sequence.HasValue ? $" {sequence}" : "s")}",
                innerException);
    }
}
