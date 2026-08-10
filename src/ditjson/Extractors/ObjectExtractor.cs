using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using ditjson.Decoders;
using ditjson.Filtering;
using ditjson.Models;
using Microsoft.Isam.Esent.Interop;

namespace ditjson.Extractors
{
    internal static class ObjectExtractor
    {
        internal static (List<User> users, List<Group> groups, List<Computer> computers)
            ExtractStructuredObjects(Session session, JET_DBID dbid, List<string> selectedTables,
                ObjectFilter.FilterOptions? filterOptions = null)
        {
            filterOptions ??= new ObjectFilter.FilterOptions();
            var users = new List<User>();
            var groups = new List<Group>();
            var computers = new List<Computer>();
            var directoryObjects = new Dictionary<int, NtdsObject>();
            var userAncestorIds = new Dictionary<int, List<int>>();

            if (!selectedTables.Contains("datatable"))
            {
                return (users, groups, computers);
            }

            try
            {
                using var table = new Table(session, dbid, "datatable", OpenTableGrbit.ReadOnly);
                var columnDict = Api.GetColumnDictionary(session, table);

                Api.JetSetTableSequential(session, table, SetTableSequentialGrbit.None);
                Api.MoveBeforeFirst(session, table);

                var recordId = 1;
                while (Api.TryMoveNext(session, table))
                {
                    try
                    {
                        // ATTj590126 is sAMAccountType. The old implementation used
                        // ATTj590000, which is not an AD datatable column, so every
                        // record was skipped before it could be classified.
                        if (!columnDict.ContainsKey(NtdsColumnNames.SamAccountType))
                        {
                            continue;
                        }

                        var samAccountType = ColumnExtractor.GetInt32(
                            session, table, columnDict, NtdsColumnNames.SamAccountType);
                        var currentRecordId = ColumnExtractor.GetRecordId(
                            session, table, columnDict, recordId);
                        directoryObjects[currentRecordId] = ExtractDirectoryReference(
                            session, table, columnDict, currentRecordId, samAccountType);

                        if (ObjectClassifier.IsUserObject(samAccountType))
                        {
                            var user = UserExtractor.ExtractUser(session, table, currentRecordId, columnDict);
                            FieldCleaner.CleanUser(user);
                            if (ObjectFilter.ShouldIncludeUser(user, filterOptions))
                            {
                                userAncestorIds[user.RecordId] = ParseAncestorIds(ColumnExtractor.GetBinary(
                                    session, table, columnDict, NtdsColumnNames.Ancestors));
                                ObjectFilter.CleanupUser(user, filterOptions.IncludeEmptyCollections);
                                users.Add(user);
                            }
                        }
                        else if (ObjectClassifier.IsGroupObject(samAccountType))
                        {
                            var group = GroupExtractor.ExtractGroup(session, table, currentRecordId, columnDict);
                            FieldCleaner.CleanGroup(group);
                            if (ObjectFilter.ShouldIncludeGroup(group, filterOptions))
                            {
                                ObjectFilter.CleanupGroup(group, filterOptions.IncludeEmptyCollections);
                                groups.Add(group);
                            }
                        }
                        else if (ObjectClassifier.IsComputerObject(samAccountType))
                        {
                            var computer = ComputerExtractor.ExtractComputer(session, table, currentRecordId, columnDict);
                            FieldCleaner.CleanComputer(computer);
                            if (ObjectFilter.ShouldIncludeComputer(computer, filterOptions))
                            {
                                ObjectFilter.CleanupComputer(computer, filterOptions.IncludeEmptyCollections);
                                computers.Add(computer);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[!] Error processing record {recordId}: {ex.GetType().Name}: {ex.Message}");
                    }

                    recordId++;
                }

                Api.JetResetTableSequential(session, table, ResetTableSequentialGrbit.None);
                PopulateAncestors(users, userAncestorIds, directoryObjects);
                foreach (var user in users)
                {
                    ObjectFilter.CleanupUser(user, filterOptions.IncludeEmptyCollections);
                }
            }
            catch (Exception ex)
            {
                throw new NtdsException("Failed to extract objects from datatable", ex);
            }

            if (selectedTables.Contains("link_table"))
            {
                Console.Error.WriteLine("[*] Extracting group memberships from link_table...");
                LinkExtractor.ExtractGroupMemberships(session, dbid, users, groups, computers);
            }

            return (users, groups, computers);
        }

        internal static List<int> ParseAncestorIds(byte[]? value)
        {
            var ids = new List<int>();
            if (value == null)
            {
                return ids;
            }

            for (var offset = 0; offset + sizeof(int) <= value.Length; offset += sizeof(int))
            {
                ids.Add(BinaryPrimitives.ReadInt32LittleEndian(value.AsSpan(offset, sizeof(int))));
            }

            return ids;
        }

        internal static void PopulateAncestors(IEnumerable<User> users,
            IReadOnlyDictionary<int, List<int>> ancestorIds,
            IReadOnlyDictionary<int, NtdsObject> directoryObjects)
        {
            foreach (var user in users)
            {
                user.Ancestors = ancestorIds.TryGetValue(user.RecordId, out var ids)
                    ? ids.Where(directoryObjects.ContainsKey).Select(id => directoryObjects[id]).ToList()
                    : [];
            }
        }

        private static NtdsObject ExtractDirectoryReference(Session session, JET_TABLEID table,
            IDictionary<string, JET_COLUMNID> columns, int recordId, int samAccountType) => new()
        {
            RecordId = recordId,
            Name = ColumnExtractor.GetString(session, table, columns, NtdsColumnNames.ObjectName),
            ObjectGuid = GuidDecoder.Decode(ColumnExtractor.GetBinary(
                session, table, columns, NtdsColumnNames.ObjectGuid)),
            ObjectSid = SidDecoder.DecodeNtds(ColumnExtractor.GetBinary(
                session, table, columns, NtdsColumnNames.ObjectSid)),
            ObjectClass = ObjectClassifier.IsUserObject(samAccountType) ? "user"
                : ObjectClassifier.IsComputerObject(samAccountType) ? "computer"
                : ObjectClassifier.IsGroupObject(samAccountType) ? "group"
                : "container"
        };
    }
}
