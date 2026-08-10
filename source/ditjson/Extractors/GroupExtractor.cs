using System;
using System.Collections.Generic;
using Microsoft.Isam.Esent.Interop;
using ditjson.Models;
using ditjson.Decoders;

namespace ditjson.Extractors
{
    internal static class GroupExtractor
    {
        internal static Group ExtractGroup(Session session, JET_TABLEID table, int recordId,
            IDictionary<string, JET_COLUMNID> columnDict)
        {
            var group = new Group
            {
                RecordId = recordId,
                Name = ColumnExtractor.GetString(session, table, columnDict, NtdsColumnNames.ObjectName)!,
                ObjectClass = "group",
                ObjectGuid = GuidDecoder.Decode(ColumnExtractor.GetBinary(session, table,
                    columnDict, NtdsColumnNames.ObjectGuid)),
                ObjectSid = SidDecoder.DecodeNtds(ColumnExtractor.GetBinary(session, table,
                    columnDict, NtdsColumnNames.ObjectSid)),

                SamAccountName = ColumnExtractor.GetString(session, table, columnDict,
                    NtdsColumnNames.SamAccountName)!,

                WhenCreated = TimestampDecoder.DecodeDsTime(ColumnExtractor.GetInt64(
                    session, table, columnDict, NtdsColumnNames.WhenCreated))!,
                WhenChanged = TimestampDecoder.DecodeDsTime(ColumnExtractor.GetInt64(
                    session, table, columnDict, NtdsColumnNames.WhenChanged))!,

                IsDeleted = ColumnExtractor.GetInt32(session, table, columnDict,
                    NtdsColumnNames.IsDeleted) != 0,

                Members = []
            };

            var groupTypeValue = ColumnExtractor.GetInt32(session, table, columnDict,
                "ATTj590574");
            if (groupTypeValue != 0)
            {
                group.GroupType = FlagsDecoder.DecodeGroupType(groupTypeValue);
            }

            return group;
        }
    }
}
