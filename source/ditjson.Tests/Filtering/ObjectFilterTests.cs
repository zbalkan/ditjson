using ditjson.Filtering;
using ditjson.Models;

namespace ditjson.Tests.Filtering;

[TestClass]
public class ObjectFilterTests
{
    [TestMethod]
    public void CleanupComputer_WithEmptyMemberOf_SetsToNullWhenExcludingEmpty()
    {
        // Arrange
        var computer = new Computer
        {
            Name = "SERVER01",
            MemberOf = []
        };

        // Act
        ObjectFilter.CleanupComputer(computer, false);

        // Assert
        Assert.IsNull(computer.MemberOf);
    }

    [TestMethod]
    public void CleanupGroup_WithEmptyMembers_SetsToNullWhenExcludingEmpty()
    {
        // Arrange
        var group = new Group
        {
            Name = "Admins",
            Members = []
        };

        // Act
        ObjectFilter.CleanupGroup(group, false);

        // Assert
        Assert.IsNull(group.Members);
    }

    [TestMethod]
    public void CleanupUser_WithEmptyCollections_KeepsWhenIncludingEmpty()
    {
        // Arrange
        var user = new User
        {
            Name = "jdoe",
            MemberOf = [],
            Ancestors = []
        };

        // Act
        ObjectFilter.CleanupUser(user, true);

        // Assert
        Assert.IsNotNull(user.MemberOf);
        Assert.IsNotNull(user.Ancestors);
    }

    [TestMethod]
    public void CleanupUser_WithEmptyCollections_SetsToNullWhenExcludingEmpty()
    {
        // Arrange
        var user = new User
        {
            Name = "jdoe",
            MemberOf = [],
            Ancestors = [],
            PasswordHistory = []
        };

        // Act
        ObjectFilter.CleanupUser(user, false);

        // Assert
        Assert.IsNull(user.MemberOf);
        Assert.IsNull(user.Ancestors);
        Assert.IsNull(user.PasswordHistory);
    }

    [TestMethod]
    public void CleanupUser_WithEmptyPasswordHashes_SetsToNull()
    {
        // Arrange
        var user = new User
        {
            Name = "jdoe",
            PasswordHashes = new PasswordHashes()
        };

        // Act
        ObjectFilter.CleanupUser(user, false);

        // Assert
        Assert.IsNull(user.PasswordHashes);
    }

    [TestMethod]
    public void ShouldIncludeComputer_WithActiveComputer_ReturnsTrue()
    {
        // Arrange
        var computer = new Computer
        {
            Name = "SERVER01",
            IsDeleted = false
        };
        var options = new ObjectFilter.FilterOptions();

        // Act
        var result = ObjectFilter.ShouldIncludeComputer(computer, options);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ShouldIncludeComputer_WithExcludeComputersFlag_ReturnsFalse()
    {
        // Arrange
        var computer = new Computer
        {
            Name = "SERVER01",
            IsDeleted = false
        };
        var options = new ObjectFilter.FilterOptions { ExcludeComputers = true };

        // Act
        var result = ObjectFilter.ShouldIncludeComputer(computer, options);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ShouldIncludeComputer_WithNullComputer_ReturnsFalse()
    {
        // Arrange
        var options = new ObjectFilter.FilterOptions();

        // Act
        var result = ObjectFilter.ShouldIncludeComputer(null, options);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ShouldIncludeGroup_WithActiveGroup_ReturnsTrue()
    {
        // Arrange
        var group = new Group
        {
            Name = "Admins",
            IsDeleted = false
        };
        var options = new ObjectFilter.FilterOptions();

        // Act
        var result = ObjectFilter.ShouldIncludeGroup(group, options);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ShouldIncludeGroup_WithExcludeGroupsFlag_ReturnsFalse()
    {
        // Arrange
        var group = new Group
        {
            Name = "Admins",
            IsDeleted = false
        };
        var options = new ObjectFilter.FilterOptions { ExcludeGroups = true };

        // Act
        var result = ObjectFilter.ShouldIncludeGroup(group, options);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ShouldIncludeGroup_WithNullGroup_ReturnsFalse()
    {
        // Arrange
        var options = new ObjectFilter.FilterOptions();

        // Act
        var result = ObjectFilter.ShouldIncludeGroup(null, options);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ShouldIncludeUser_WithActiveUser_ReturnsTrue()
    {
        // Arrange
        var user = new User
        {
            Name = "jdoe",
            IsDeleted = false,
            UserAccountControl = ["NORMAL_ACCOUNT"]
        };
        var options = new ObjectFilter.FilterOptions();

        // Act
        var result = ObjectFilter.ShouldIncludeUser(user, options);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ShouldIncludeUser_WithDeletedUser_ReturnsFalseWhenNotIncludingDeleted()
    {
        // Arrange
        var user = new User
        {
            Name = "jdoe",
            IsDeleted = true,
            UserAccountControl = []
        };
        var options = new ObjectFilter.FilterOptions { IncludeDeleted = false };

        // Act
        var result = ObjectFilter.ShouldIncludeUser(user, options);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ShouldIncludeUser_WithDeletedUser_ReturnsTrueWhenIncludingDeleted()
    {
        // Arrange
        var user = new User
        {
            Name = "jdoe",
            IsDeleted = true,
            UserAccountControl = []
        };
        var options = new ObjectFilter.FilterOptions { IncludeDeleted = true };

        // Act
        var result = ObjectFilter.ShouldIncludeUser(user, options);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ShouldIncludeUser_WithDisabledAccount_ReturnsFalseWhenExcludingDisabled()
    {
        // Arrange
        var user = new User
        {
            Name = "jdoe",
            IsDeleted = false,
            UserAccountControl = ["ACCOUNTDISABLE"]
        };
        var options = new ObjectFilter.FilterOptions { ExcludeDisabled = true };

        // Act
        var result = ObjectFilter.ShouldIncludeUser(user, options);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ShouldIncludeUser_WithLockedOutAccount_ReturnsFalseWhenExcludingLockedOut()
    {
        // Arrange
        var user = new User
        {
            Name = "jdoe",
            IsDeleted = false,
            UserAccountControl = ["LOCKOUT"]
        };
        var options = new ObjectFilter.FilterOptions { ExcludeLockedOut = true };

        // Act
        var result = ObjectFilter.ShouldIncludeUser(user, options);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ShouldIncludeUser_WithNullUser_ReturnsFalse()
    {
        // Arrange
        var options = new ObjectFilter.FilterOptions();

        // Act
        var result = ObjectFilter.ShouldIncludeUser(null, options);

        // Assert
        Assert.IsFalse(result);
    }
}
