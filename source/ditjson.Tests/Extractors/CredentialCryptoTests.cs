using ditjson.Extractors;

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
}
