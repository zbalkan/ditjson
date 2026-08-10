using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ditjson.Extractors
{
    /// A deliberately small, read-only regf parser for key navigation, class names and values.
    internal sealed class RegistryHive : IDisposable
    {
        private const int HbinBase = 0x1000;
        private readonly FileStream stream;
        private readonly BinaryReader reader;

        internal RegistryHive(string path)
        {
            stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            reader = new BinaryReader(stream, Encoding.Unicode, leaveOpen: true);
            if (ReadAscii(0, 4) != "regf")
            {
                throw new InvalidDataException("Not a registry hive");
            }
        }

        internal int RootCell => ReadInt32(0x24);
        internal static readonly char[] separator = new[] { '\\' };

        internal int OpenKey(string path)
        {
            var cell = RootCell;
            foreach (var part in path.Split(separator, StringSplitOptions.RemoveEmptyEntries))
            {
                var found = -1;
                foreach (var child in EnumerateSubkeys(cell))
                {
                    if (string.Equals(ReadKeyName(child), part, StringComparison.OrdinalIgnoreCase)) { found = child; break; }
                }

                if (found < 0)
                {
                    throw new KeyNotFoundException($"Registry key not found: {path}");
                }

                cell = found;
            }
            return cell;
        }

        internal string ReadClassName(int keyCell)
        {
            RequireSignature(keyCell, "nk");
            var offset = ReadInt32(Absolute(keyCell) + 0x30);
            var length = ReadUInt16(Absolute(keyCell) + 0x4a);
            return length == 0 || offset < 0 ? string.Empty : Encoding.Unicode.GetString(ReadBytes(Absolute(offset) + 4, length)).TrimEnd('\0');
        }

        internal byte[]? ReadValue(int keyCell, string name)
        {
            RequireSignature(keyCell, "nk");
            var count = ReadInt32(Absolute(keyCell) + 0x24);
            var list = ReadInt32(Absolute(keyCell) + 0x28);
            if (count <= 0 || list < 0)
            {
                return null;
            }

            for (var i = 0; i < count; i++)
            {
                var valueCell = ReadInt32(Absolute(list) + 4 + i * 4);
                RequireSignature(valueCell, "vk");
                var at = Absolute(valueCell);
                var nameLength = ReadUInt16(at + 2);
                var ascii = (ReadUInt16(at + 0x10) & 1) != 0;
                var valueName = nameLength == 0 ? string.Empty : (ascii ? Encoding.ASCII : Encoding.Unicode).GetString(ReadBytes(at + 0x14, nameLength));
                if (!string.Equals(valueName, name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var rawLength = ReadUInt32(at + 4);
                var length = (int)(rawLength & 0x7fffffff);
                var dataOffset = at + 8;
                return (rawLength & 0x80000000) != 0 ? ReadBytes(dataOffset, Math.Min(length, 4)) : ReadBytes(Absolute(ReadInt32(dataOffset)) + 4, length);
            }
            return null;
        }

        private IEnumerable<int> EnumerateSubkeys(int keyCell)
        {
            RequireSignature(keyCell, "nk");
            var count = ReadInt32(Absolute(keyCell) + 0x14);
            var list = ReadInt32(Absolute(keyCell) + 0x1c);
            if (count <= 0 || list < 0)
            {
                yield break;
            }

            foreach (var cell in ReadSubkeyList(list))
            {
                yield return cell;
            }
        }

        private IEnumerable<int> ReadSubkeyList(int listCell)
        {
            var at = Absolute(listCell);
            var sig = ReadAscii(at + 4, 2);
            var count = ReadUInt16(at + 6);
            if (sig == "ri")
            {
                for (var i = 0; i < count; i++)
                {
                    foreach (var child in ReadSubkeyList(ReadInt32(at + 8 + i * 4)))
                    {
                        yield return child;
                    }
                }

                yield break;
            }
            var stride = sig == "li" ? 4 : sig is "lf" or "lh" ? 8 : throw new InvalidDataException($"Unknown subkey list {sig}");
            for (var i = 0; i < count; i++)
            {
                yield return ReadInt32(at + 8 + i * stride);
            }
        }

        private string ReadKeyName(int cell)
        {
            RequireSignature(cell, "nk");
            var at = Absolute(cell); var length = ReadUInt16(at + 0x48); var ascii = (ReadUInt16(at + 6) & 0x20) != 0;
            return (ascii ? Encoding.ASCII : Encoding.Unicode).GetString(ReadBytes(at + 0x4c, length));
        }

        private void RequireSignature(int cell, string expected) { if (cell < 0 || ReadAscii(Absolute(cell) + 4, 2) != expected)
            {
                throw new InvalidDataException($"Invalid {expected} cell");
            }
        }
        private static long Absolute(int relative) => HbinBase + (long)relative;
        private byte[] ReadBytes(long at, int count) { stream.Position = at; var result = reader.ReadBytes(count); if (result.Length != count) { throw new EndOfStreamException(); } return result; }
        private string ReadAscii(long at, int count) => Encoding.ASCII.GetString(ReadBytes(at, count));
        private ushort ReadUInt16(long at) { stream.Position = at; return reader.ReadUInt16(); }
        private uint ReadUInt32(long at) { stream.Position = at; return reader.ReadUInt32(); }
        private int ReadInt32(long at) { stream.Position = at; return reader.ReadInt32(); }
        public void Dispose() { reader.Dispose(); stream.Dispose(); }
    }
}
