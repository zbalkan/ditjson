using ditjson.Extractors;

namespace ditjson.Tests.Extractors;

[TestClass]
public class NtdsColumnNamesTests
{
    [TestMethod]
    public void ObjectColumns_MatchPhysicalNtdsNames()
    {
        Assert.AreEqual("ATTm589825", NtdsColumnNames.ObjectName);
        Assert.AreEqual("ATTk589826", NtdsColumnNames.ObjectGuid);
        Assert.AreEqual("ATTl131074", NtdsColumnNames.WhenCreated);
        Assert.AreEqual("ATTl131075", NtdsColumnNames.WhenChanged);
        Assert.AreEqual("ATTi131120", NtdsColumnNames.IsDeleted);
    }

    [TestMethod]
    public void CredentialColumns_MatchPhysicalNtdsNames()
    {
        Assert.AreEqual("ATTm590045", NtdsColumnNames.SamAccountName);
        Assert.AreEqual("ATTj590126", NtdsColumnNames.SamAccountType);
        Assert.AreEqual("ATTr589970", NtdsColumnNames.ObjectSid);
        Assert.AreEqual("ATTm590480", NtdsColumnNames.UserPrincipalName);
        Assert.AreEqual("ATTj589832", NtdsColumnNames.UserAccountControl);
        Assert.AreEqual("ATTq589876", NtdsColumnNames.LastLogon);
        Assert.AreEqual("ATTq591520", NtdsColumnNames.LastLogonTimestamp);
        Assert.AreEqual("ATTq589983", NtdsColumnNames.AccountExpires);
        Assert.AreEqual("ATTq589920", NtdsColumnNames.PasswordLastSet);
        Assert.AreEqual("ATTq589873", NtdsColumnNames.BadPasswordTime);
        Assert.AreEqual("ATTj589993", NtdsColumnNames.LogonCount);
        Assert.AreEqual("ATTj589836", NtdsColumnNames.BadPasswordCount);
        Assert.AreEqual("ATTj589922", NtdsColumnNames.PrimaryGroupId);
        Assert.AreEqual("ATTi590943", NtdsColumnNames.DialInAccessPermission);
        Assert.AreEqual("ATTk589879", NtdsColumnNames.LmHash);
        Assert.AreEqual("ATTk589914", NtdsColumnNames.NtHash);
        Assert.AreEqual("ATTk590689", NtdsColumnNames.PekList);
        Assert.AreEqual("ATTk589984", NtdsColumnNames.LmHashHistory);
        Assert.AreEqual("ATTk589918", NtdsColumnNames.NtHashHistory);
        Assert.AreEqual("ATTk589949", NtdsColumnNames.SupplementalCredentials);
    }

    [TestMethod]
    public void ComputerColumns_MatchPhysicalNtdsNames()
    {
        Assert.AreEqual("ATTm590443", NtdsColumnNames.DnsHostName);
        Assert.AreEqual("ATTm590187", NtdsColumnNames.OperatingSystem);
        Assert.AreEqual("ATTm590188", NtdsColumnNames.OperatingSystemVersion);
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
