using ditjson.Decoders;

namespace ditjson.Tests.Decoders;

[TestClass]
public class FlagsDecoderTests
{
    [TestMethod]
    public void DecodeGroupType_WithSecurityGlobalGroup_ReturnsCorrectType()
    {
        // Arrange: GROUP_TYPE_SECURITY_GLOBAL (bit mask)
        uint groupType = 0x00000004;

        // Act
        var result = FlagsDecoder.DecodeGroupType((int)groupType);

        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void DecodeSAMAccountType_WithComputerType_ReturnsComputer()
    {
        // Arrange: SAM_MACHINE_ACCOUNT (0x30000001)
        var samType = 0x30000001;

        // Act
        var result = FlagsDecoder.DecodeSAMAccountType(samType);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Computer", result);
    }

    [TestMethod]
    public void DecodeSAMAccountType_WithGroupType_ReturnsSamGroupObject()
    {
        // Arrange: SAM_GROUP_OBJECT (0x10000000)
        var samType = 0x10000000;

        // Act
        var result = FlagsDecoder.DecodeSAMAccountType(samType);

        // Assert
        Assert.IsNotNull(result);
        Assert.Contains("GROUP", result);
    }

    [TestMethod]
    public void DecodeSAMAccountType_WithUnknownType_ReturnsUnknown()
    {
        // Arrange: Unknown type
        var samType = unchecked((int)0xFFFFFFFF);

        // Act
        var result = FlagsDecoder.DecodeSAMAccountType(samType);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Unknown", result);
    }

    [TestMethod]
    public void DecodeSAMAccountType_WithUserType_ReturnsUser()
    {
        // Arrange: SAM_USER_OBJECT (0x30000000)
        var samType = 0x30000000;

        // Act
        var result = FlagsDecoder.DecodeSAMAccountType(samType);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("User", result);
    }

    [TestMethod]
    public void DecodeUAC_WithDisabledAccount_ReturnsAccountdisable()
    {
        // Arrange: ACCOUNTDISABLE flag (0x0002)
        var uacValue = 0x0002;

        // Act
        var result = FlagsDecoder.DecodeUAC(uacValue);

        // Assert
        Assert.IsNotNull(result);
        Assert.Contains("ACCOUNTDISABLE", result);
    }

    [TestMethod]
    public void DecodeUAC_WithDontExpirePassword_ReturnsDontexpirepasswd()
    {
        // Arrange: DONT_EXPIRE_PASSWORD flag (0x10000)
        var uacValue = 0x10000;

        // Act
        var result = FlagsDecoder.DecodeUAC(uacValue);

        // Assert
        Assert.IsNotNull(result);
        Assert.Contains("DONT_EXPIRE_PASSWORD", result);
    }

    [TestMethod]
    public void DecodeUAC_WithLockedOutAccount_ReturnsLockout()
    {
        // Arrange: LOCKOUT flag (0x0010)
        var uacValue = 0x0010;

        // Act
        var result = FlagsDecoder.DecodeUAC(uacValue);

        // Assert
        Assert.IsNotNull(result);
        Assert.Contains("LOCKOUT", result);
    }

    [TestMethod]
    public void DecodeUAC_WithMultipleFlags_ReturnsAll()
    {
        // Arrange: ACCOUNTDISABLE (0x0002) + LOCKOUT (0x0010) = 0x0012
        var uacValue = 0x0012;

        // Act
        var result = FlagsDecoder.DecodeUAC(uacValue);

        // Assert
        Assert.IsNotNull(result);
        Assert.Contains("ACCOUNTDISABLE", result);
        Assert.Contains("LOCKOUT", result);
    }

    [TestMethod]
    public void DecodeUAC_WithPasswordNotRequired_ReturnsPwdnotreq()
    {
        // Arrange: PASSWD_NOTREQD flag (0x0020)
        var uacValue = 0x0020;

        // Act
        var result = FlagsDecoder.DecodeUAC(uacValue);

        // Assert
        Assert.IsNotNull(result);
        Assert.Contains("PWD_NOTREQD", result);
    }

    [TestMethod]
    public void DecodeUAC_WithZeroValue_ReturnsEmptyList()
    {
        // Act
        var result = FlagsDecoder.DecodeUAC(0);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsEmpty(result);
    }
}
