using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ditjson.Models;
using Microsoft.Isam.Esent.Interop;

namespace ditjson.Extractors
{
    internal static class SupplementalCredentialsParser
    {
        // USER_PROPERTIES has four reserved scalar fields followed by a
        // 96-byte reserved buffer, a signature, and the property count.
        private const int UserPropertiesFixedHeaderLength = 110;
        private const int PropertyCountOffset = 110;

        internal static void ParseSupplementalCredentials(Session session, JET_DBID dbid, List<User> users, List<Computer> computers, IReadOnlyList<byte[]> peks)
        {
            Console.Error.WriteLine("[*] Parsing supplemental credentials...");
            ParseUserCredentials(session, dbid, users, peks);
            ParseComputerCredentials(session, dbid, computers, peks);
        }

        private static string GetKerberosAlgorithmName(int keyType) => keyType switch
        {
            1 => "DES_CBC_CRC",
            3 => "DES_CBC_MD5",
            17 => "AES128_CTS_HMAC_SHA1_96",
            18 => "AES256_CTS_HMAC_SHA1_96",
            23 => "RC4_HMAC_MD5",
            unchecked((int)0xffffff74) => "RC4_HMAC_MD5",
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
                Console.Error.WriteLine($"[!] Error parsing computer credentials: {ex.GetType().Name}: {ex.Message}");
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

                if (!string.IsNullOrEmpty(cleartext) || (kerberosKeys?.Count > 0))
                {
                    computer.SupplementalCredentials ??= new SupplementalCredentials();

                    if (!string.IsNullOrEmpty(cleartext))
                    {
                        computer.SupplementalCredentials.ClearTextPassword = cleartext;
                    }

                    if (kerberosKeys?.Count > 0)
                    {
                        computer.SupplementalCredentials.KerberosKeys = kerberosKeys;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[!] Error parsing supplemental credentials for computer {computer.SamAccountName}: {ex.GetType().Name}: {ex.Message}");
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

                if (!string.IsNullOrEmpty(cleartext) || (kerberosKeys?.Count > 0))
                {
                    user.SupplementalCredentials ??= new SupplementalCredentials();

                    if (!string.IsNullOrEmpty(cleartext))
                    {
                        user.SupplementalCredentials.ClearTextPassword = cleartext;
                    }

                    if (kerberosKeys?.Count > 0)
                    {
                        user.SupplementalCredentials.KerberosKeys = kerberosKeys;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[!] Error parsing supplemental credentials for user {user.SamAccountName}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private static List<KerberosKey> ParseKerberosKeysFromBlob(byte[] data)
        {
            var keys = new List<KerberosKey>();
            if (data.Length < 24)
            {
                return keys;
            }

            var keyCount = BitConverter.ToUInt16(data, 4);
            var offset = 24;
            for (var i = 0; i < keyCount; i++)
            {
                if (offset + 24 > data.Length)
                {
                    throw new InvalidDataException("Truncated KERB_KEY_DATA_NEW entry");
                }
                var keyType = BitConverter.ToInt32(data, offset + 12);
                var keyLength = BitConverter.ToInt32(data, offset + 16);
                var keyOffset = BitConverter.ToInt32(data, offset + 20);
                offset += 24;
                if (keyOffset < 0 || keyLength < 0 || keyOffset > data.Length - keyLength)
                {
                    throw new InvalidDataException("Invalid Kerberos key range");
                }
                keys.Add(new KerberosKey {
                    Algorithm = GetKerberosAlgorithmName(keyType),
                    Key = Convert.ToHexString(data.AsSpan(keyOffset, keyLength))
                });
            }
            return keys;
        }

        internal static (string? cleartext, List<KerberosKey>? keys) ParseSupplementalCredentialsBlob(byte[] data)
        {
            string? cleartext = null;
            var keys = new List<KerberosKey>();
            if (data.Length < UserPropertiesFixedHeaderLength + 1)
            {
                return (null, null);
            }

            var length = BitConverter.ToUInt32(data, 4);
            var propertiesEnd = 12L + length;
            if (propertiesEnd < UserPropertiesFixedHeaderLength || propertiesEnd >= data.Length)
            {
                throw new InvalidDataException("Invalid USER_PROPERTIES length");
            }
            if (BitConverter.ToUInt16(data, 108) != 0x50)
            {
                throw new InvalidDataException("Invalid USER_PROPERTIES signature");
            }
            // A header-only structure omits PropertyCount.
            if (propertiesEnd == UserPropertiesFixedHeaderLength)
            {
                return (null, null);
            }
            if (propertiesEnd < PropertyCountOffset + 2)
            {
                throw new InvalidDataException("USER_PROPERTIES omits its property count");
            }

            var propertyCount = BitConverter.ToUInt16(data, PropertyCountOffset);
            var offset = PropertyCountOffset + 2;
            for (var property = 0; property < propertyCount; property++)
            {
                if (offset + 6 > propertiesEnd)
                {
                    throw new InvalidDataException("Truncated USER_PROPERTY header");
                }
                var nameLength = BitConverter.ToUInt16(data, offset);
                var valueLength = BitConverter.ToUInt16(data, offset + 2);
                offset += 6;
                if (offset + (long)nameLength + valueLength > propertiesEnd)
                {
                    throw new InvalidDataException("Truncated USER_PROPERTY value");
                }

                var name = Encoding.Unicode.GetString(data, offset, nameLength).TrimEnd('\0');
                offset += nameLength;
                var value = Convert.FromHexString(Encoding.ASCII.GetString(data, offset, valueLength));
                offset += valueLength;
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
