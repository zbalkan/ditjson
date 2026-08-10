using System;
using System.Collections.Generic;
using Microsoft.Isam.Esent.Interop;
using ditjson.Filtering;
using ditjson.Models;

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

            if (!selectedTables.Contains("datatable"))
                return (users, groups, computers);

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
                                continue;

                            var samAccountType = ColumnExtractor.GetInt32(
                                session, table, columnDict, NtdsColumnNames.SamAccountType);
                            var currentRecordId = ColumnExtractor.GetRecordId(
                                session, table, columnDict, recordId);

                            if (ObjectClassifier.IsUserObject(samAccountType))
                            {
                                var user = UserExtractor.ExtractUser(session, table, currentRecordId, columnDict);
                                FieldCleaner.CleanUser(user);
                                if (ObjectFilter.ShouldIncludeUser(user, filterOptions))
                                {
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
                            Console.Error.WriteLine($"[!] Error processing record {recordId}: {ex.Message}");
                        }

                        recordId++;
                    }

                    Api.JetResetTableSequential(session, table, ResetTableSequentialGrbit.None);
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
    }
}
