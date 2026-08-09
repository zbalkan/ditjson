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
                Name = ColumnExtractor.GetString(session, table, columnDict, "ATTm131220"),
                ObjectClass = "group",
                ObjectGuid = GuidDecoder.Decode(ColumnExtractor.GetBinary(session, table,
                    columnDict, "ATTb131353")),
                ObjectSid = SidDecoder.Decode(ColumnExtractor.GetBinary(session, table,
                    columnDict, "ATTr589970")),

                SamAccountName = ColumnExtractor.GetString(session, table, columnDict,
                    "ATTm590045"),

                WhenCreated = TimestampDecoder.DecodeFromInt64(ColumnExtractor.GetInt64(
                    session, table, columnDict, "ATTq591520")),
                WhenChanged = TimestampDecoder.DecodeFromInt64(ColumnExtractor.GetInt64(
                    session, table, columnDict, "ATTq591521")),

                IsDeleted = ColumnExtractor.GetString(session, table, columnDict,
                    "ATTb589825") != null,

                Members = []
            };

            var groupTypeValue = ColumnExtractor.GetInt32(session, table, columnDict,
                "ATTj590077");
            if (groupTypeValue != 0)
            {
                group.GroupType = FlagsDecoder.DecodeGroupType(groupTypeValue);
            }

            return group;
        }
    }
}
