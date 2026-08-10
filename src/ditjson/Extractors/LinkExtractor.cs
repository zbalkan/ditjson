using System;
using System.Collections.Generic;
using System.Linq;
using ditjson.Decoders;
using ditjson.Models;
using Microsoft.Isam.Esent.Interop;

namespace ditjson.Extractors
{
    internal static class LinkExtractor
    {
        private const string BacklinkDnt = "backlink_DNT";
        private const string LinkBase = "link_base";
        private const string LinkDeleteTime = "link_deltime";
        private const string LinkDnt = "link_DNT";
        private const int MemberLinkId = 2;

        internal static void ExtractGroupMemberships(Session session, JET_DBID dbid, List<User> users,
            List<Group> groups, List<Computer> computers)
        {
            if (users.Count == 0 && computers.Count == 0 && groups.Count == 0)
            {
                return;
            }

            try
            {
                using var table = new Table(session, dbid, "link_table", OpenTableGrbit.ReadOnly);
                var columns = Api.GetColumnDictionary(session, table);
                if (!columns.ContainsKey(BacklinkDnt) || !columns.ContainsKey(LinkDnt))
                {
                    Console.Error.WriteLine(
                        $"[!] Group membership unavailable: link_table must contain {BacklinkDnt} and {LinkDnt}");
                    return;
                }

                var linksRead = 0;
                var membershipsAdded = 0;
                var userById = users.ToDictionary(u => u.RecordId);
                var groupById = groups.ToDictionary(g => g.RecordId);
                var computerById = computers.ToDictionary(c => c.RecordId);
                Api.JetSetTableSequential(session, table, SetTableSequentialGrbit.None);
                Api.MoveBeforeFirst(session, table);

                while (Api.TryMoveNext(session, table))
                {
                    try
                    {
                        // link_base is the schema linkID. member is linkID 2; without
                        // this filter, unrelated linked attributes (for example
                        // managedBy) can be mistaken for group membership.
                        if (columns.ContainsKey(LinkBase) &&
                            ColumnExtractor.GetInt32(session, table, columns, LinkBase) != MemberLinkId)
                        {
                            continue;
                        }

                        // In the AD link table, backlink_DNT is the member and link_DNT is
                        // the object holding the forward link (the group for member links).
                        var memberRecordId = ColumnExtractor.GetInt32(session, table, columns, BacklinkDnt);
                        var groupRecordId = ColumnExtractor.GetInt32(session, table, columns, LinkDnt);
                        var deletedTime = TimestampDecoder.DecodeDsTime(
                            ColumnExtractor.GetInt64(session, table, columns, LinkDeleteTime));
                        linksRead++;
                        if (AddDirectMembership(memberRecordId, groupRecordId, deletedTime,
                                userById, groupById, computerById))
                        {
                            membershipsAdded++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[!] Error processing link record: {ex.GetType().Name}: {ex.Message}");
                    }
                }

                Api.JetResetTableSequential(session, table, ResetTableSequentialGrbit.None);
                var primaryGroupsAdded = AddPrimaryGroupMemberships(users, groups, computers);
                Console.Error.WriteLine(
                    $"[+] Group memberships: {membershipsAdded} direct and {primaryGroupsAdded} primary-group relationships ({linksRead} link rows read)");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[!] Failed to extract group memberships: {ex.GetType().Name}: {ex.Message}");
            }
        }

        internal static bool AddDirectMembership(int memberRecordId, int groupRecordId, string? deletedTime,
            IList<User> users, IList<Group> groups, IList<Computer> computers)
            => AddDirectMembership(memberRecordId, groupRecordId, deletedTime,
                users.ToDictionary(u => u.RecordId), groups.ToDictionary(g => g.RecordId),
                computers.ToDictionary(c => c.RecordId));

        private static bool AddDirectMembership(int memberRecordId, int groupRecordId, string? deletedTime,
            IReadOnlyDictionary<int, User> users, IReadOnlyDictionary<int, Group> groups,
            IReadOnlyDictionary<int, Computer> computers)
        {
            if (!groups.TryGetValue(groupRecordId, out var group))
            {
                return false;
            }

            NtdsObject? member = users.TryGetValue(memberRecordId, out var user) ? user
                : computers.TryGetValue(memberRecordId, out var computer) ? computer
                : groups.TryGetValue(memberRecordId, out var childGroup) ? childGroup
                : null;
            if (member == null)
            {
                return false;
            }

            AddMembership(member, group, deletedTime, false);
            return true;
        }

        internal static int AddPrimaryGroupMemberships(IList<User> users, IList<Group> groups,
            IList<Computer> computers)
        {
            var groupsBySid = groups
                .Where(g => !string.IsNullOrEmpty(g.ObjectSid))
                .GroupBy(g => g.ObjectSid!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var added = 0;

            foreach (var user in users)
            {
                if (TryAddPrimaryGroup(user, user.PrimaryGroupId, groupsBySid)) added++;
            }

            foreach (var computer in computers)
            {
                if (TryAddPrimaryGroup(computer, computer.PrimaryGroupId, groupsBySid)) added++;
            }

            return added;
        }

        private static bool TryAddPrimaryGroup(NtdsObject principal, int primaryGroupId,
            IDictionary<string, Group> groupsBySid)
        {
            if (primaryGroupId <= 0 || string.IsNullOrEmpty(principal.ObjectSid)) return false;
            var separator = principal.ObjectSid.LastIndexOf('-');
            if (separator <= 0) return false;
            var groupSid = $"{principal.ObjectSid[..separator]}-{primaryGroupId}";
            if (!groupsBySid.TryGetValue(groupSid, out var group)) return false;

            AddMembership(principal, group, null, true);
            return true;
        }

        private static void AddMembership(NtdsObject member, Group group, string? deletedTime, bool isPrimary)
        {
            group.Members ??= [];
            if (!group.Members.Any(m => m.RecordId == member.RecordId &&
                    m.DeletedTime == deletedTime && m.IsPrimaryGroup == isPrimary))
            {
                group.Members.Add(new GroupMember {
                    RecordId = member.RecordId,
                    Name = member.Name,
                    ObjectGuid = member.ObjectGuid,
                    ObjectSid = member.ObjectSid,
                    ObjectClass = member.ObjectClass,
                    IsPrimaryGroup = isPrimary,
                    DeletedTime = deletedTime
                });
            }

            var membership = new GroupMembership {
                Name = group.Name,
                ObjectSid = group.ObjectSid
            };

            List<GroupMembership>? memberships = member switch {
                User user => user.MemberOf ??= [],
                Computer computer => computer.MemberOf ??= [],
                Group childGroup => childGroup.MemberOf ??= [],
                _ => null
            };
            if (memberships != null && !memberships.Any(m =>
                    string.Equals(m.ObjectSid, group.ObjectSid, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(m.Name, group.Name, StringComparison.Ordinal)))
            {
                memberships.Add(membership);
            }
        }
    }
}
