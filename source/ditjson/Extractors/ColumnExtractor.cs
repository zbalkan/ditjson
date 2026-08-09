using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Isam.Esent.Interop;

namespace ditjson.Extractors
{
    internal static class ColumnExtractor
    {
        internal static string? GetString(Session session, JET_TABLEID table,
            IDictionary<string, JET_COLUMNID> columnDict, string fieldName)
        {
            try
            {
                if (!columnDict.ContainsKey(fieldName))
                    return null;

                var value = Api.RetrieveColumnAsString(session, table, columnDict[fieldName],
                    Encoding.Unicode);
                return string.IsNullOrEmpty(value) ? null : value;
            }
            catch
            {
                return null;
            }
        }

        internal static int GetInt32(Session session, JET_TABLEID table,
            IDictionary<string, JET_COLUMNID> columnDict, string fieldName)
        {
            try
            {
                if (!columnDict.ContainsKey(fieldName))
                    return 0;

                var value = Api.RetrieveColumnAsInt32(session, table, columnDict[fieldName]);
                return value ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        internal static long GetInt64(Session session, JET_TABLEID table,
            IDictionary<string, JET_COLUMNID> columnDict, string fieldName)
        {
            try
            {
                if (!columnDict.ContainsKey(fieldName))
                    return 0;

                var value = Api.RetrieveColumnAsInt64(session, table, columnDict[fieldName]);
                return value ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        internal static byte[]? GetBinary(Session session, JET_TABLEID table,
            IDictionary<string, JET_COLUMNID> columnDict, string fieldName)
        {
            try
            {
                if (!columnDict.ContainsKey(fieldName))
                    return null;

                return Api.RetrieveColumn(session, table, columnDict[fieldName]);
            }
            catch
            {
                return null;
            }
        }

        internal static bool HasColumn(Dictionary<string, JET_COLUMNID> columnDict, string fieldName) => columnDict.ContainsKey(fieldName);
    }
}
