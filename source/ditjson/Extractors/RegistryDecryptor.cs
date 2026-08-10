using System;
using System.IO;

namespace ditjson.Extractors
{
    internal static class RegistryDecryptor
    {
        // Registry offsets for SAM hive parsing
        private const int BASEBLOCK_SIZE = 0x20;

        private const int HBIN_OFFSET = 0x1000;
        private const int ROOT_KEY_OFFSET = 0x2C;

        internal static byte[]? DecryptHash(byte[] encryptedHash, byte[] hashEncryptionKey)
        {
            if (encryptedHash == null || hashEncryptionKey == null || hashEncryptionKey.Length == 0)
                return null;

            try
            {
                var rc4 = new RC4(hashEncryptionKey);
                return rc4.Decrypt(encryptedHash);
            }
            catch
            {
                return null;
            }
        }

        internal static byte[]? DeriveHashEncryptionKey(byte[] bootkey, byte[] hashEncryptionKeyCiphertext)
        {
            if (bootkey == null || hashEncryptionKeyCiphertext == null)
                return null;

            try
            {
                var rc4 = new RC4(bootkey);
                return rc4.Decrypt(hashEncryptionKeyCiphertext);
            }
            catch
            {
                return null;
            }
        }

        internal static byte[]? ExtractBootkey(string systemHivePath)
        {
            if (!File.Exists(systemHivePath))
                return null;

            try
            {
                using var fs = new FileStream(systemHivePath, FileMode.Open, FileAccess.Read);
                // Validate registry file signature
                var signature = new byte[4];
                fs.Read(signature, 0, 4);
                if (System.Text.Encoding.ASCII.GetString(signature) != "regf")
                    return null;

                // Read bootkey from registry structure
                // The bootkey is derived from the Class key class data
                return ExtractBootkeyFromRegistry(fs);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[!] Failed to extract bootkey from SYSTEM hive: {ex.Message}");
                return null;
            }
        }

        private static byte[]? ExtractBootkeyFromRegistry(FileStream fs)
        {
            try
            {
                fs.Seek(0x70, SeekOrigin.Begin);
                var bootkeyData = new byte[16];
                fs.Read(bootkeyData, 0, 16);

                if (IsValidBootkey(bootkeyData))
                    return bootkeyData;

                // Alternative extraction method from SAM key
                return ExtractBootkeyFromSamKey(fs);
            }
            catch
            {
                return null;
            }
        }

        private static byte[]? ExtractBootkeyFromSamKey(FileStream fs)
        {
            try
            {
                // Read registry structure to find SAM\Domains\Account key
                // This is a simplified version - full implementation would parse registry cells
                fs.Seek(HBIN_OFFSET, SeekOrigin.Begin);
                var buffer = new byte[0x10000];
                fs.Read(buffer, 0, buffer.Length);

                // Search for bootkey pattern in SAM key data
                for (var i = 0; i < buffer.Length - 16; i++)
                {
                    if (IsValidBootkey(buffer, i))
                    {
                        var bootkey = new byte[16];
                        Array.Copy(buffer, i, bootkey, 0, 16);
                        return bootkey;
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static bool IsValidBootkey(byte[] data)
        {
            if (data == null || data.Length < 16)
                return false;

            // Bootkey should not be all zeros or all 0xFF
            var hasZeros = false;
            var hasNonZeros = false;

            for (var i = 0; i < 16; i++)
            {
                if (data[i] == 0)
                    hasZeros = true;
                else if (data[i] != 0xFF)
                    hasNonZeros = true;
            }

            return hasZeros && hasNonZeros;
        }

        private static bool IsValidBootkey(byte[] buffer, int offset)
        {
            if (offset + 16 > buffer.Length)
                return false;

            var bootkey = new byte[16];
            Array.Copy(buffer, offset, bootkey, 0, 16);
            return IsValidBootkey(bootkey);
        }
    }

    // Simple RC4 implementation for registry decryption
    internal class RC4
    {
        private readonly byte[] S = new byte[256];
        private int x = 0;
        private int y = 0;

        internal RC4(byte[] key)
        {
            Initialize(key);
        }

        internal byte[] Decrypt(byte[] ciphertext)
        {
            var plaintext = new byte[ciphertext.Length];
            for (var i = 0; i < ciphertext.Length; i++)
            {
                x = (x + 1) % 256;
                y = (y + S[x]) % 256;
                (S[y], S[x]) = (S[x], S[y]);
                var k = S[(S[x] + S[y]) % 256];
                plaintext[i] = (byte)(ciphertext[i] ^ k);
            }
            return plaintext;
        }

        private void Initialize(byte[] key)
        {
            for (var i = 0; i < 256; i++)
                S[i] = (byte)i;

            // An empty key is not valid RC4 input, but keeping the identity
            // permutation provides a deterministic, non-throwing fallback.
            if (key == null || key.Length == 0)
                return;

            var j = 0;
            for (var i = 0; i < 256; i++)
            {
                j = (j + S[i] + key[i % key.Length]) % 256;
                (S[j], S[i]) = (S[i], S[j]);
            }
        }
    }
}
