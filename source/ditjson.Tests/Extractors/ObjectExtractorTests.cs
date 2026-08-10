using ditjson.Extractors;
using ditjson.Models;

namespace ditjson.Tests.Extractors;

[TestClass]
public class ObjectExtractorTests
{
    [TestMethod]
    public void ParseAncestorIds_ReadsLittleEndianDntsAndIgnoresPartialTail()
    {
        var value = new byte[] {
            1, 0, 0, 0,
            0x78, 0x56, 0x34, 0x12,
            0xff
        };

        var result = ObjectExtractor.ParseAncestorIds(value);

        Assert.AreSequenceEqual(new[] { 1, 0x12345678 }, result);
    }

    [TestMethod]
    public void PopulateAncestors_PreservesStoredOrderAndSkipsUnknownDnts()
    {
        var user = new User { RecordId = 10 };
        var domain = new NtdsObject { RecordId = 1, Name = "example" };
        var organizationalUnit = new NtdsObject { RecordId = 2, Name = "People" };

        ObjectExtractor.PopulateAncestors([user],
            new Dictionary<int, List<int>> { [10] = [1, 999, 2] },
            new Dictionary<int, NtdsObject> { [1] = domain, [2] = organizationalUnit });

        Assert.HasCount(2, user.Ancestors!);
        Assert.AreSame(domain, user.Ancestors![0]);
        Assert.AreSame(organizationalUnit, user.Ancestors[1]);
    }

    [TestMethod]
    public void EncodeBinary_UsesBase64WithoutChangingCertificateBytes()
    {
        var certificate = Convert.FromHexString("30820100AABBCCDD");

        var encoded = UserExtractor.EncodeBinary(certificate);

        Assert.AreEqual(Convert.ToBase64String(certificate), encoded);
        Assert.IsNull(UserExtractor.EncodeBinary(null));
        Assert.IsNull(UserExtractor.EncodeBinary([]));
    }
}
