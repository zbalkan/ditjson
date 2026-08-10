using System.Text;
using System.Security.Cryptography;
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
    public void UnwrapAttribute_ReadsEntireFourBytePekIndex()
    {
        var blob = new byte[40];
        blob[4] = 0;
        blob[5] = 1;

        Assert.ThrowsExactly<InvalidDataException>(() =>
            CredentialCrypto.UnwrapAttribute(blob, new[] { new byte[16] }));
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
    public void ParseSupplementalCredentialsBlob_ReadsUserPropertiesHeader()
    {
        const string name = "Primary:CLEARTEXT";
        var nameBytes = Encoding.Unicode.GetBytes(name);
        var cleartextBytes = Encoding.Unicode.GetBytes("Roadmap test");
        var valueBytes = Encoding.ASCII.GetBytes(Convert.ToHexString(cleartextBytes));
        var blob = new byte[112 + 6 + nameBytes.Length + valueBytes.Length];
        BitConverter.GetBytes((ushort)1).CopyTo(blob, 110);
        BitConverter.GetBytes((ushort)nameBytes.Length).CopyTo(blob, 112);
        BitConverter.GetBytes((ushort)valueBytes.Length).CopyTo(blob, 114);
        nameBytes.CopyTo(blob, 118);
        valueBytes.CopyTo(blob, 118 + nameBytes.Length);

        var (cleartext, keys) = SupplementalCredentialsParser.ParseSupplementalCredentialsBlob(blob);

        Assert.AreEqual("Roadmap test", cleartext);
        Assert.IsNull(keys);
    }
}
