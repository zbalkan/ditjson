using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Isam.Esent.Interop;
using ditjson.Models;

namespace ditjson.Extractors
{
    internal static class LinkExtractor
    {
        // Link table attribute IDs (from ntdsxtract)
        private const int MEMBEROF_ATTR = 0x20000210;  // memberOf
        private const int MEMBER_ATTR = 0x200000d8;    // member
        private const int PRIMARY_GROUP_ATTR = 0x20000009;

        internal static void ExtractGroupMemberships(Session session, JET_DBID dbid, List<User> users, List<Group> groups, List<Computer> computers)
        {
            if (users.Count == 0 && computers.Count == 0 && groups.Count == 0)
                return;

            var userDict = users.ToDictionary(u => u.RecordId);
            var groupDict = groups.ToDictionary(g => g.RecordId);
            var computerDict = computers.ToDictionary(c => c.RecordId);

            try
            {
                using var table = new Table(session, dbid, "link_table", OpenTableGrbit.ReadOnly);
                var columnDict = Api.GetColumnDictionary(session, table);

                if (!columnDict.ContainsKey("ATTj590001") || !columnDict.ContainsKey("ATTj590002") || !columnDict.ContainsKey("ATTk590005"))
                    return;

                Api.JetSetTableSequential(session, table, SetTableSequentialGrbit.None);
                Api.MoveBeforeFirst(session, table);

                while (Api.TryMoveNext(session, table))
                {
                    try
                    {
                        var backLinkAttribute = ColumnExtractor.GetInt32(session, table, columnDict, "ATTj590001");
                        var sourceRecordId = ColumnExtractor.GetInt32(session, table, columnDict, "ATTj590002");
                        var targetRecordId = ColumnExtractor.GetInt32(session, table, columnDict, "ATTk590005");
                        var deletedTime = ColumnExtractor.GetString(session, table, columnDict, "ATTm590006");

                        // memberOf relationship (0x20000210)
                        if (backLinkAttribute == MEMBEROF_ATTR)
                        {
                            ProcessMemberOf(sourceRecordId, targetRecordId, deletedTime, userDict, computerDict, groupDict);
                        }
                        // member relationship (0x200000d8)
                        else if (backLinkAttribute == MEMBER_ATTR)
                        {
                            ProcessMember(sourceRecordId, targetRecordId, deletedTime, groupDict, userDict, computerDict);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[!] Error processing link record: {ex.Message}");
                    }
                }

                Api.JetResetTableSequential(session, table, ResetTableSequentialGrbit.None);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Failed to extract group memberships: {ex.Message}");
            }
        }

        private static void ProcessMemberOf(int sourceRecordId, int targetRecordId, string? deletedTime,
            Dictionary<int, User> userDict, Dictionary<int, Computer> computerDict, Dictionary<int, Group> groupDict)
        {
            if (!groupDict.TryGetValue(targetRecordId, out var targetGroup))
                return;

            var membership = new GroupMembership
            {
                RecordId = targetRecordId,
                Name = targetGroup.Name,
                ObjectGuid = targetGroup.ObjectGuid,
                ObjectSid = targetGroup.ObjectSid,
                IsPrimaryGroup = false,
                DeletedTime = deletedTime
            };

            if (userDict.TryGetValue(sourceRecordId, out var user))
            {
                user.MemberOf ??= [];
                if (!user.MemberOf.Any(m => m.RecordId == targetRecordId))
                    user.MemberOf.Add(membership);
            }
            else if (computerDict.TryGetValue(sourceRecordId, out var computer))
            {
                computer.MemberOf ??= [];
                if (!computer.MemberOf.Any(m => m.RecordId == targetRecordId))
                    computer.MemberOf.Add(membership);
            }
        }

        private static void ProcessMember(int sourceRecordId, int targetRecordId, string? deletedTime,
            Dictionary<int, Group> groupDict, Dictionary<int, User> userDict, Dictionary<int, Computer> computerDict)
        {
            if (!groupDict.TryGetValue(sourceRecordId, out var group))
                return;

            GroupMember? member = null;

            if (userDict.TryGetValue(targetRecordId, out var user))
            {
                member = new GroupMember
                {
                    RecordId = targetRecordId,
                    Name = user.Name,
                    ObjectGuid = user.ObjectGuid,
                    ObjectClass = user.ObjectClass,
                    IsPrimaryGroup = false,
                    DeletedTime = deletedTime
                };
            }
            else if (computerDict.TryGetValue(targetRecordId, out var computer))
            {
                member = new GroupMember
                {
                    RecordId = targetRecordId,
                    Name = computer.Name,
                    ObjectGuid = computer.ObjectGuid,
                    ObjectClass = computer.ObjectClass,
                    IsPrimaryGroup = false,
                    DeletedTime = deletedTime
                };
            }
            else if (groupDict.TryGetValue(targetRecordId, out var targetGroup))
            {
                member = new GroupMember
                {
                    RecordId = targetRecordId,
                    Name = targetGroup.Name,
                    ObjectGuid = targetGroup.ObjectGuid,
                    ObjectClass = targetGroup.ObjectClass,
                    IsPrimaryGroup = false,
                    DeletedTime = deletedTime
                };
            }

            if (member != null)
            {
                group.Members ??= [];
                if (!group.Members.Any(m => m.RecordId == targetRecordId))
                    group.Members.Add(member);
            }
        }
    }
}
