using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SALT.Scripting.MSC
{
    public class MSCFile : IScriptCollection
    {
        public MSCFile(string filepath)
        {
            Strings = new List<string>();
            Scripts = new SortedList<uint, IScript>();
            Offsets = new List<uint>();
            Sizes = new SortedList<uint, int>();

            DoParse(filepath);
        }
        public const int HEADER_SIZE = 0x30;
        public int Size
        {
            get
            {
                int i = 0x30;
                i += 4 * Scripts.Count;
                i += StringSize * Strings.Count;
                foreach (var scr in Scripts)
                    i += scr.Value.Size;
                return i;
            }
        }
        // Header Fields
        public const uint MAGIC = 0xBABCACB2;
        public const uint STUFF1 = 0x013290E6;
        public const uint STUFF2 = 0x2FD;
        public const uint STUFF3 = 0x0;
        public uint EntryOffsets { get; set; }
        public uint EntryPoint { get; set; }
        public int EntryCount { get; set; }
        public uint Unk { get; set; }
        public int StringSize { get; set; }
        public int StringCount { get; set; }
        public uint Unk1 { get; set; }
        public uint Unk2 { get; set; }

        public List<string> Strings { get; set; }
        public SortedList<uint, IScript> Scripts { get; set; }
        public SortedList<uint, int> Sizes { get; set; }
        public List<uint> Offsets { get; set; }

        public void DoParse(string path)
        {
            using (var stream = File.Open(path, FileMode.Open))
            {
                using (var reader = new BinaryReader(stream))
                {
                    // Header stuff
                    stream.Seek(0x10, SeekOrigin.Begin);
                    EntryOffsets = reader.ReadUInt32();
                    EntryPoint = reader.ReadUInt32();
                    EntryCount = reader.ReadInt32();
                    Unk = reader.ReadUInt32();
                    StringSize = reader.ReadInt32();
                    StringCount = reader.ReadInt32();
                    Unk1 = reader.ReadUInt32();
                    Unk2 = reader.ReadUInt32();

                    // Offsets
                    uint baseAddr = (EntryOffsets + HEADER_SIZE).RoundUp(0x10);
                    stream.Seek(baseAddr, SeekOrigin.Begin);
                    for (int i = 0; i < EntryCount; i++)
                        Offsets.Add(reader.ReadUInt32());
                    Offsets.Sort();

                    for (int i = 0; i < Offsets.Count; i++)
                    {
                        if (i + 1 != Offsets.Count)
                            Sizes.Add(Offsets[i], (int)(Offsets[i + 1] - Offsets[i]));
                        else
                            Sizes.Add(Offsets[i], (int)(EntryOffsets - Offsets[i]));
                    }

                    // Scripts
                    for (int i = 0; i < Offsets.Count; i++)
                    {
                        Scripts.Add(Offsets[i], ParseScript(reader, Offsets[i] + HEADER_SIZE, Sizes[Offsets[i]]));
                    }

                    // Strings
                    stream.Seek(baseAddr + (EntryCount * 4).RoundUp(0x10), SeekOrigin.Begin);
                    for (int i = 0; i < StringCount; i++)
                        Strings.Add(new string(reader.ReadChars(StringSize)).TrimEnd('\0'));
                }
            }
        }
        public MSCScript ParseScript(BinaryReader reader, uint offset, int size)
        {
            MSCScript script = new MSCScript();
            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            uint ident;
            while (reader.BaseStream.Position != offset + size)
            {
                ident = reader.ReadByte();
                var cmd = new MSCCommand(ident);
                for (int i = 0; i < cmd.ParamSpecifiers.Length; i++)
                {
                    switch (cmd.ParamSpecifiers[i])
                    {
                        case "B":
                            cmd.Parameters.Add(reader.ReadByte());
                            break;
                        case "I":
                            cmd.Parameters.Add(reader.ReadInt32());
                            break;
                    }
                }
                script.Add(cmd);
            }
            return script;
        }
        public byte[] GetBytes(Endianness endian)
        {
            List<byte> data = new List<byte>();
            Offsets = new List<uint>(EntryCount);

            // Header
            data.AddRange(BitConverter.GetBytes(MAGIC));
            data.AddRange(BitConverter.GetBytes(STUFF1));
            data.AddRange(BitConverter.GetBytes(STUFF2));
            data.AddRange(BitConverter.GetBytes(STUFF3));
            data.AddRange(BitConverter.GetBytes(EntryOffsets));
            data.AddRange(BitConverter.GetBytes(EntryPoint));
            data.AddRange(BitConverter.GetBytes(EntryCount));
            data.AddRange(BitConverter.GetBytes(Unk));
            data.AddRange(BitConverter.GetBytes(StringSize));
            data.AddRange(BitConverter.GetBytes(StringCount));
            data.AddRange(BitConverter.GetBytes(Unk1));
            data.AddRange(BitConverter.GetBytes(Unk2));
            data.AddRange(new byte[] { 0, 0, 0, 0 });
            data.AddRange(new byte[] { 0, 0, 0, 0 });
            data.AddRange(new byte[] { 0, 0, 0, 0 });
            data.AddRange(new byte[] { 0, 0, 0, 0 });

            // Scripts
            for (int i = 0; i < Scripts.Count; i++)
            {
                Offsets.Add((uint)data.Count - HEADER_SIZE);
                data.AddRange(Scripts.Values[i].GetBytes(endian));
            }
            while (data.Count % 0x10 > 0)
                data.Add(0);

            // Offsets
            foreach (var off in Offsets)
                data.AddRange(BitConverter.GetBytes(off));
            while (data.Count % 0x10 > 0)
                data.Add(0);

            // Strings
            foreach (var str in Strings)
            {
                data.AddRange(Encoding.ASCII.GetBytes(str));
                for (int i = str.Length; i % StringSize > 0; i++)
                    data.Add(0x00);
            }
            while (data.Count % 0x10 > 0)
                data.Add(0);

            return data.ToArray();
        }
        public void Export(string path)
        {
            File.WriteAllBytes(path, GetBytes(Endianness.Little));
        }
    }
}
