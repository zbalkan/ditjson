using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Isam.Esent.Interop;
using Microsoft.Isam.Esent.Interop.Vista;
using Microsoft.Isam.Esent.Interop.Windows10;

namespace ditjson
{
    internal static class NtdsData
    {
        internal static Dictionary<string, List<Dictionary<string, string>>> ExtractTables(
            Session session, JET_DBID dbid, List<string> tables) =>
            tables.ToDictionary(table => table, table => TableToList(session, dbid, table),
                StringComparer.Ordinal);

        private static List<Dictionary<string, string>> TableToList(
            Session session, JET_DBID dbid, string tableName)
        {
            var rows = new List<Dictionary<string, string>>();
            var columns = Api.GetTableColumns(session, dbid, tableName);

            using var table = new Table(session, dbid, tableName, OpenTableGrbit.ReadOnly);
            Api.JetSetTableSequential(session, table, SetTableSequentialGrbit.None);
            Api.MoveBeforeFirst(session, table);

            while (Api.TryMoveNext(session, table))
            {
                var row = new Dictionary<string, string>();
                foreach (var column in columns)
                {
                    var value = GetFormattedValue(session, table, column);
                    if (!string.IsNullOrEmpty(value))
                    {
                        row.Add(column.Name, value);
                    }
                }

                rows.Add(row);
            }

            return rows;
        }

        private static string GetFormattedValue(Session session, JET_TABLEID table, ColumnInfo column)
        {
            if (!column.Grbit.HasFlag(ColumndefGrbit.ColumnMultiValued))
            {
                return Format(Api.RetrieveColumn(session, table, column.Columnid), column.Coltyp);
            }

            var request = new JET_RETRIEVECOLUMN { columnid = column.Columnid };
            Api.JetRetrieveColumns(session, table, [request], 1);

            return string.Join(",", Enumerable.Range(1, request.itagSequence)
                .Select(index => Api.RetrieveColumn(session, table, column.Columnid, RetrieveColumnGrbit.None,
                    new JET_RETINFO { itagSequence = index }))
                .Where(value => value != null)
                .Select(value => Format(value!, column.Coltyp)));
        }

        private static string Format(byte[]? value, JET_coltyp type) =>
            value == null ? string.Empty : type switch
            {
                JET_coltyp.Bit => (value[0] != 0).ToString(),
                VistaColtyp.LongLong or JET_coltyp.Currency => BitConverter.ToInt64(value).ToString(CultureInfo.InvariantCulture),
                Windows10Coltyp.UnsignedLongLong => BitConverter.ToUInt64(value).ToString(CultureInfo.InvariantCulture),
                JET_coltyp.IEEEDouble => BitConverter.ToDouble(value).ToString(CultureInfo.InvariantCulture),
                JET_coltyp.IEEESingle => BitConverter.ToSingle(value).ToString(CultureInfo.InvariantCulture),
                JET_coltyp.Long => BitConverter.ToInt32(value).ToString(CultureInfo.InvariantCulture),
                JET_coltyp.Text or JET_coltyp.LongText => Encoding.Unicode.GetString(value).Replace("\0", string.Empty),
                JET_coltyp.Short => BitConverter.ToInt16(value).ToString(CultureInfo.InvariantCulture),
                JET_coltyp.UnsignedByte => value[0].ToString(CultureInfo.InvariantCulture),
                JET_coltyp.DateTime => DateTime.FromOADate(BitConverter.ToDouble(value)).ToString("O"),
                VistaColtyp.UnsignedShort => BitConverter.ToUInt16(value).ToString(CultureInfo.InvariantCulture),
                VistaColtyp.UnsignedLong => BitConverter.ToUInt32(value).ToString(CultureInfo.InvariantCulture),
                VistaColtyp.GUID => new Guid(value).ToString(),
                JET_coltyp.Binary or JET_coltyp.LongBinary => Convert.ToHexStringLower(value),
                JET_coltyp.Nil => string.Empty,
                _ => throw new NtdsException($"Unhandled column type {type}")
            };
    }
}
