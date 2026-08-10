using ditjson.Filtering;
using ditjson.Models;

namespace ditjson.Tests.Filtering;

[TestClass]
public class FieldCleanerTests
{
    [TestMethod]
    public void CleanUser_WithWhitespaceStrings_ConvertsToNull()
    {
        // Arrange
        var user = new User
        {
            Name = "   ",
            SamAccountName = "\t",
            UserPrincipalName = null,
            Certificate = "  \n  "
        };

        // Act
        FieldCleaner.CleanUser(user);

        // Assert
        Assert.IsNull(user.Name);
        Assert.IsNull(user.SamAccountName);
        Assert.IsNull(user.UserPrincipalName);
        Assert.IsNull(user.Certificate);
    }

    [TestMethod]
    public void CleanUser_WithValidStrings_KeepsOriginalValues()
    {
        // Arrange
        var user = new User
        {
            Name = "John Doe",
            SamAccountName = "jdoe",
            UserPrincipalName = "jdoe@example.com"
        };

        // Act
        FieldCleaner.CleanUser(user);

        // Assert
        Assert.AreEqual("John Doe", user.Name);
        Assert.AreEqual("jdoe", user.SamAccountName);
        Assert.AreEqual("jdoe@example.com", user.UserPrincipalName);
    }

    [TestMethod]
    public void CleanUser_WithNegativeIntegers_ResetsToZero()
    {
        // Arrange
        var user = new User
        {
            PrimaryGroupId = -1,
            LogonCount = -5,
            BadPwdCount = -10,
            DialInAccessPermission = -1
        };

        // Act
        FieldCleaner.CleanUser(user);

        // Assert
        Assert.AreEqual(0, user.PrimaryGroupId);
        Assert.AreEqual(0, user.LogonCount);
        Assert.AreEqual(0, user.BadPwdCount);
        Assert.AreEqual(0, user.DialInAccessPermission);
    }

    [TestMethod]
    public void CleanUser_WithPositiveIntegers_KeepsValues()
    {
        // Arrange
        var user = new User
        {
            PrimaryGroupId = 513,
            LogonCount = 42,
            BadPwdCount = 5
        };

        // Act
        FieldCleaner.CleanUser(user);

        // Assert
        Assert.AreEqual(513, user.PrimaryGroupId);
        Assert.AreEqual(42, user.LogonCount);
        Assert.AreEqual(5, user.BadPwdCount);
    }

    [TestMethod]
    public void CleanUser_WithNullUserAccountControl_Handles()
    {
        // Arrange
        var user = new User
        {
            UserAccountControl = null
        };

        // Act & Assert (should not throw)
        FieldCleaner.CleanUser(user);
        Assert.IsNull(user.UserAccountControl);
    }

    [TestMethod]
    public void CleanUser_WithEmptyUserAccountControl_SetsToNull()
    {
        // Arrange
        var user = new User
        {
            UserAccountControl = []
        };

        // Act
        FieldCleaner.CleanUser(user);

        // Assert
        Assert.IsNull(user.UserAccountControl);
    }

    [TestMethod]
    public void CleanUser_WithEmptyStringsInUserAccountControl_RemovesThem()
    {
        // Arrange
        var user = new User
        {
            UserAccountControl = ["SCRIPT", "", "ACCOUNTDISABLE", null, "   "]
        };

        // Act
        FieldCleaner.CleanUser(user);

        // Assert
        Assert.IsNotNull(user.UserAccountControl);
        Assert.HasCount(2, user.UserAccountControl);
        Assert.Contains("SCRIPT", user.UserAccountControl);
        Assert.Contains("ACCOUNTDISABLE", user.UserAccountControl);
    }

    [TestMethod]
    public void CleanGroup_WithWhitespaceStrings_ConvertsToNull()
    {
        // Arrange
        var group = new Group
        {
            Name = "  ",
            SamAccountName = "\n",
            GroupType = null
        };

        // Act
        FieldCleaner.CleanGroup(group);

        // Assert
        Assert.IsNull(group.Name);
        Assert.IsNull(group.SamAccountName);
        Assert.IsNull(group.GroupType);
    }

    [TestMethod]
    public void CleanComputer_WithWhitespaceStrings_ConvertsToNull()
    {
        // Arrange
        var computer = new Computer
        {
            Name = "   ",
            DnsHostName = "\t",
            OperatingSystem = "  \n  ",
            OperatingSystemVersion = null
        };

        // Act
        FieldCleaner.CleanComputer(computer);

        // Assert
        Assert.IsNull(computer.Name);
        Assert.IsNull(computer.DnsHostName);
        Assert.IsNull(computer.OperatingSystem);
        Assert.IsNull(computer.OperatingSystemVersion);
    }

    [TestMethod]
    public void CleanComputer_WithNegativeIntegers_ResetsToZero()
    {
        // Arrange
        var computer = new Computer
        {
            DialInAccessPermission = -5
        };

        // Act
        FieldCleaner.CleanComputer(computer);

        // Assert
        Assert.AreEqual(0, computer.DialInAccessPermission);
    }

    [TestMethod]
    public void CleanUser_WithNullObject_Handles() =>
        // Act & Assert (should not throw)
        FieldCleaner.CleanUser(null);

    [TestMethod]
    public void CleanGroup_WithNullObject_Handles() =>
        // Act & Assert (should not throw)
        FieldCleaner.CleanGroup(null);

    [TestMethod]
    public void CleanComputer_WithNullObject_Handles() =>
        // Act & Assert (should not throw)
        FieldCleaner.CleanComputer(null);
}
