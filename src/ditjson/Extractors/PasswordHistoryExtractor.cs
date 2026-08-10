using System;
using System.Collections.Generic;
using System.Linq;
using ditjson.Models;
using Microsoft.Isam.Esent.Interop;

namespace ditjson.Extractors
{
    internal static class PasswordHistoryExtractor
    {
        internal static void ExtractPasswordHistory(Session session, JET_DBID dbid, List<User> users, IReadOnlyList<byte[]> peks)
        {
            if (users == null || users.Count == 0 || peks.Count == 0)
            {
                return;
            }

            Console.Error.WriteLine("[*] Extracting password history...");

            try
            {
                using var table = new Table(session, dbid, "datatable", OpenTableGrbit.ReadOnly);
                var columnDict = Api.GetColumnDictionary(session, table);
                var userDict = users.ToDictionary(u => u.RecordId);

                Api.JetSetTableSequential(session, table, SetTableSequentialGrbit.None);
                Api.MoveBeforeFirst(session, table);

                var recordId = 1;
                while (Api.TryMoveNext(session, table))
                {
                    var currentRecordId = ColumnExtractor.GetRecordId(session, table, columnDict, recordId);
                    if (userDict.TryGetValue(currentRecordId, out var user))
                    {
                        ExtractHistoryForUser(session, table, columnDict, user, peks);
                    }
                    recordId++;
                }

                Api.JetResetTableSequential(session, table, ResetTableSequentialGrbit.None);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[!] Error extracting password history: {ex.Message}");
            }
        }

        internal static List<string> ParsePasswordHistory(byte[] data, IReadOnlyList<byte[]> peks, uint rid)
        {
            var hashes = new List<string>();
            var plain = CredentialCrypto.RemoveRidDesLayer(CredentialCrypto.UnwrapAttribute(data, peks), rid);
            // ntPwdHistory/lmPwdHistory contain the current value at index zero.
            // It is already represented by passwordHashes, so only expose prior
            // values here. Preserve zero entries after it to keep LM/NT indices
            // aligned without duplicating the current hashes in the JSON.
            for (var offset = 16; offset + 16 <= plain.Length; offset += 16)
            {
                var hashData = plain.AsSpan(offset, 16).ToArray();
                hashes.Add(Convert.ToHexString(hashData));
            }
            return hashes;
        }

        private static void ExtractHistoryForUser(Session session, JET_TABLEID table,
                    IDictionary<string, JET_COLUMNID> columnDict, User user, IReadOnlyList<byte[]> peks)
        {
            try
            {
                var ntHistoryData = ColumnExtractor.GetBinary(
                    session, table, columnDict, NtdsColumnNames.NtHashHistory);
                var lmHistoryData = ColumnExtractor.GetBinary(
                    session, table, columnDict, NtdsColumnNames.LmHashHistory);
                if ((ntHistoryData == null || ntHistoryData.Length == 0) &&
                    (lmHistoryData == null || lmHistoryData.Length == 0))
                {
                    return;
                }

                var rid = PasswordHashDecryptor.GetRid(user.ObjectSid);
                if (ntHistoryData?.Length > 0)
                {
                    var hashes = ParsePasswordHistory(ntHistoryData, peks, rid);
                    if (hashes.Count > 0)
                    {
                        user.PasswordHistory = hashes;
                    }
                }
                if (lmHistoryData?.Length > 0)
                {
                    var hashes = ParsePasswordHistory(lmHistoryData, peks, rid);
                    if (hashes.Count > 0)
                    {
                        user.LmPasswordHistory = hashes;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[!] Error extracting password history for user {user.SamAccountName}: {ex.Message}");
            }
        }
    }
}
