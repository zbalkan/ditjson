using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Isam.Esent.Interop;
using ditjson.Models;

namespace ditjson.Extractors
{
    internal static class PasswordHistoryExtractor
    {
        // Attribute ID for password history
        private const int PWD_HISTORY_ATTR = 589986;  // ATTx589986

        internal static void ExtractPasswordHistory(Session session, JET_DBID dbid, List<User> users, byte[] bootkey)
        {
            if (users == null || users.Count == 0 || bootkey == null || bootkey.Length == 0)
                return;

            Console.WriteLine("[*] Extracting password history...");

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
                            ExtractHistoryForUser(session, table, columnDict, user, bootkey);
                        }
                        recordId++;
                    }

                    Api.JetResetTableSequential(session, table, ResetTableSequentialGrbit.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Error extracting password history: {ex.Message}");
            }
        }

        private static void ExtractHistoryForUser(Session session, JET_TABLEID table,
            IDictionary<string, JET_COLUMNID> columnDict, User user, byte[] bootkey)
        {
            try
            {
                var pwdHistoryData = ColumnExtractor.GetBinary(session, table, columnDict, "ATTx589986");
                if (pwdHistoryData == null || pwdHistoryData.Length == 0)
                    return;

                var hashes = ParsePasswordHistory(pwdHistoryData, bootkey);
                if (hashes != null && hashes.Count > 0)
                {
                    user.PasswordHistory = hashes;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Error extracting password history for user {user.SamAccountName}: {ex.Message}");
            }
        }

        private static List<string> ParsePasswordHistory(byte[] data, byte[] bootkey)
        {
            var hashes = new List<string>();

            try
            {
                // Password history format: series of 16-byte NT hashes or 24-byte encrypted hashes
                // Read in chunks and decrypt each hash
                if (data.Length < 16)
                    return hashes;

                var offset = 0;

                // Skip version/reserved bytes if present
                if (data.Length > 4)
                {
                    var possibleVersion = BitConverter.ToInt32(data, 0);
                    // If first 4 bytes look like a version number, skip them
                    if (possibleVersion < 256)
                        offset = 4;
                }

                // Process 16-byte NT hashes (or 24-byte if encrypted with salt)
                while (offset + 16 <= data.Length)
                {
                    var hashData = new byte[16];
                    Array.Copy(data, offset, hashData, 0, 16);

                    // Try to decrypt if it looks like it might be encrypted
                    if (offset + 24 <= data.Length && looksEncrypted(data, offset))
                    {
                        var encrypted = new byte[24];
                        Array.Copy(data, offset, encrypted, 0, 24);
                        var decrypted = RegistryDecryptor.DecryptHash(encrypted, bootkey);
                        if (decrypted != null && decrypted.Length >= 16)
                        {
                            hashData = new byte[16];
                            Array.Copy(decrypted, 0, hashData, 0, 16);
                            offset += 24;
                        }
                        else
                        {
                            offset += 16;
                        }
                    }
                    else
                    {
                        offset += 16;
                    }

                    // Check if hash is all zeros (indicate no password)
                    if (!isZeroHash(hashData))
                    {
                        var hashHex = BitConverter.ToString(hashData).Replace("-", "").ToUpperInvariant();
                        hashes.Add(hashHex);
                    }
                }

                return hashes;
            }
            catch
            {
                return hashes;
            }
        }

        private static bool looksEncrypted(byte[] data, int offset)
        {
            // Encrypted hashes have salt prefix (8 bytes) followed by ciphertext
            // Look for entropy patterns typical of encrypted data
            if (offset + 24 > data.Length)
                return false;

            // Sample first 8 bytes (salt) - should have some entropy
            var salt = new byte[8];
            Array.Copy(data, offset, salt, 0, 8);

            var uniqueBytes = salt.Distinct().Count();
            return uniqueBytes > 2; // Real salt should have decent entropy
        }

        private static bool isZeroHash(byte[] hash)
        {
            if (hash == null || hash.Length < 16)
                return true;

            for (var i = 0; i < 16; i++)
            {
                if (hash[i] != 0)
                    return false;
            }

            return true;
        }
    }
}
