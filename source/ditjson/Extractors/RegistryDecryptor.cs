using System;
using System.IO;

namespace ditjson.Extractors
{
    internal static class RegistryDecryptor
    {
        private static readonly int[] BootkeyTransform = { 8, 5, 4, 2, 11, 9, 13, 3, 0, 6, 1, 12, 14, 10, 15, 7 };

        internal static byte[]? ExtractBootkey(string systemHivePath)
        {
            if (!File.Exists(systemHivePath)) return null;
            try
            {
                using var hive = new RegistryHive(systemHivePath);
                var currentBytes = hive.ReadValue(hive.OpenKey("Select"), "Current");
                if (currentBytes == null || currentBytes.Length < 4) return null;
                var controlSet = $"ControlSet{BitConverter.ToInt32(currentBytes, 0):D3}";
                var hex = string.Empty;
                foreach (var name in new[] { "JD", "Skew1", "GBG", "Data" })
                    hex += hive.ReadClassName(hive.OpenKey($"{controlSet}\\Control\\Lsa\\{name}"));
                if (hex.Length != 32) throw new InvalidDataException("LSA class names do not contain a 16-byte boot key");
                var scrambled = Convert.FromHexString(hex);
                var bootkey = new byte[16];
                for (var i = 0; i < bootkey.Length; i++) bootkey[i] = scrambled[BootkeyTransform[i]];
                return bootkey;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[!] Failed to extract bootkey from SYSTEM hive: {ex.Message}");
                return null;
            }
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
