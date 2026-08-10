using System;
using System.Collections.Generic;
using Microsoft.Isam.Esent.Interop;
using ditjson.Models;
using ditjson.Decoders;

namespace ditjson.Extractors
{
    internal static class ComputerExtractor
    {
        internal static Computer ExtractComputer(Session session, JET_TABLEID table, int recordId,
            IDictionary<string, JET_COLUMNID> columnDict)
        {
            var computer = new Computer
            {
                RecordId = recordId,
                Name = ColumnExtractor.GetString(session, table, columnDict, NtdsColumnNames.ObjectName)!,
                ObjectClass = "computer",
                ObjectGuid = GuidDecoder.Decode(ColumnExtractor.GetBinary(session, table,
                    columnDict, NtdsColumnNames.ObjectGuid)),
                ObjectSid = SidDecoder.Decode(ColumnExtractor.GetBinary(session, table,
                    columnDict, NtdsColumnNames.ObjectSid)),

                SamAccountName = ColumnExtractor.GetString(session, table, columnDict,
                    NtdsColumnNames.SamAccountName)!,
                DnsHostName = ColumnExtractor.GetString(session, table, columnDict,
                    NtdsColumnNames.DnsHostName)!,
                OperatingSystem = ColumnExtractor.GetString(session, table, columnDict,
                    NtdsColumnNames.OperatingSystem)!,
                OperatingSystemVersion = ColumnExtractor.GetString(session, table, columnDict,
                    NtdsColumnNames.OperatingSystemVersion)!,

                WhenCreated = TimestampDecoder.DecodeDsTime(ColumnExtractor.GetInt64(
                    session, table, columnDict, NtdsColumnNames.WhenCreated))!,
                WhenChanged = TimestampDecoder.DecodeDsTime(ColumnExtractor.GetInt64(
                    session, table, columnDict, NtdsColumnNames.WhenChanged))!,

                PasswordLastSet = TimestampDecoder.DecodeFromInt64(ColumnExtractor.GetInt64(
                    session, table, columnDict, NtdsColumnNames.PasswordLastSet))!,

                DialInAccessPermission = ColumnExtractor.GetInt32(session, table, columnDict,
                    NtdsColumnNames.DialInAccessPermission),

                IsDeleted = ColumnExtractor.GetInt32(session, table, columnDict,
                    NtdsColumnNames.IsDeleted) != 0,

                MemberOf = []
            };

            return computer;
        }
    }
}
