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
                Name = ColumnExtractor.GetString(session, table, columnDict, "ATTm589825")!,
                ObjectClass = "group",
                ObjectGuid = GuidDecoder.Decode(ColumnExtractor.GetBinary(session, table,
                    columnDict, "ATTk589826")),
                ObjectSid = SidDecoder.Decode(ColumnExtractor.GetBinary(session, table,
                    columnDict, NtdsColumnNames.ObjectSid)),

                SamAccountName = ColumnExtractor.GetString(session, table, columnDict,
                    NtdsColumnNames.SamAccountName)!,

                WhenCreated = ColumnExtractor.GetString(session, table, columnDict,
                    "ATTl131074")!,
                WhenChanged = ColumnExtractor.GetString(session, table, columnDict,
                    "ATTl131075")!,

                IsDeleted = ColumnExtractor.GetInt32(session, table, columnDict,
                    "ATTi131120") != 0,

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
