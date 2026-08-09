using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Isam.Esent.Interop;
using ditjson.Models;

namespace ditjson.Extractors
{
    internal static class SupplementalCredentialsParser
    {
        // Attribute ID for supplementalCredentials
        private const int SUPPLEMENTAL_CREDENTIALS_ATTR = 589985;  // ATTr589985

        internal static void ParseSupplementalCredentials(Session session, JET_DBID dbid, List<User> users, List<Computer> computers)
        {
            Console.WriteLine("[*] Parsing supplemental credentials...");
            ParseUserCredentials(session, dbid, users);
            ParseComputerCredentials(session, dbid, computers);
        }

        private static void ParseUserCredentials(Session session, JET_DBID dbid, List<User> users)
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
                        ParseCredentialsForUser(session, table, columnDict, user);
                    }
                    recordId++;
                }

                Api.JetResetTableSequential(session, table, ResetTableSequentialGrbit.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Error parsing user credentials: {ex.Message}");
            }
        }

        private static void ParseComputerCredentials(Session session, JET_DBID dbid, List<Computer> computers)
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
                        ParseCredentialsForComputer(session, table, columnDict, computer);
                    }
                    recordId++;
                }

                Api.JetResetTableSequential(session, table, ResetTableSequentialGrbit.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Error parsing computer credentials: {ex.Message}");
            }
        }

        private static void ParseCredentialsForUser(Session session, JET_TABLEID table,
            IDictionary<string, JET_COLUMNID> columnDict, User user)
        {
            try
            {
                var supCredData = ColumnExtractor.GetBinary(session, table, columnDict, "ATTx589985");
                if (supCredData == null || supCredData.Length == 0)
                    return;

                var (cleartext, kerberosKeys) = ParseSupplementalCredentialsBlob(supCredData);

                if (!string.IsNullOrEmpty(cleartext) || (kerberosKeys != null && kerberosKeys.Count > 0))
                {
                    user.SupplementalCredentials ??= new SupplementalCredentials();

                    if (!string.IsNullOrEmpty(cleartext))
                        user.SupplementalCredentials.ClearTextPassword = cleartext;

                    if (kerberosKeys != null && kerberosKeys.Count > 0)
                        user.SupplementalCredentials.KerberosKeys = kerberosKeys;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Error parsing supplemental credentials for user {user.SamAccountName}: {ex.Message}");
            }
        }

        private static void ParseCredentialsForComputer(Session session, JET_TABLEID table,
            IDictionary<string, JET_COLUMNID> columnDict, Computer computer)
        {
            try
            {
                var supCredData = ColumnExtractor.GetBinary(session, table, columnDict, "ATTx589985");
                if (supCredData == null || supCredData.Length == 0)
                    return;

                var (cleartext, kerberosKeys) = ParseSupplementalCredentialsBlob(supCredData);

                if (!string.IsNullOrEmpty(cleartext) || (kerberosKeys != null && kerberosKeys.Count > 0))
                {
                    computer.SupplementalCredentials ??= new SupplementalCredentials();

                    if (!string.IsNullOrEmpty(cleartext))
                        computer.SupplementalCredentials.ClearTextPassword = cleartext;

                    if (kerberosKeys != null && kerberosKeys.Count > 0)
                        computer.SupplementalCredentials.KerberosKeys = kerberosKeys;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Error parsing supplemental credentials for computer {computer.SamAccountName}: {ex.Message}");
            }
        }

        private static (string? cleartext, List<KerberosKey>? keys) ParseSupplementalCredentialsBlob(byte[] data)
        {
            string? cleartext = null;
            var keys = new List<KerberosKey>();

            try
            {
                // Skip version and reserved bytes
                if (data.Length < 4)
                    return (null, null);

                var offset = 4;
                var flags = BitConverter.ToInt32(data, offset);
                offset += 4;

                // Parse property entries
                while (offset + 10 <= data.Length)
                {
                    var reserved = BitConverter.ToInt32(data, offset);
                    offset += 4;
                    var type = BitConverter.ToInt32(data, offset);
                    offset += 4;
                    var length = BitConverter.ToInt32(data, offset);
                    offset += 4;

                    if (offset + length > data.Length)
                        break;

                    var value = new byte[length];
                    Array.Copy(data, offset, value, 0, length);
                    offset += length;

                    // Type 2 = Cleartext password (unicode string)
                    if (type == 2 && length > 0)
                    {
                        try
                        {
                            cleartext = System.Text.Encoding.Unicode.GetString(value).TrimEnd('\0');
                        }
                        catch { }
                    }
                    // Type 3 = Kerberos keys
                    else if (type == 3)
                    {
                        var keyList = ParseKerberosKeysFromBlob(value);
                        if (keyList != null && keyList.Count > 0)
                            keys.AddRange(keyList);
                    }
                }

                return (cleartext, keys.Count > 0 ? keys : null);
            }
            catch
            {
                return (null, null);
            }
        }

        private static List<KerberosKey> ParseKerberosKeysFromBlob(byte[] data)
        {
            var keys = new List<KerberosKey>();

            try
            {
                if (data.Length < 4)
                    return keys;

                var offset = 0;
                var keyCount = BitConverter.ToInt32(data, offset);
                offset += 4;

                for (var i = 0; i < keyCount && offset + 12 <= data.Length; i++)
                {
                    int reserved = BitConverter.ToInt16(data, offset);
                    offset += 2;
                    int keyType = BitConverter.ToInt16(data, offset);
                    offset += 2;
                    var keyLength = BitConverter.ToInt32(data, offset);
                    offset += 4;
                    var keyOffset = BitConverter.ToInt32(data, offset);
                    offset += 4;

                    if (keyOffset + keyLength <= data.Length)
                    {
                        var keyData = new byte[keyLength];
                        Array.Copy(data, keyOffset, keyData, 0, keyLength);

                        var algorithm = GetKerberosAlgorithmName(keyType);
                        var keyHex = BitConverter.ToString(keyData).Replace("-", "").ToUpperInvariant();

                        keys.Add(new KerberosKey
                        {
                            Algorithm = algorithm,
                            Key = keyHex
                        });
                    }
                }

                return keys;
            }
            catch
            {
                return keys;
            }
        }

        private static string GetKerberosAlgorithmName(int keyType) => keyType switch
        {
            1 => "DES_CBC_MD5",
            3 => "DES_CBC_MD5",
            17 => "AES128_CTS_HMAC_SHA1_96",
            18 => "AES256_CTS_HMAC_SHA1_96",
            23 => "RC4_HMAC_MD5",
            _ => $"UNKNOWN({keyType})"
        };
    }
}
