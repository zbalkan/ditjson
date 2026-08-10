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
        StringAssert.Contains(json, "<Alice>");
    }
}
