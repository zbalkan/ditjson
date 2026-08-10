using System.Text.Json;
using ditjson.Models;
using ditjson.Output;

namespace ditjson.Tests.Output;

[TestClass]
public class JsonOutputFormatterTests
{
    [TestMethod]
    public void FormatStructuredOutput_UsesExpectedSchemaAndCounts()
    {
        var users = new List<User>
        {
            new() { Name = "<Alice>", ObjectClass = "user" }
        };
        var groups = new List<Group>
        {
            new() { Name = "Admins", ObjectClass = "group" }
        };

        var json = JsonOutputFormatter.FormatStructuredOutput(users, groups, new List<Computer>());

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.AreEqual(1, root.GetProperty("metadata").GetProperty("totalUsers").GetInt32());
        Assert.AreEqual(1, root.GetProperty("metadata").GetProperty("totalGroups").GetInt32());
        Assert.AreEqual(0, root.GetProperty("metadata").GetProperty("totalComputers").GetInt32());
        Assert.AreEqual("<Alice>", root.GetProperty("users")[0].GetProperty("Name").GetString());
        Assert.IsFalse(root.GetProperty("users")[0].TryGetProperty("SamAccountName", out _));
        Assert.Contains("<Alice>", json);
    }

    [TestMethod]
    public void FormatStructuredOutput_PreservesCredentialsAddedAfterObjectExtraction()
    {
        var user = new User { Name = "Alice", ObjectClass = "user" };
        var computer = new Computer { Name = "DC", ObjectClass = "computer" };

        user.PasswordHashes = new PasswordHashes { NtHash = "NT-HASH", LmHash = "LM-HASH" };
        user.PasswordHistory = ["OLD-NT-HASH"];
        user.LmPasswordHistory = ["OLD-LM-HASH"];
        user.SupplementalCredentials = new SupplementalCredentials
        {
            ClearTextPassword = "password",
            KerberosKeys = [new KerberosKey { Algorithm = "AES256", Key = "USER-KEY" }]
        };
        computer.PasswordHashes = new PasswordHashes { NtHash = "COMPUTER-HASH" };
        computer.SupplementalCredentials = new SupplementalCredentials
        {
            KerberosKeys = [new KerberosKey { Algorithm = "AES128", Key = "COMPUTER-KEY" }]
        };

        var json = JsonOutputFormatter.FormatStructuredOutput([user], [], [computer]);

        using var document = JsonDocument.Parse(json);
        var serializedUser = document.RootElement.GetProperty("users")[0];
        Assert.AreEqual("NT-HASH",
            serializedUser.GetProperty("passwordHashes").GetProperty("ntHash").GetString());
        Assert.AreEqual("LM-HASH",
            serializedUser.GetProperty("passwordHashes").GetProperty("lmHash").GetString());
        Assert.AreEqual("OLD-NT-HASH", serializedUser.GetProperty("passwordHistory")[0].GetString());
        Assert.AreEqual("OLD-LM-HASH", serializedUser.GetProperty("lmPasswordHistory")[0].GetString());
        Assert.AreEqual("password", serializedUser.GetProperty("supplementalCredentials")
            .GetProperty("clearTextPassword").GetString());
        Assert.AreEqual("USER-KEY", serializedUser.GetProperty("supplementalCredentials")
            .GetProperty("kerberosKeys")[0].GetProperty("key").GetString());
        Assert.IsFalse(serializedUser.TryGetProperty("PasswordHashes", out _));
        Assert.IsFalse(serializedUser.TryGetProperty("SupplementalCredentials", out _));

        var serializedComputer = document.RootElement.GetProperty("computers")[0];
        Assert.AreEqual("COMPUTER-HASH",
            serializedComputer.GetProperty("passwordHashes").GetProperty("ntHash").GetString());
        Assert.AreEqual("COMPUTER-KEY", serializedComputer.GetProperty("supplementalCredentials")
            .GetProperty("kerberosKeys")[0].GetProperty("key").GetString());
    }

    [TestMethod]
    public void FormatStructuredOutput_UsesCompactMemberOfReferences()
    {
        var user = new User
        {
            MemberOf = [new GroupMembership { Name = "Domain Users", ObjectSid = "S-1-5-21-1-2-3-513" }]
        };

        var json = JsonOutputFormatter.FormatStructuredOutput([user], [], []);

        using var document = JsonDocument.Parse(json);
        var membership = document.RootElement.GetProperty("users")[0].GetProperty("MemberOf")[0];
        Assert.AreEqual(2, membership.EnumerateObject().Count());
        Assert.AreEqual("Domain Users", membership.GetProperty("Name").GetString());
        Assert.AreEqual("S-1-5-21-1-2-3-513", membership.GetProperty("ObjectSid").GetString());
    }
}
