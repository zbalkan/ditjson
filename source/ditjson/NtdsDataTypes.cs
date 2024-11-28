using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Isam.Esent.Interop;
using Microsoft.Isam.Esent.Interop.Vista;
using Microsoft.Isam.Esent.Interop.Windows10;

namespace ditjson
{
    internal static class NtdsDataTypes
    {
        /// <summary>
        ///     Extracts the column data as the correct data type and formats appropriately
        /// </summary>
        /// <param name="session">
        /// </param>
        /// <param name="table">
        /// </param>
        /// <param name="columnInfo">
        /// </param>
        /// <returns>
        /// </returns>
        /// <exception cref="NtdsException">
        /// </exception>
        /// <exception cref="FormatException">
        /// </exception>
        /// <exception cref="OverflowException">
        /// </exception>
        internal static string GetFormattedValue(Session session,
                                                    JET_TABLEID table,
                                                    ColumnInfo columnInfo)
        {
            var temp = string.Empty;

            // Edge case: link_data_v2 column cannot be retreived properly None of the
            // Api.RetrieveColumnXXX commands can process this column.
            if (columnInfo.Name.Equals("link_data_v2"))
            {
                return temp;
            }

            if(columnInfo.Name.Equals("ATTp131353"))
            {
                //
            }

            if (columnInfo.Grbit.HasFlag(ColumndefGrbit.ColumnMultiValued))
            {
                temp = GetMultipleValues(session, table, columnInfo.Columnid, columnInfo.Coltyp);
            }
            else
            {
                temp = GetSingleValue(session, table, columnInfo.Columnid, columnInfo.Coltyp);
            }

            return temp.Replace("\0", string.Empty);
        }

        private static string GetMultipleValues(Session session, JET_TABLEID table, JET_COLUMNID columnId, JET_coltyp columnType)
        {
            var values = new List<string>();

            var retrievecolumn = new JET_RETRIEVECOLUMN
            {
                columnid = columnId,
                itagSequence = 0
            };
            Api.JetRetrieveColumns(session, table, [retrievecolumn], 1);
            var count = retrievecolumn.itagSequence;

            if (count != 0)
            {
                // itag value starts at 1
                for (var itag = 1; itag <= count; ++itag)
                {
                    var s = GetSingleValue(session, table, columnId, columnType);
                    values.Add(s);
                }
            }
            return values.Count > 0 ? string.Join(",", values) : string.Empty;
        }

        private static string GetSingleValue(Session session, JET_TABLEID table, JET_COLUMNID columnId, JET_coltyp columnType)
        {
            var temp = string.Empty;
            switch (columnType)
            {
                case JET_coltyp.Bit:
                    temp = string.Format("{0}", Api.RetrieveColumnAsBoolean(session, table, columnId));
                    break;

                case VistaColtyp.LongLong:
                case Windows10Coltyp.UnsignedLongLong:
                case JET_coltyp.Currency:
                    temp = string.Format("{0}", Api.RetrieveColumnAsInt64(session, table, columnId));
                    break;

                case JET_coltyp.IEEEDouble:
                    temp = string.Format("{0}", Api.RetrieveColumnAsDouble(session, table, columnId));
                    break;

                case JET_coltyp.IEEESingle:
                    temp = string.Format("{0}", Api.RetrieveColumnAsFloat(session, table, columnId));
                    break;

                case JET_coltyp.Long:
                    temp = string.Format("{0}", Api.RetrieveColumnAsInt32(session, table, columnId));
                    break;

                case JET_coltyp.Text:
                case JET_coltyp.LongText:
                    temp = string.Format("{0}", Api.RetrieveColumnAsString(session, table, columnId, Encoding.Unicode, RetrieveColumnGrbit.None));
                    break;

                case JET_coltyp.Short:
                    temp = string.Format("{0}", Api.RetrieveColumnAsInt16(session, table, columnId));
                    break;

                case JET_coltyp.UnsignedByte:
                    temp = string.Format("{0}", Api.RetrieveColumnAsByte(session, table, columnId));
                    break;

                case JET_coltyp.DateTime:
                    temp = string.Format("{0}", Api.RetrieveColumnAsDateTime(session, table, columnId));
                    break;

                case VistaColtyp.UnsignedShort:
                    temp = string.Format("{0}", Api.RetrieveColumnAsUInt16(session, table, columnId));
                    break;

                case VistaColtyp.UnsignedLong:
                    temp = string.Format("{0}", Api.RetrieveColumnAsUInt32(session, table, columnId));
                    break;

                case VistaColtyp.GUID:
                    temp = string.Format("{0}", Api.RetrieveColumnAsGuid(session, table, columnId));
                    break;

                case JET_coltyp.Nil:
                    break;

                case JET_coltyp.Binary:
                case JET_coltyp.LongBinary:
                    var columnBytes = Api.RetrieveColumn(session, table, columnId);
                    if (columnBytes != null)
                    {
                        temp = ByteHelper.FormatBytes(columnBytes);
                    }
                    break;

                default:
                    throw new NtdsException($"Unhandled column type {columnType} for {columnId}");
            }

            return temp;
        }
    }
}