using System.Buffers.Binary;
using System.Text;
using ditjson.Extractors;

namespace ditjson.Tests.Extractors;

[TestClass]
public sealed class RegistryHiveTests
{
    [TestMethod]
    public void OpenKey_NavigatesRiAndLeafListsWithoutMaterializingNames()
    {
        var path = CreateHive();
        try
        {
            using var hive = new RegistryHive(path);

            Assert.AreEqual(0x300, hive.OpenKey(@"software\TËST"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void OpenKey_UsesLhHashes()
    {
        var path = CreateHive();
        try
        {
            using var hive = new RegistryHive(path);

            Assert.AreEqual(0x200, hive.OpenKey("Software"));
            Assert.AreEqual(0x200, hive.OpenKey("software"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void OpenKey_ThrowsForMissingKey()
    {
        var path = CreateHive();
        try
        {
            using var hive = new RegistryHive(path);

            Assert.ThrowsExactly<KeyNotFoundException>(() => hive.OpenKey(@"Software\Missing"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void ReadsClassNameAndInlineValueFromCellPayloads()
    {
        var path = CreateHive();
        try
        {
            using var hive = new RegistryHive(path);
            var key = hive.OpenKey(@"Software\TËST");

            Assert.AreEqual("0011223344556677", hive.ReadClassName(key));
            Assert.AreSequenceEqual(BitConverter.GetBytes(7), hive.ReadValue(key, "Current")!);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string CreateHive()
    {
        const int hbin = 0x1000;
        var bytes = new byte[0x2000];
        WriteUInt32(bytes, 0, 0x66676572); // regf
        WriteInt32(bytes, 0x24, 0); // root cell

        WriteKey(bytes, hbin, 0, "Root", compressed: true, subkeyList: 0x100);
        WriteList(bytes, hbin, 0x100, "ri", 0x180);
        WriteList(bytes, hbin, 0x180, "lh", 0x200);
        WriteKey(bytes, hbin, 0x200, "Software", compressed: true, subkeyList: 0x280);
        WriteList(bytes, hbin, 0x280, "li", 0x300);
        WriteKey(bytes, hbin, 0x300, "Tëst", compressed: false, subkeyList: -1,
            classCell: 0x400, valueList: 0x480);
        Encoding.Unicode.GetBytes("0011223344556677").CopyTo(bytes, hbin + 0x400 + 4);
        WriteInt32(bytes, hbin + 0x480 + 4, 0x500);
        WriteValue(bytes, hbin, 0x500, "Current", BitConverter.GetBytes(7));

        var path = Path.Combine(Path.GetTempPath(), $"ditjson-{Guid.NewGuid():N}.hive");
        File.WriteAllBytes(path, bytes);
        return path;
    }

    private static void WriteKey(byte[] hive, int hbin, int cell, string name, bool compressed, int subkeyList,
        int classCell = -1, int valueList = -1)
    {
        var at = hbin + cell + 4;
        WriteAscii(hive, at, "nk");
        WriteUInt16(hive, at + 2, compressed ? (ushort)0x20 : (ushort)0);
        WriteInt32(hive, at + 0x14, subkeyList < 0 ? 0 : 1);
        WriteInt32(hive, at + 0x1c, subkeyList);
        WriteInt32(hive, at + 0x24, valueList < 0 ? 0 : 1);
        WriteInt32(hive, at + 0x28, valueList);
        WriteInt32(hive, at + 0x30, classCell);
        var nameBytes = (compressed ? Encoding.ASCII : Encoding.Unicode).GetBytes(name);
        WriteUInt16(hive, at + 0x48, (ushort)nameBytes.Length);
        WriteUInt16(hive, at + 0x4a, classCell < 0 ? (ushort)0 : (ushort)32);
        nameBytes.CopyTo(hive, at + 0x4c);
    }

    private static void WriteList(byte[] hive, int hbin, int cell, string signature, int child)
    {
        var at = hbin + cell + 4;
        WriteAscii(hive, at, signature);
        WriteUInt16(hive, at + 2, 1);
        WriteInt32(hive, at + 4, child);
        if (signature == "lh")
        {
            WriteUInt32(hive, at + 8, ComputeLhHash("Software"));
        }
    }

    private static uint ComputeLhHash(string name)
    {
        uint hash = 0;
        foreach (var character in name)
        {
            hash = hash * 37 + char.ToUpperInvariant(character);
        }

        return hash;
    }

    private static void WriteValue(byte[] hive, int hbin, int cell, string name, byte[] value)
    {
        var at = hbin + cell + 4;
        WriteAscii(hive, at, "vk");
        WriteUInt16(hive, at + 2, (ushort)name.Length);
        WriteUInt32(hive, at + 4, 0x80000000u | (uint)value.Length);
        value.CopyTo(hive, at + 8);
        WriteUInt16(hive, at + 0x10, 1);
        WriteAscii(hive, at + 0x14, name);
    }

    private static void WriteAscii(byte[] destination, int offset, string value) =>
        Encoding.ASCII.GetBytes(value).CopyTo(destination, offset);

    private static void WriteUInt16(byte[] destination, int offset, ushort value) =>
        BinaryPrimitives.WriteUInt16LittleEndian(destination.AsSpan(offset), value);

    private static void WriteUInt32(byte[] destination, int offset, uint value) =>
        BinaryPrimitives.WriteUInt32LittleEndian(destination.AsSpan(offset), value);

    private static void WriteInt32(byte[] destination, int offset, int value) =>
        BinaryPrimitives.WriteInt32LittleEndian(destination.AsSpan(offset), value);
}
