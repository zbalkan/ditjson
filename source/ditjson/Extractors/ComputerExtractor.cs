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
            var sid = string.Empty;
            if(Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                sid = SidDecoder.Decode(ColumnExtractor.GetBinary(session, table, columnDict, "ATTr589970")) ?? string.Empty;
            }
            var computer = new Computer
            {
                RecordId = recordId,
                Name = ColumnExtractor.GetString(session, table, columnDict, "ATTm131220")!,
                ObjectClass = "computer",
                ObjectGuid = GuidDecoder.Decode(ColumnExtractor.GetBinary(session, table,
                    columnDict, "ATTb131353")),
                
                ObjectSid = sid,

                SamAccountName = ColumnExtractor.GetString(session, table, columnDict,
                    "ATTm590045")!,
                DnsHostName = ColumnExtractor.GetString(session, table, columnDict,
                    "ATTm1677470")!,
                OperatingSystem = ColumnExtractor.GetString(session, table, columnDict,
                    "ATTm590280")!,
                OperatingSystemVersion = ColumnExtractor.GetString(session, table, columnDict,
                    "ATTm590281")!,

                WhenCreated = TimestampDecoder.DecodeFromInt64(ColumnExtractor.GetInt64(
                    session, table, columnDict, "ATTq591520"))!,
                WhenChanged = TimestampDecoder.DecodeFromInt64(ColumnExtractor.GetInt64(
                    session, table, columnDict, "ATTq591521"))!,

                PasswordLastSet = TimestampDecoder.DecodeFromInt64(ColumnExtractor.GetInt64(
                    session, table, columnDict, "ATTq589926"))!,

                DialInAccessPermission = ColumnExtractor.GetInt32(session, table, columnDict,
                    "ATTj590093"),

                IsDeleted = ColumnExtractor.GetString(session, table, columnDict,
                    "ATTb589825") != null,

                MemberOf = []
            };

            return computer;
        }
    }
}
