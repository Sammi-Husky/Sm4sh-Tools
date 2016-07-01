using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SALT.Archives
{
    class PACFile : BaseFile
    {
        public PACFile()
        {
            Files = new Dictionary<string, byte[]>();
            _dataOffsets = new List<uint>();
            _strOffsets = new List<uint>();
            _sizes = new List<int>();
        }
        public PACFile(string filepath) : this()
        {
            using (var stream = File.Open(filepath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = new BinaryReader(stream))
                {
                    var tag = new String(reader.ReadChars(4));
                    if (tag == "KCAP")
                        Endian = Endianness.Big;
                    else if (tag == "PACK")
                        Endian = Endianness.Little;

                    stream.Seek(0x0C, SeekOrigin.Begin);
                    int count = 0;
                    if (Endian == Endianness.Big)
                        count = reader.ReadBint32();
                    else if (Endian == Endianness.Little)
                        count = reader.ReadInt32();

                    for (int i = 0; i < count; i++)
                        _strOffsets.Add(reader.ReadUInt32(Endian));
                    for (int i = 0; i < count; i++)
                        _dataOffsets.Add(reader.ReadUInt32(Endian));
                    for (int i = 0; i < count; i++)
                        _sizes.Add(reader.ReadInt32(Endian));

                    for (int i = 0; i < count; i++)
                    {
                        stream.Seek(_strOffsets[i], SeekOrigin.Begin);
                        var str = reader.ReadStringNT();
                        stream.Seek(_dataOffsets[i] + 0x10, SeekOrigin.Begin);
                        byte[] b = reader.ReadBytes(_sizes[i]);
                        Files.Add(str, b);
                    }
                }
            }
        }

        public Endianness Endian { get; set; }
        public Dictionary<string, byte[]> Files { get; set; }
        private List<uint> _dataOffsets;
        private List<uint> _strOffsets;
        private List<int> _sizes;

        public override void Export(string path)
        {
            File.WriteAllBytes(path, GetBytes());
        }
        public void UnpackFile(string filepath, string file)
        {
            if (Files.ContainsKey(file))
                File.WriteAllBytes(filepath, Files[file]);
        }

        public override byte[] GetBytes()
        {
            using (var dest = new MemoryStream())
            {
                var strings = Files.Keys.ToList();

                if (Endian == Endianness.Big)
                    dest.Write(Encoding.ASCII.GetBytes("KCAP"), 0, 4);
                else
                    dest.Write(Encoding.ASCII.GetBytes("PACK"), 0, 4);

                dest.Write(new byte[4], 0, 4);
                dest.Write(BitConverter.GetBytes(Endian == Endianness.Big ? Files.Count.Reverse() : Files.Count), 0, 4);
                dest.Write(new byte[4], 0, 4);

                // Write entire offset section first, we'll fill it in later
                dest.Write(new byte[Files.Count * 0x0C], 0, Files.Count * 0x0C);

                foreach (var keypair in Files)
                {
                    _sizes.Add(keypair.Value.Length);
                    _strOffsets.Add((uint)dest.Position);
                    dest.Write(Encoding.ASCII.GetBytes(keypair.Key + '\0'), 0, keypair.Key.Length + 1);
                }
                while (dest.Position % 0x10 != 0)
                    dest.WriteByte(0);

                foreach (var keypair in Files)
                {
                    _dataOffsets.Add((uint)dest.Position);
                    dest.Write(keypair.Value, 0, keypair.Value.Length);

                    while (dest.Position % 0x10 != 0)
                        dest.WriteByte(0);
                }

                dest.Seek(0x10, SeekOrigin.Begin);
                for (int i = 0; i < strings.Count; i++)
                    dest.Write(BitConverter.GetBytes(Endian == Endianness.Big ? _strOffsets[i].Reverse() : _strOffsets[i]), 0, 4);
                for (int i = 0; i < strings.Count; i++)
                    dest.Write(BitConverter.GetBytes(Endian == Endianness.Big ? _dataOffsets[i].Reverse() : _dataOffsets[i]), 0, 4);
                for (int i = 0; i < strings.Count; i++)
                    dest.Write(BitConverter.GetBytes(Endian == Endianness.Big ? _sizes[i].Reverse() : _sizes[i]), 0, 4);

                return dest.ToArray();
            }
        }

        public override int CalcSize()
        {
            int size = Files.Count * 0x0c;
            size += Files.Keys.Sum(x => x.Length + 1).RoundUp(0x10);
            size += Files.Values.Sum(x => x.Length);
            return size;
        }
    }
}
