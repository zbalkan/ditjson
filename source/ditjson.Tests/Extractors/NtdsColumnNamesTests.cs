using ditjson.Extractors;

namespace ditjson.Tests.Extractors;

[TestClass]
public class NtdsColumnNamesTests
{
    [TestMethod]
    public void CredentialColumns_MatchPhysicalNtdsNames()
    {
        Assert.AreEqual("ATTm590045", NtdsColumnNames.SamAccountName);
        Assert.AreEqual("ATTj590126", NtdsColumnNames.SamAccountType);
        Assert.AreEqual("ATTr589970", NtdsColumnNames.ObjectSid);
        Assert.AreEqual("ATTk589879", NtdsColumnNames.LmHash);
        Assert.AreEqual("ATTk589914", NtdsColumnNames.NtHash);
        Assert.AreEqual("ATTk590689", NtdsColumnNames.PekList);
        Assert.AreEqual("ATTk589984", NtdsColumnNames.LmHashHistory);
        Assert.AreEqual("ATTk589918", NtdsColumnNames.NtHashHistory);
        Assert.AreEqual("ATTk589949", NtdsColumnNames.SupplementalCredentials);
    }

    [TestMethod]
    public void BitLockerColumns_MatchPhysicalNtdsNames()
    {
        Assert.AreEqual("ATTk591823", NtdsColumnNames.BitLockerKeyPackage);
        Assert.AreEqual("ATTk591789", NtdsColumnNames.BitLockerRecoveryGuid);
        Assert.AreEqual("ATTm591788", NtdsColumnNames.BitLockerRecoveryPassword);
        Assert.AreEqual("ATTk591822", NtdsColumnNames.BitLockerVolumeGuid);
    }
}
