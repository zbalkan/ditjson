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
                Name = ColumnExtractor.GetString(session, table, columnDict, "ATTm589825")!,
                ObjectClass = "computer",
                ObjectGuid = GuidDecoder.Decode(ColumnExtractor.GetBinary(session, table,
                    columnDict, "ATTk589826")),
                ObjectSid = SidDecoder.Decode(ColumnExtractor.GetBinary(session, table,
                    columnDict, NtdsColumnNames.ObjectSid)),

                SamAccountName = ColumnExtractor.GetString(session, table, columnDict,
                    NtdsColumnNames.SamAccountName)!,
                DnsHostName = ColumnExtractor.GetString(session, table, columnDict,
                    "ATTm590443")!,
                OperatingSystem = ColumnExtractor.GetString(session, table, columnDict,
                    "ATTm590187")!,
                OperatingSystemVersion = ColumnExtractor.GetString(session, table, columnDict,
                    "ATTm590188")!,

                WhenCreated = TimestampDecoder.DecodeDsTime(ColumnExtractor.GetInt64(
                    session, table, columnDict, "ATTl131074"))!,
                WhenChanged = TimestampDecoder.DecodeDsTime(ColumnExtractor.GetInt64(
                    session, table, columnDict, "ATTl131075"))!,

                PasswordLastSet = TimestampDecoder.DecodeFromInt64(ColumnExtractor.GetInt64(
                    session, table, columnDict, "ATTq589920"))!,

                DialInAccessPermission = ColumnExtractor.GetInt32(session, table, columnDict,
                    "ATTi590943"),

                IsDeleted = ColumnExtractor.GetInt32(session, table, columnDict,
                    "ATTi131120") != 0,

                MemberOf = []
            };

            return computer;
        }
    }
}
