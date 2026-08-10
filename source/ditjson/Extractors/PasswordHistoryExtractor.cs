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
                return;

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

        private static void ExtractHistoryForUser(Session session, JET_TABLEID table,
            IDictionary<string, JET_COLUMNID> columnDict, User user, IReadOnlyList<byte[]> peks)
        {
            try
            {
                var pwdHistoryData = ColumnExtractor.GetBinary(
                    session, table, columnDict, NtdsColumnNames.NtHashHistory);
                if (pwdHistoryData == null || pwdHistoryData.Length == 0)
                    return;

                var hashes = ParsePasswordHistory(pwdHistoryData, peks, PasswordHashDecryptor.GetRid(user.ObjectSid));
                if (hashes != null && hashes.Count > 0)
                {
                    user.PasswordHistory = hashes;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[!] Error extracting password history for user {user.SamAccountName}: {ex.Message}");
            }
        }

        private static bool isZeroHash(byte[] hash)
        {
            if (hash == null || hash.Length < 16) return true;
            for (var i = 0; i < 16; i++) if (hash[i] != 0) return false;
            return true;
        }

        internal static List<string> ParsePasswordHistory(byte[] data, IReadOnlyList<byte[]> peks, uint rid)
        {
            var hashes = new List<string>();
            try
            {
                var plain = CredentialCrypto.RemoveRidDesLayer(CredentialCrypto.UnwrapAttribute(data, peks), rid);
                for (var offset = 0; offset + 16 <= plain.Length; offset += 16)
                {
                    var hashData = plain.AsSpan(offset, 16).ToArray();
                    if (!isZeroHash(hashData)) hashes.Add(BitConverter.ToString(hashData).Replace("-", "").ToUpperInvariant());
                }
                return hashes;
            }
            catch
            {
                return hashes;
            }
        }
    }
}
