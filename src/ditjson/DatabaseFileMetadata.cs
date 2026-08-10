using System;
using System.Buffers.Binary;
using System.IO;
using System.Text.Json.Serialization;

namespace ditjson
{
    internal sealed class DatabaseFileMetadata
    {
        [JsonPropertyName("attachTime")]
        public string? AttachTime { get; init; }

        [JsonPropertyName("consistentTime")]
        public string? ConsistentTime { get; init; }

        [JsonPropertyName("creationTime")]
        public string? CreationTime { get; init; }

        [JsonPropertyName("databaseTime")]
        public string DatabaseTime { get; init; } = string.Empty;

        [JsonPropertyName("detachTime")]
        public string? DetachTime { get; init; }

        [JsonPropertyName("fileFormatVersion")]
        public string FileFormatVersion { get; init; } = string.Empty;

        [JsonPropertyName("fileType")]
        public string FileType { get; init; } = string.Empty;

        [JsonPropertyName("headerChecksum")]
        public string HeaderChecksum { get; init; } = string.Empty;

        [JsonPropertyName("isDirty")]
        public bool IsDirty { get; init; }

        [JsonPropertyName("pageSize")]
        public uint PageSize { get; init; }

        [JsonPropertyName("recoveryTime")]
        public string? RecoveryTime { get; init; }

        [JsonPropertyName("signature")]
        public string Signature { get; init; } = string.Empty;

        [JsonPropertyName("windowsVersion")]
        public string WindowsVersion { get; init; } = string.Empty;

        public static DatabaseFileMetadata Read(string path)
        {
            const int headerSize = 8192;
            var header = new byte[headerSize];
            using var stream = File.OpenRead(path);
            stream.ReadExactly(header);

            var isDirty = header[88] == 0;
            var detachTime = isDirty ? null : ReadLogTime(header, 88);
            return new DatabaseFileMetadata {
                HeaderChecksum = ReadHexUInt32(header, 0),
                Signature = ReadHexUInt32(header, 4),
                FileFormatVersion = ReadHexUInt32(header, 8),
                FileType = ReadHexUInt32(header, 12),
                PageSize = ReadUInt32(header, 236),
                DatabaseTime = $"0x{ReadUInt64(header, 16):X16}",
                WindowsVersion = $"{ReadUInt32(header, 216)}.{ReadUInt32(header, 220)} " +
                                 $"({ReadUInt32(header, 224)}) Service Pack {ReadUInt32(header, 228)}",
                CreationTime = ReadLogTime(header, 28),
                ConsistentTime = ReadLogTime(header, 64),
                AttachTime = ReadLogTime(header, 72),
                DetachTime = detachTime,
                IsDirty = isDirty,
                RecoveryTime = ReadLogTime(header, 244)
            };
        }

        private static string ReadHexUInt32(byte[] header, int offset) => $"0x{ReadUInt32(header, offset):X8}";

        // ESE LOGTIME stores the year relative to 1900 and the month as a zero-based value.
        private static string? ReadLogTime(byte[] header, int offset)
        {
            var second = header[offset];
            var minute = header[offset + 1];
            var hour = header[offset + 2];
            var day = header[offset + 3];
            var month = header[offset + 4];
            var year = header[offset + 5];
            if (second == 0 && minute == 0 && hour == 0 && day == 0 && month == 0 && year == 0)
            {
                return null;
            }

            try
            {
                return new DateTime(year + 1900, month + 1, day, hour, minute, second,
                    DateTimeKind.Utc).ToString("O");
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        private static uint ReadUInt32(byte[] header, int offset) =>
                            BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(offset, sizeof(uint)));

        private static ulong ReadUInt64(byte[] header, int offset) =>
            BinaryPrimitives.ReadUInt64LittleEndian(header.AsSpan(offset, sizeof(ulong)));
    }
}
