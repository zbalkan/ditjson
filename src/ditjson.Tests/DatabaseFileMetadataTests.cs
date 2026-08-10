using System.Buffers.Binary;

namespace ditjson.Tests;

[TestClass]
public class DatabaseFileMetadataTests
{
    [TestMethod]
    public void Read_ExtractsEseHeaderFields()
    {
        var header = new byte[8192];
        WriteUInt32(header, 0, 0x11223344);
        WriteUInt32(header, 4, 0x89ABCDEF);
        WriteUInt32(header, 8, 0x620);
        WriteUInt32(header, 12, 1);
        BinaryPrimitives.WriteUInt64LittleEndian(header.AsSpan(16), 42);
        WriteLogTime(header, 28, 126, 7, 10, 14, 3, 39);
        WriteLogTime(header, 72, 126, 7, 10, 14, 4, 1);
        WriteUInt32(header, 216, 10);
        WriteUInt32(header, 220, 0);
        WriteUInt32(header, 224, 20348);
        WriteUInt32(header, 228, 0);
        WriteUInt32(header, 236, 8192);

        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, header);
            var metadata = DatabaseFileMetadata.Read(path);

            Assert.AreEqual("0x11223344", metadata.HeaderChecksum);
            Assert.AreEqual("0x89ABCDEF", metadata.Signature);
            Assert.AreEqual("0x00000620", metadata.FileFormatVersion);
            Assert.AreEqual((uint)8192, metadata.PageSize);
            Assert.AreEqual("0x000000000000002A", metadata.DatabaseTime);
            Assert.AreEqual("10.0 (20348) Service Pack 0", metadata.WindowsVersion);
            Assert.AreEqual("2026-08-10T14:03:39.0000000Z", metadata.CreationTime);
            Assert.AreEqual("2026-08-10T14:04:01.0000000Z", metadata.AttachTime);
            Assert.IsTrue(metadata.IsDirty);
            Assert.IsNull(metadata.DetachTime);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static void WriteUInt32(byte[] data, int offset, uint value) =>
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset), value);

    private static void WriteLogTime(byte[] data, int offset, byte year, byte month, byte day,
        byte hour, byte minute, byte second)
    {
        data[offset] = second;
        data[offset + 1] = minute;
        data[offset + 2] = hour;
        data[offset + 3] = day;
        data[offset + 4] = month;
        data[offset + 5] = year;
    }
}
