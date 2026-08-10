using ditjson.Extractors;
using System.Security.Cryptography;

namespace ditjson.Tests.Extractors;

[TestClass]
public class CredentialCryptoTests
{
    [TestMethod]
    public void TransformKey_MatchesMsSamrVector()
    {
        Assert.AreSequenceEqual(Convert.FromHexString("008080604028180E"), CredentialCrypto.TransformKey(Convert.FromHexString("01020304050607")));
    }

    [TestMethod]
    public void DeriveRidKeys_UsesLittleEndianShuffle()
    {
        var (first, second) = CredentialCrypto.DeriveRidKeys(500);
        Assert.AreSequenceEqual(Convert.FromHexString("F40040000EA00400"), first);
        Assert.AreSequenceEqual(Convert.FromHexString("007A00200006D002"), second);
    }

    [TestMethod]
    public void PasswordHistory_OmitsCurrentHashChunk()
    {
        const uint rid = 500;
        var pek = Convert.FromHexString("00112233445566778899AABBCCDDEEFF");
        var material = Convert.FromHexString("102132435465768798A9BACBDCEDFE0F");
        var current = Convert.FromHexString("11111111111111111111111111111111");
        var historical = Convert.FromHexString("22222222222222222222222222222222");
        var ridEncrypted = AddRidDesLayer(current.Concat(historical).ToArray(), rid);
        using var md5 = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
        md5.AppendData(pek);
        md5.AppendData(material);
        var cipher = new RC4(md5.GetHashAndReset()).Decrypt(ridEncrypted);
        var blob = new byte[24 + cipher.Length];
        material.CopyTo(blob, 8);
        cipher.CopyTo(blob, 24);

        var result = PasswordHistoryExtractor.ParsePasswordHistory(blob, new[] { pek }, rid);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(Convert.ToHexString(historical), result[0]);
    }

    private static byte[] AddRidDesLayer(byte[] plain, uint rid)
    {
        var (first, second) = CredentialCrypto.DeriveRidKeys(rid);
        var result = new byte[plain.Length];
        for (var offset = 0; offset < plain.Length; offset += 16)
        {
            EncryptDes(first, plain, offset, result, offset);
            EncryptDes(second, plain, offset + 8, result, offset + 8);
        }
        return result;
    }

    private static void EncryptDes(byte[] key, byte[] input, int inputOffset, byte[] output, int outputOffset)
    {
        using var des = DES.Create();
        des.Key = key;
        des.Mode = CipherMode.ECB;
        des.Padding = PaddingMode.None;
        var block = des.CreateEncryptor().TransformFinalBlock(input, inputOffset, 8);
        block.CopyTo(output, outputOffset);
    }
}
