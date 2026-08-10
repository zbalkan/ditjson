using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ditjson.Models;
using Microsoft.Isam.Esent.Interop;

namespace ditjson.Extractors
{
    internal static class SupplementalCredentialsParser
    {
        internal static void ParseSupplementalCredentials(Session session, JET_DBID dbid, List<User> users, List<Computer> computers, IReadOnlyList<byte[]> peks)
        {
            Console.Error.WriteLine("[*] Parsing supplemental credentials...");
            ParseUserCredentials(session, dbid, users, peks);
            ParseComputerCredentials(session, dbid, computers, peks);
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

        private static void ParseComputerCredentials(Session session, JET_DBID dbid, List<Computer> computers, IReadOnlyList<byte[]> peks)
        {
            if (computers == null || computers.Count == 0)
            {
                return;
            }

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
                        ParseCredentialsForComputer(session, table, columnDict, computer, peks);
                    }
                    recordId++;
                }

                Api.JetResetTableSequential(session, table, ResetTableSequentialGrbit.None);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[!] Error parsing computer credentials: {ex.Message}");
            }
        }

        private static void ParseCredentialsForComputer(Session session, JET_TABLEID table,
            IDictionary<string, JET_COLUMNID> columnDict, Computer computer, IReadOnlyList<byte[]> peks)
        {
            try
            {
                var supCredData = ColumnExtractor.GetBinary(
                    session, table, columnDict, NtdsColumnNames.SupplementalCredentials);
                if (supCredData == null || supCredData.Length == 0)
                {
                    return;
                }

                var (cleartext, kerberosKeys) = ParseSupplementalCredentialsBlob(CredentialCrypto.UnwrapAttribute(supCredData, peks));

                if (!string.IsNullOrEmpty(cleartext) || (kerberosKeys != null && kerberosKeys.Count > 0))
                {
                    computer.SupplementalCredentials ??= new SupplementalCredentials();

                    if (!string.IsNullOrEmpty(cleartext))
                    {
                        computer.SupplementalCredentials.ClearTextPassword = cleartext;
                    }

                    if (kerberosKeys != null && kerberosKeys.Count > 0)
                    {
                        computer.SupplementalCredentials.KerberosKeys = kerberosKeys;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[!] Error parsing supplemental credentials for computer {computer.SamAccountName}: {ex.Message}");
            }
        }

        private static void ParseCredentialsForUser(Session session, JET_TABLEID table,
            IDictionary<string, JET_COLUMNID> columnDict, User user, IReadOnlyList<byte[]> peks)
        {
            try
            {
                var supCredData = ColumnExtractor.GetBinary(
                    session, table, columnDict, NtdsColumnNames.SupplementalCredentials);
                if (supCredData == null || supCredData.Length == 0)
                {
                    return;
                }

                var (cleartext, kerberosKeys) = ParseSupplementalCredentialsBlob(CredentialCrypto.UnwrapAttribute(supCredData, peks));

                if (!string.IsNullOrEmpty(cleartext) || (kerberosKeys != null && kerberosKeys.Count > 0))
                {
                    user.SupplementalCredentials ??= new SupplementalCredentials();

                    if (!string.IsNullOrEmpty(cleartext))
                    {
                        user.SupplementalCredentials.ClearTextPassword = cleartext;
                    }

                    if (kerberosKeys != null && kerberosKeys.Count > 0)
                    {
                        user.SupplementalCredentials.KerberosKeys = kerberosKeys;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[!] Error parsing supplemental credentials for user {user.SamAccountName}: {ex.Message}");
            }
        }

        private static List<KerberosKey> ParseKerberosKeysFromBlob(byte[] data)
        {
            var keys = new List<KerberosKey>();
            try
            {
                if (data.Length < 24)
                {
                    return keys;
                }

                var keyCount = BitConverter.ToUInt16(data, 4);
                var offset = 24;
                for (var i = 0; i < keyCount && offset + 24 <= data.Length; i++)
                {
                    var keyType = BitConverter.ToInt32(data, offset + 12);
                    var keyLength = BitConverter.ToInt32(data, offset + 16);
                    var keyOffset = BitConverter.ToInt32(data, offset + 20);
                    offset += 24;
                    if (keyOffset >= 0 && keyLength >= 0 && keyOffset + keyLength <= data.Length)
                    {
                        var keyData = new byte[keyLength];
                        Array.Copy(data, keyOffset, keyData, 0, keyLength);
                        keys.Add(new KerberosKey { Algorithm = GetKerberosAlgorithmName(keyType), Key = Convert.ToHexString(keyData) });
                    }
                }
                return keys;
            }
            catch { return keys; }
        }

        internal static (string? cleartext, List<KerberosKey>? keys) ParseSupplementalCredentialsBlob(byte[] data)
        {
            string? cleartext = null;
            var keys = new List<KerberosKey>();
            try
            {
                if (data.Length < 16)
                {
                    return (null, null);
                }

                var propertyCount = BitConverter.ToUInt16(data, 14);
                var offset = 16;
                for (var property = 0; property < propertyCount && offset + 6 <= data.Length; property++)
                {
                    var nameLength = BitConverter.ToUInt16(data, offset);
                    var valueLength = BitConverter.ToUInt16(data, offset + 2);
                    offset += 6;
                    if (offset + nameLength + valueLength > data.Length)
                    {
                        break;
                    }

                    var name = Encoding.Unicode.GetString(data, offset, nameLength).TrimEnd('\0');
                    offset += nameLength;
                    var encodedValue = Encoding.ASCII.GetString(data, offset, valueLength);
                    offset += valueLength;
                    byte[] value;
                    try { value = Convert.FromHexString(encodedValue); } catch (FormatException) { continue; }
                    if (name == "Primary:CLEARTEXT" && value.Length > 0)
                    {
                        try { cleartext = new UnicodeEncoding(false, false, true).GetString(value).TrimEnd('\0'); }
                        catch (DecoderFallbackException) { cleartext = Convert.ToHexString(value); }
                    }
                    else if (name == "Primary:Kerberos-Newer-Keys")
                    {
                        keys.AddRange(ParseKerberosKeysFromBlob(value));
                    }
                }
                return (cleartext, keys.Count > 0 ? keys : null);
            }
            catch { return (null, null); }
        }

        private static void ParseUserCredentials(Session session, JET_DBID dbid, List<User> users, IReadOnlyList<byte[]> peks)
        {
            if (users == null || users.Count == 0)
            {
                return;
            }

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
                        ParseCredentialsForUser(session, table, columnDict, user, peks);
                    }
                    recordId++;
                }

                Api.JetResetTableSequential(session, table, ResetTableSequentialGrbit.None);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[!] Error parsing user credentials: {ex.Message}");
            }
        }
    }
}
