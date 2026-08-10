using System.Security.Cryptography;
using System.Text;
using ditjson.Decoders;
using ditjson.Extractors;

namespace ditjson.Tests.Extractors;

[TestClass]
public class CredentialStructureTests
{
    [TestMethod]
    public void ApplyBootkeyTransform_MatchesDocumentedPermutation()
    {
        var scrambled = Enumerable.Range(0, 16).Select(value => (byte)value).ToArray();

        Assert.AreSequenceEqual(
            Convert.FromHexString("080504020B090D030006010C0E0A0F07"),
            RegistryDecryptor.ApplyBootkeyTransform(scrambled));
    }

    [TestMethod]
    public void GetRid_UsesDecodedNtdsSidLastSubAuthority()
    {
        var sid = SidDecoder.DecodeNtds(
            Convert.FromHexString("010500000000000500000015BE04FEA6AED6D3BA1919A135000001F4"));

        Assert.AreEqual(500u, PasswordHashDecryptor.GetRid(sid));
    }

    [TestMethod]
    public void ParseSupplementalCredentialsBlob_ReadsKerberosNewerKeyOffsets()
    {
        const string name = "Primary:Kerberos-Newer-Keys";
        var nameBytes = Encoding.Unicode.GetBytes(name);
        var key = Convert.FromHexString("00112233445566778899AABBCCDDEEFF");
        var credential = new byte[48 + key.Length];
        BitConverter.GetBytes((ushort)1).CopyTo(credential, 4);
        BitConverter.GetBytes(18).CopyTo(credential, 24 + 12);
        BitConverter.GetBytes(key.Length).CopyTo(credential, 24 + 16);
        BitConverter.GetBytes(48).CopyTo(credential, 24 + 20);
        key.CopyTo(credential, 48);
        var valueBytes = Encoding.ASCII.GetBytes(Convert.ToHexString(credential));
        var blob = new byte[112 + 6 + nameBytes.Length + valueBytes.Length + 1];
        BitConverter.GetBytes((uint)(blob.Length - 13)).CopyTo(blob, 4);
        BitConverter.GetBytes((ushort)0x50).CopyTo(blob, 108);
        BitConverter.GetBytes((ushort)1).CopyTo(blob, 110);
        BitConverter.GetBytes((ushort)nameBytes.Length).CopyTo(blob, 112);
        BitConverter.GetBytes((ushort)valueBytes.Length).CopyTo(blob, 114);
        nameBytes.CopyTo(blob, 118);
        valueBytes.CopyTo(blob, 118 + nameBytes.Length);

        var (_, keys) = SupplementalCredentialsParser.ParseSupplementalCredentialsBlob(blob);

        Assert.IsNotNull(keys);
        Assert.AreEqual(1, keys.Count);
        Assert.AreEqual("AES256_CTS_HMAC_SHA1_96", keys[0].Algorithm);
        Assert.AreEqual(Convert.ToHexString(key), keys[0].Key);
    }

    [TestMethod]
    public void ParseSupplementalCredentialsBlob_ReadsUserPropertiesHeader()
    {
        const string name = "Primary:CLEARTEXT";
        var nameBytes = Encoding.Unicode.GetBytes(name);
        var cleartextBytes = Encoding.Unicode.GetBytes("Roadmap test");
        var valueBytes = Encoding.ASCII.GetBytes(Convert.ToHexString(cleartextBytes));
        var blob = new byte[112 + 6 + nameBytes.Length + valueBytes.Length + 1];
        BitConverter.GetBytes((uint)(blob.Length - 13)).CopyTo(blob, 4);
        BitConverter.GetBytes((ushort)0x50).CopyTo(blob, 108);
        BitConverter.GetBytes((ushort)1).CopyTo(blob, 110);
        BitConverter.GetBytes((ushort)nameBytes.Length).CopyTo(blob, 112);
        BitConverter.GetBytes((ushort)valueBytes.Length).CopyTo(blob, 114);
        nameBytes.CopyTo(blob, 118);
        valueBytes.CopyTo(blob, 118 + nameBytes.Length);

        var (cleartext, keys) = SupplementalCredentialsParser.ParseSupplementalCredentialsBlob(blob);

        Assert.AreEqual("Roadmap test", cleartext);
        Assert.IsNull(keys);
    }

    [TestMethod]
    public void UnwrapAttribute_AesW16SkipsUnknownField()
    {
        var pek = Convert.FromHexString("000102030405060708090A0B0C0D0E0F");
        var iv = Convert.FromHexString("101112131415161718191A1B1C1D1E1F");
        var expected = Convert.FromHexString("202122232425262728292A2B2C2D2E2F");
        byte[] cipher;
        using (var aes = Aes.Create())
        {
            aes.Key = pek;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;
            cipher = aes.CreateEncryptor().TransformFinalBlock(expected, 0, expected.Length);
        }

        var blob = new byte[28 + cipher.Length];
        BitConverter.GetBytes(0x13u).CopyTo(blob, 0);
        iv.CopyTo(blob, 8);
        Convert.FromHexString("AABBCCDD").CopyTo(blob, 24);
        cipher.CopyTo(blob, 28);

        Assert.AreSequenceEqual(expected, CredentialCrypto.UnwrapAttribute(blob, new[] { pek }));
    }

    [TestMethod]
    public void UnwrapAttribute_AesZeroPadsShortFinalCipherBlock()
    {
        var pek = Convert.FromHexString("000102030405060708090A0B0C0D0E0F");
        var iv = Convert.FromHexString("101112131415161718191A1B1C1D1E1F");
        var expectedFirstBlock = Convert.FromHexString("202122232425262728292A2B2C2D2E2F");
        var plain = expectedFirstBlock.Concat(new byte[16]).ToArray();
        byte[] cipher;
        using (var aes = Aes.Create())
        {
            aes.Key = pek;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;
            cipher = aes.CreateEncryptor().TransformFinalBlock(plain, 0, plain.Length);
        }

        var blob = new byte[28 + 17];
        BitConverter.GetBytes(0x13u).CopyTo(blob, 0);
        iv.CopyTo(blob, 8);
        cipher.AsSpan(0, 17).CopyTo(blob.AsSpan(28));

        var result = CredentialCrypto.UnwrapAttribute(blob, new[] { pek });

        Assert.AreSequenceEqual(expectedFirstBlock, result.AsSpan(0, 16).ToArray());
        Assert.AreEqual(32, result.Length);
    }

    [TestMethod]
    public void UnwrapAttribute_ReadsPekIndexFromFifthHeaderByte()
    {
        var blob = new byte[40];
        blob[4] = 0;
        blob[5] = 1;

        var result = CredentialCrypto.UnwrapAttribute(blob, new[] { new byte[16] });

        Assert.AreEqual(16, result.Length);
    }
}
