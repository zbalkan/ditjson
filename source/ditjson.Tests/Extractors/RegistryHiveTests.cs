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
        WriteKey(bytes, hbin, 0x300, "Tëst", compressed: false, subkeyList: -1);

        var path = Path.Combine(Path.GetTempPath(), $"ditjson-{Guid.NewGuid():N}.hive");
        File.WriteAllBytes(path, bytes);
        return path;
    }

    private static void WriteKey(byte[] hive, int hbin, int cell, string name, bool compressed, int subkeyList)
    {
        var at = hbin + cell;
        WriteAscii(hive, at + 4, "nk");
        WriteUInt16(hive, at + 6, compressed ? (ushort)0x20 : (ushort)0);
        WriteInt32(hive, at + 0x14, subkeyList < 0 ? 0 : 1);
        WriteInt32(hive, at + 0x1c, subkeyList);
        var nameBytes = (compressed ? Encoding.ASCII : Encoding.Unicode).GetBytes(name);
        WriteUInt16(hive, at + 0x48, (ushort)nameBytes.Length);
        nameBytes.CopyTo(hive, at + 0x4c);
    }

    private static void WriteList(byte[] hive, int hbin, int cell, string signature, int child)
    {
        var at = hbin + cell;
        WriteAscii(hive, at + 4, signature);
        WriteUInt16(hive, at + 6, 1);
        WriteInt32(hive, at + 8, child);
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
