using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Isam.Esent.Interop;
using ditjson.Models;

namespace ditjson.Extractors
{
    internal static class PasswordHashDecryptor
    {
        // Attribute IDs for password hash fields
        private const int DESHASHED_ATTR = 0x90090 + 589824;      // dBCSPwd (LM hash)
        private const int NTHASHED_ATTR = 0x90091 + 589824;       // unicodePwd (NT hash)
        private const int DSRM_HASH_KEY_ATTR = 0x90096 + 589824;  // supplementalCredentials

        internal static void DecryptPasswordHashes(Session session, JET_DBID dbid, List<User> users, List<Computer> computers,
            string systemHivePath)
        {
            var bootkey = RegistryDecryptor.ExtractBootkey(systemHivePath);
            if (bootkey == null || bootkey.Length == 0)
            {
                Console.WriteLine("[!] Failed to extract bootkey from SYSTEM hive");
                return;
            }

            Console.WriteLine("[*] Decrypting password hashes...");
            DecryptUserHashes(session, dbid, users, bootkey);
            DecryptComputerHashes(session, dbid, computers, bootkey);
        }

        private static void DecryptUserHashes(Session session, JET_DBID dbid, List<User> users, byte[] bootkey)
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
                    if (userDict.TryGetValue(recordId, out var user))
                    {
                        DecryptHashesForUser(session, table, columnDict, user, bootkey);
                    }
                    recordId++;
                }

                Api.JetResetTableSequential(session, table, ResetTableSequentialGrbit.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Error decrypting user hashes: {ex.Message}");
            }
        }

        private static void DecryptComputerHashes(Session session, JET_DBID dbid, List<Computer> computers, byte[] bootkey)
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
                    if (computerDict.TryGetValue(recordId, out var computer))
                    {
                        DecryptHashesForComputer(session, table, columnDict, computer, bootkey);
                    }
                    recordId++;
                }

                Api.JetResetTableSequential(session, table, ResetTableSequentialGrbit.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Error decrypting computer hashes: {ex.Message}");
            }
        }

        private static void DecryptHashesForUser(Session session, JET_TABLEID table,
            IDictionary<string, JET_COLUMNID> columnDict, User user, byte[] bootkey)
        {
            try
            {
                // Decrypt NT hash
                var ntHashEncrypted = ColumnExtractor.GetBinary(session, table, columnDict, "ATTp589920");
                if (ntHashEncrypted != null && ntHashEncrypted.Length >= 24)
                {
                    var ntHashDecrypted = DecryptHash(ntHashEncrypted, bootkey);
                    if (ntHashDecrypted != null && ntHashDecrypted.Length >= 16)
                    {
                        user.PasswordHashes ??= new PasswordHashes();
                        user.PasswordHashes.NtHash = BitConverter.ToString(ntHashDecrypted, 0, 16).Replace("-", "").ToUpperInvariant();
                    }
                }

                // Decrypt LM hash
                var lmHashEncrypted = ColumnExtractor.GetBinary(session, table, columnDict, "ATTp589919");
                if (lmHashEncrypted != null && lmHashEncrypted.Length >= 24)
                {
                    var lmHashDecrypted = DecryptHash(lmHashEncrypted, bootkey);
                    if (lmHashDecrypted != null && lmHashDecrypted.Length >= 16)
                    {
                        user.PasswordHashes ??= new PasswordHashes();
                        user.PasswordHashes.LmHash = BitConverter.ToString(lmHashDecrypted, 0, 16).Replace("-", "").ToUpperInvariant();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Error decrypting hashes for user {user.SamAccountName}: {ex.Message}");
            }
        }

        private static void DecryptHashesForComputer(Session session, JET_TABLEID table,
            IDictionary<string, JET_COLUMNID> columnDict, Computer computer, byte[] bootkey)
        {
            try
            {
                // Decrypt NT hash
                var ntHashEncrypted = ColumnExtractor.GetBinary(session, table, columnDict, "ATTp589920");
                if (ntHashEncrypted != null && ntHashEncrypted.Length >= 24)
                {
                    var ntHashDecrypted = DecryptHash(ntHashEncrypted, bootkey);
                    if (ntHashDecrypted != null && ntHashDecrypted.Length >= 16)
                    {
                        computer.PasswordHashes ??= new PasswordHashes();
                        computer.PasswordHashes.NtHash = BitConverter.ToString(ntHashDecrypted, 0, 16).Replace("-", "").ToUpperInvariant();
                    }
                }

                // Decrypt LM hash
                var lmHashEncrypted = ColumnExtractor.GetBinary(session, table, columnDict, "ATTp589919");
                if (lmHashEncrypted != null && lmHashEncrypted.Length >= 24)
                {
                    var lmHashDecrypted = DecryptHash(lmHashEncrypted, bootkey);
                    if (lmHashDecrypted != null && lmHashDecrypted.Length >= 16)
                    {
                        computer.PasswordHashes ??= new PasswordHashes();
                        computer.PasswordHashes.LmHash = BitConverter.ToString(lmHashDecrypted, 0, 16).Replace("-", "").ToUpperInvariant();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Error decrypting hashes for computer {computer.SamAccountName}: {ex.Message}");
            }
        }

        private static byte[]? DecryptHash(byte[] encryptedHash, byte[] bootkey)
        {
            if (encryptedHash == null || encryptedHash.Length < 24 || bootkey == null || bootkey.Length == 0)
                return null;

            try
            {
                // Skip the first 8 bytes (salt), decrypt the remaining bytes
                var ciphertext = new byte[encryptedHash.Length - 8];
                Array.Copy(encryptedHash, 8, ciphertext, 0, ciphertext.Length);

                return RegistryDecryptor.DecryptHash(ciphertext, bootkey);
            }
            catch
            {
                return null;
            }
        }
    }
}
