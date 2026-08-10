using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace ditjson.Extractors
{
    /// NTDS PEK and RID keyed cryptography shared by all credential attributes.
    internal static class CredentialCrypto
    {
        internal static List<byte[]> DecryptPekList(byte[] blob, byte[] bootkey)
        {
            if (blob.Length < 24 || bootkey.Length != 16)
            {
                throw new InvalidDataException("Invalid encrypted PEK list");
            }

            var version = BitConverter.ToUInt32(blob, 0);
            var material = blob.AsSpan(8, 16).ToArray();
            var cipher = blob.AsSpan(24).ToArray();
            byte[] plain;
            if (version == 2)
            {
                using var md5 = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
                md5.AppendData(bootkey);
                for (var i = 0; i < 1000; i++)
                {
                    md5.AppendData(material);
                }

                plain = new RC4(md5.GetHashAndReset()).Decrypt(cipher);
            }
            else if (version == 3)
            {
                plain = AesDecrypt(bootkey, material, cipher);
            }
            else
            {
                throw new InvalidDataException($"Unsupported PEK list version {version}");
            }

            if (plain.Length < 52)
            {
                throw new InvalidDataException("Truncated plaintext PEK list");
            }

            var keys = new List<byte[]>();
            for (var offset = 32; offset + 20 <= plain.Length; offset += 20)
            {
                var index = version == 2 ? plain[offset] : BitConverter.ToInt32(plain, offset);
                if (index != keys.Count)
                {
                    break;
                }

                keys.Add(plain.AsSpan(offset + 4, 16).ToArray());
            }
            if (keys.Count == 0)
            {
                throw new InvalidDataException("PEK list contains no keys");
            }

            return keys;
        }

        internal static byte[] UnwrapAttribute(byte[] blob, IReadOnlyList<byte[]> peks)
        {
            if (blob.Length < 24)
            {
                throw new InvalidDataException("Truncated encrypted credential attribute");
            }

            var version = BitConverter.ToUInt32(blob, 0);
            // The PEK index is the fifth byte of the eight-byte header.  The
            // remaining three bytes are header data, not part of the index.
            var index = blob[4];
            if (index >= peks.Count)
            {
                throw new InvalidDataException($"Unknown PEK index {index}");
            }

            var material = blob.AsSpan(8, 16).ToArray();
            if (version == 0x13)
            {
                // CRYPTED_HASHW16 places a four-byte field before the AES
                // ciphertext. Hash, history, and supplemental blobs all use
                // this layout for Windows Server 2016 encryption.
                if (blob.Length < 44)
                {
                    throw new InvalidDataException("Truncated AES credential attribute");
                }

                var aesCipher = blob.AsSpan(28).ToArray();
                return AesDecrypt(peks[(int)index], material, aesCipher);
            }

            var cipher = blob.AsSpan(24).ToArray();
            using var md5 = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
            md5.AppendData(peks[(int)index]);
            md5.AppendData(material);
            return new RC4(md5.GetHashAndReset()).Decrypt(cipher);
        }

        internal static byte[] RemoveRidDesLayer(byte[] encrypted, uint rid)
        {
            if (encrypted.Length % 16 != 0)
            {
                throw new InvalidDataException("RID-DES ciphertext must contain 16-byte hashes");
            }

            var (key1, key2) = DeriveRidKeys(rid);
            var result = new byte[encrypted.Length];
            for (var offset = 0; offset < encrypted.Length; offset += 16)
            {
                DesDecrypt(key1, encrypted, offset, result, offset);
                DesDecrypt(key2, encrypted, offset + 8, result, offset + 8);
            }
            return result;
        }

        internal static (byte[] first, byte[] second) DeriveRidKeys(uint rid)
        {
            var r = BitConverter.GetBytes(rid);
            return (TransformKey(new[] { r[0], r[1], r[2], r[3], r[0], r[1], r[2] }),
                TransformKey(new[] { r[3], r[0], r[1], r[2], r[3], r[0], r[1] }));
        }

        internal static byte[] TransformKey(byte[] key)
        {
            if (key.Length != 7)
            {
                throw new ArgumentException("A DES source key is exactly seven bytes", nameof(key));
            }

            var output = new byte[8];
            output[0] = (byte)(key[0] >> 1);
            output[1] = (byte)(((key[0] & 1) << 6) | (key[1] >> 2));
            output[2] = (byte)(((key[1] & 3) << 5) | (key[2] >> 3));
            output[3] = (byte)(((key[2] & 7) << 4) | (key[3] >> 4));
            output[4] = (byte)(((key[3] & 15) << 3) | (key[4] >> 5));
            output[5] = (byte)(((key[4] & 31) << 2) | (key[5] >> 6));
            output[6] = (byte)(((key[5] & 63) << 1) | (key[6] >> 7));
            output[7] = (byte)(key[6] & 127);
            for (var i = 0; i < output.Length; i++)
            {
                output[i] <<= 1;
            }

            return output;
        }

        private static byte[] AesDecrypt(byte[] key, byte[] iv, byte[] cipher)
        {
            if (cipher.Length == 0)
            {
                return Array.Empty<byte>();
            }

            // secretsdump's decryptAES zero-pads a short final ciphertext
            // block. NTDS blobs are normally aligned, but preserving that
            // behavior allows damaged/truncated final padding to be handled
            // without losing the preceding complete blocks.
            var alignedLength = (cipher.Length + 15) & ~15;
            var alignedCipher = cipher;
            if (alignedLength != cipher.Length)
            {
                alignedCipher = new byte[alignedLength];
                Buffer.BlockCopy(cipher, 0, alignedCipher, 0, cipher.Length);
            }
            using var aes = Aes.Create();
            aes.Key = key; aes.IV = iv; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.None;
            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(alignedCipher, 0, alignedCipher.Length);
        }

        private static void DesDecrypt(byte[] key, byte[] input, int inputOffset, byte[] output, int outputOffset)
        {
            using var des = DES.Create();
            des.Key = key; des.Mode = CipherMode.ECB; des.Padding = PaddingMode.None;
            using var decryptor = des.CreateDecryptor();
            var block = decryptor.TransformFinalBlock(input, inputOffset, 8);
            Buffer.BlockCopy(block, 0, output, outputOffset, 8);
        }
    }
}
