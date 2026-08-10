using System;
using System.Collections.Generic;
using System.Linq;
using ditjson.Models;
using Microsoft.Isam.Esent.Interop;

namespace ditjson.Extractors
{
    internal static class PasswordHashDecryptor
    {
        internal static void DecryptPasswordHashes(Session session, JET_DBID dbid, List<User> users, List<Computer> computers,
            IReadOnlyList<byte[]> peks)
        {
            Console.Error.WriteLine("[*] Decrypting password hashes...");
            DecryptUserHashes(session, dbid, users, peks);
            DecryptComputerHashes(session, dbid, computers, peks);
        }

        private static void DecryptComputerHashes(Session session, JET_DBID dbid, List<Computer> computers, IReadOnlyList<byte[]> peks)
        {
            if (computers == null || computers.Count == 0)
                return;

            try
            {
                using var table = new Table(session, dbid, "datatable", OpenTableGrbit.ReadOnly);
                var columnDict = Api.GetColumnDictionary(session, table);
                var computerDict = computers.ToDictionary(c => c.RecordId);

                Api.JetSetTableSequential(session, table, SetTableSequentialGrbit.None);
                Api.MoveBeforeFirst(session, table);

                var recordId = 1;
                while (Api.TryMoveNext(session, table))
                {
                    var currentRecordId = ColumnExtractor.GetRecordId(session, table, columnDict, recordId);
                    if (computerDict.TryGetValue(currentRecordId, out var computer))
                    {
                        DecryptHashesForComputer(session, table, columnDict, computer, peks);
                    }
                    recordId++;
                }

                Api.JetResetTableSequential(session, table, ResetTableSequentialGrbit.None);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[!] Error decrypting computer hashes: {ex.Message}");
            }
        }

        internal static uint GetRid(string? sid)
        {
            if (string.IsNullOrWhiteSpace(sid)) throw new InvalidOperationException("Credential-bearing object has no SID");
            var separator = sid.LastIndexOf('-');
            if (separator < 0 || !uint.TryParse(sid.Substring(separator + 1), out var rid)) throw new InvalidOperationException($"Invalid SID: {sid}");
            return rid;
        }

        internal static byte[] DecryptHash(byte[] encryptedHash, IReadOnlyList<byte[]> peks, uint rid) =>
            CredentialCrypto.RemoveRidDesLayer(CredentialCrypto.UnwrapAttribute(encryptedHash, peks), rid);

        private static void DecryptHashesForComputer(Session session, JET_TABLEID table,
            IDictionary<string, JET_COLUMNID> columnDict, Computer computer, IReadOnlyList<byte[]> peks)
        {
            try
            {
                // Decrypt NT hash
                var ntHashEncrypted = ColumnExtractor.GetBinary(
                    session, table, columnDict, NtdsColumnNames.NtHash);
                if (ntHashEncrypted != null && ntHashEncrypted.Length >= 24)
                {
                    var ntHashDecrypted = DecryptHash(ntHashEncrypted, peks, GetRid(computer.ObjectSid));
                    if (ntHashDecrypted != null && ntHashDecrypted.Length >= 16)
                    {
                        computer.PasswordHashes ??= new PasswordHashes();
                        computer.PasswordHashes.NtHash = BitConverter.ToString(ntHashDecrypted, 0, 16).Replace("-", "").ToUpperInvariant();
                    }
                }

                // Decrypt LM hash
                var lmHashEncrypted = ColumnExtractor.GetBinary(
                    session, table, columnDict, NtdsColumnNames.LmHash);
                if (lmHashEncrypted != null && lmHashEncrypted.Length >= 24)
                {
                    var lmHashDecrypted = DecryptHash(lmHashEncrypted, peks, GetRid(computer.ObjectSid));
                    if (lmHashDecrypted != null && lmHashDecrypted.Length >= 16)
                    {
                        computer.PasswordHashes ??= new PasswordHashes();
                        computer.PasswordHashes.LmHash = BitConverter.ToString(lmHashDecrypted, 0, 16).Replace("-", "").ToUpperInvariant();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[!] Error decrypting hashes for computer {computer.SamAccountName}: {ex.Message}");
            }
        }

        private static void DecryptHashesForUser(Session session, JET_TABLEID table,
            IDictionary<string, JET_COLUMNID> columnDict, User user, IReadOnlyList<byte[]> peks)
        {
            try
            {
                // Decrypt NT hash
                var ntHashEncrypted = ColumnExtractor.GetBinary(
                    session, table, columnDict, NtdsColumnNames.NtHash);
                if (ntHashEncrypted != null && ntHashEncrypted.Length >= 24)
                {
                    var ntHashDecrypted = DecryptHash(ntHashEncrypted, peks, GetRid(user.ObjectSid));
                    if (ntHashDecrypted != null && ntHashDecrypted.Length >= 16)
                    {
                        user.PasswordHashes ??= new PasswordHashes();
                        user.PasswordHashes.NtHash = BitConverter.ToString(ntHashDecrypted, 0, 16).Replace("-", "").ToUpperInvariant();
                    }
                }

                // Decrypt LM hash
                var lmHashEncrypted = ColumnExtractor.GetBinary(
                    session, table, columnDict, NtdsColumnNames.LmHash);
                if (lmHashEncrypted != null && lmHashEncrypted.Length >= 24)
                {
                    var lmHashDecrypted = DecryptHash(lmHashEncrypted, peks, GetRid(user.ObjectSid));
                    if (lmHashDecrypted != null && lmHashDecrypted.Length >= 16)
                    {
                        user.PasswordHashes ??= new PasswordHashes();
                        user.PasswordHashes.LmHash = BitConverter.ToString(lmHashDecrypted, 0, 16).Replace("-", "").ToUpperInvariant();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[!] Error decrypting hashes for user {user.SamAccountName}: {ex.Message}");
            }
        }

        private static void DecryptUserHashes(Session session, JET_DBID dbid, List<User> users, IReadOnlyList<byte[]> peks)
        {
            if (users == null || users.Count == 0)
                return;

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
                        DecryptHashesForUser(session, table, columnDict, user, peks);
                    }
                    recordId++;
                }

                Api.JetResetTableSequential(session, table, ResetTableSequentialGrbit.None);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[!] Error decrypting user hashes: {ex.Message}");
            }
        }
    }
}
