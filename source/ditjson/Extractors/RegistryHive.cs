using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ditjson.Extractors
{
    /// A deliberately small, read-only regf parser for key navigation, class names and values.
    internal sealed class RegistryHive : IDisposable
    {
        private const int HbinBase = 0x1000;
        private const uint Regf = (uint)('r' | ('e' << 8) | ('g' << 16) | ('f' << 24));
        private const ushort Nk = (ushort)('n' | ('k' << 8));
        private const ushort Vk = (ushort)('v' | ('k' << 8));
        private const ushort Li = (ushort)('l' | ('i' << 8));
        private const ushort Lf = (ushort)('l' | ('f' << 8));
        private const ushort Lh = (ushort)('l' | ('h' << 8));
        private const ushort Ri = (ushort)('r' | ('i' << 8));

        private readonly FileStream stream;

        internal RegistryHive(string path)
        {
            stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (ReadUInt32(0) != Regf)
            {
                stream.Dispose();
                throw new InvalidDataException("Not a registry hive");
            }
        }

        internal int RootCell => ReadInt32(0x24);

        internal int OpenKey(string path)
        {
            var cell = RootCell;
            foreach (var part in path.Split('\\', StringSplitOptions.RemoveEmptyEntries))
            {
                var found = FindSubkey(cell, part);
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
            RequireSignature(keyCell, Nk, "nk");
            var at = Payload(keyCell);
            var offset = ReadInt32(at + 0x30);
            var length = ReadUInt16(at + 0x4a);
            return length == 0 || offset < 0
                ? string.Empty
                : Encoding.Unicode.GetString(ReadBytes(Payload(offset), length)).TrimEnd('\0');
        }

        internal byte[]? ReadValue(int keyCell, string name)
        {
            RequireSignature(keyCell, Nk, "nk");
            var count = ReadInt32(Payload(keyCell) + 0x24);
            var list = ReadInt32(Payload(keyCell) + 0x28);
            if (count <= 0 || list < 0)
            {
                return null;
            }

            for (var i = 0; i < count; i++)
            {
                var valueCell = ReadInt32(Payload(list) + i * 4);
                RequireSignature(valueCell, Vk, "vk");
                var at = Payload(valueCell);
                var nameLength = ReadUInt16(at + 2);
                var ascii = (ReadUInt16(at + 0x10) & 1) != 0;
                var valueName = nameLength == 0
                    ? string.Empty
                    : (ascii ? Encoding.ASCII : Encoding.Unicode).GetString(ReadBytes(at + 0x14, nameLength));
                if (!string.Equals(valueName, name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var rawLength = ReadUInt32(at + 4);
                var length = (int)(rawLength & 0x7fffffff);
                var dataOffset = at + 8;
                return (rawLength & 0x80000000) != 0
                    ? ReadBytes(dataOffset, Math.Min(length, 4))
                    : ReadBytes(Payload(ReadInt32(dataOffset)), length);
            }

            return null;
        }

        private int FindSubkey(int keyCell, string name)
        {
            RequireSignature(keyCell, Nk, "nk");
            var count = ReadInt32(Payload(keyCell) + 0x14);
            var list = ReadInt32(Payload(keyCell) + 0x1c);
            return count <= 0 || list < 0 ? -1 : FindInSubkeyList(list, name);
        }

        private int FindInSubkeyList(int listCell, string name)
        {
            var at = Payload(listCell);
            var signature = ReadUInt16(at);
            var count = ReadUInt16(at + 2);
            if (signature == Ri)
            {
                for (var i = 0; i < count; i++)
                {
                    var found = FindInSubkeyList(ReadInt32(at + 4 + i * 4), name);
                    if (found >= 0)
                    {
                        return found;
                    }
                }

                return -1;
            }

            var stride = signature switch {
                Li => 4,
                Lf or Lh => 8,
                _ => throw new InvalidDataException($"Unknown subkey list 0x{signature:x4}")
            };

            for (var i = 0; i < count; i++)
            {
                var child = ReadInt32(at + 4 + i * stride);
                if (KeyNameEquals(child, name))
                {
                    return child;
                }
            }

            return -1;
        }

        private bool KeyNameEquals(int cell, string requestedName)
        {
            RequireSignature(cell, Nk, "nk");
            var at = Payload(cell);
            var length = ReadUInt16(at + 0x48);
            var ascii = (ReadUInt16(at + 2) & 0x20) != 0;
            if ((ascii && length != requestedName.Length) || (!ascii && length != requestedName.Length * 2))
            {
                return false;
            }

            Span<byte> buffer = length <= 256 ? stackalloc byte[length] : new byte[length];
            ReadExactly(at + 0x4c, buffer);
            for (var i = 0; i < requestedName.Length; i++)
            {
                var hiveCharacter = ascii ? (char)buffer[i] : (char)BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(i * 2, 2));
                if (!CharactersEqualIgnoreCase(hiveCharacter, requestedName[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CharactersEqualIgnoreCase(char left, char right)
        {
            return left == right || char.ToUpperInvariant(left) == char.ToUpperInvariant(right);
        }

        private void RequireSignature(int cell, ushort expected, string name)
        {
            if (cell < 0 || ReadUInt16(Payload(cell)) != expected)
            {
                throw new InvalidDataException($"Invalid {name} cell");
            }
        }

        private static long Absolute(int relative) => HbinBase + (long)relative;

        private static long Payload(int relative) => Absolute(relative) + sizeof(int);

        private byte[] ReadBytes(long offset, int count)
        {
            var result = new byte[count];
            ReadExactly(offset, result);
            return result;
        }

        private void ReadExactly(long offset, Span<byte> destination)
        {
            stream.Position = offset;
            while (!destination.IsEmpty)
            {
                var read = stream.Read(destination);
                if (read == 0)
                {
                    throw new EndOfStreamException();
                }

                destination = destination.Slice(read);
            }
        }

        private ushort ReadUInt16(long offset)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ushort)];
            ReadExactly(offset, buffer);
            return BinaryPrimitives.ReadUInt16LittleEndian(buffer);
        }

        private uint ReadUInt32(long offset)
        {
            Span<byte> buffer = stackalloc byte[sizeof(uint)];
            ReadExactly(offset, buffer);
            return BinaryPrimitives.ReadUInt32LittleEndian(buffer);
        }

        private int ReadInt32(long offset)
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            ReadExactly(offset, buffer);
            return BinaryPrimitives.ReadInt32LittleEndian(buffer);
        }

        public void Dispose() => stream.Dispose();
    }
}
