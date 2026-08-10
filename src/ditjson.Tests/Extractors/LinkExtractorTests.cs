using ditjson.Extractors;
using ditjson.Models;

namespace ditjson.Tests.Extractors;

[TestClass]
public class LinkExtractorTests
{
    [TestMethod]
    public void AddDirectMembership_PopulatesBothSidesForUser()
    {
        var user = new User {
            RecordId = 10,
            Name = "Alice",
            ObjectClass = "user",
            ObjectSid = "S-1-5-21-1-2-3-1100",
            MemberOf = []
        };
        var group = new Group {
            RecordId = 20,
            Name = "Operators",
            ObjectClass = "group",
            ObjectSid = "S-1-5-21-1-2-3-1200",
            Members = []
        };

        var added = LinkExtractor.AddDirectMembership(10, 20, "2026-01-01T00:00:00Z",
            [user], [group], []);

        Assert.IsTrue(added);
        Assert.HasCount(1, group.Members!);
        Assert.AreEqual(user.ObjectSid, group.Members![0].ObjectSid);
        Assert.AreEqual("2026-01-01T00:00:00Z", group.Members[0].DeletedTime);
        Assert.HasCount(1, user.MemberOf!);
        Assert.AreEqual(group.ObjectSid, user.MemberOf![0].ObjectSid);
        Assert.AreEqual(group.Name, user.MemberOf[0].Name);
    }

    [TestMethod]
    public void AddDirectMembership_PopulatesNestedGroupMemberOf()
    {
        var child = new Group { RecordId = 10, Name = "Child", Members = [], MemberOf = [] };
        var parent = new Group { RecordId = 20, Name = "Parent", Members = [], MemberOf = [] };

        var added = LinkExtractor.AddDirectMembership(10, 20, null, [], [child, parent], []);

        Assert.IsTrue(added);
        Assert.AreEqual(10, parent.Members!.Single().RecordId);
        Assert.AreEqual("Parent", child.MemberOf!.Single().Name);
    }

    [TestMethod]
    public void AddPrimaryGroupMemberships_ResolvesUsersAndComputersBySid()
    {
        var group = new Group {
            RecordId = 20,
            Name = "Domain Computers",
            ObjectSid = "S-1-5-21-1-2-3-515",
            Members = []
        };
        var user = new User {
            RecordId = 10,
            ObjectSid = "S-1-5-21-1-2-3-1100",
            PrimaryGroupId = 515,
            MemberOf = []
        };
        var computer = new Computer {
            RecordId = 11,
            ObjectSid = "S-1-5-21-1-2-3-1101",
            PrimaryGroupId = 515,
            MemberOf = []
        };

        var added = LinkExtractor.AddPrimaryGroupMemberships([user], [group], [computer]);

        Assert.AreEqual(2, added);
        Assert.HasCount(2, group.Members!);
        Assert.IsTrue(group.Members!.All(m => m.IsPrimaryGroup));
        Assert.AreEqual(group.ObjectSid, user.MemberOf!.Single().ObjectSid);
        Assert.AreEqual(group.ObjectSid, computer.MemberOf!.Single().ObjectSid);
    }

    [TestMethod]
    public void AddDirectMembership_IgnoresUnknownGroupOrMember()
    {
        Assert.IsFalse(LinkExtractor.AddDirectMembership(10, 20, null, [], [], []));
        Assert.IsFalse(LinkExtractor.AddDirectMembership(10, 20, null, [],
            [new Group { RecordId = 20 }], []));
    }
}
