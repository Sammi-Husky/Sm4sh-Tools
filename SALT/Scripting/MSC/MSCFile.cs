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
            DoParse(filepath);
        }
        public const int HEADER_Size = 0x30;
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
        private uint[] _offsets;

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

                    // Script offsets
                    uint baseAddr = (EntryOffsets + HEADER_Size).RoundUp(0x10);
                    stream.Seek(baseAddr, SeekOrigin.Begin);
                    _offsets = new uint[EntryCount];
                    for (int i = 0; i < _offsets.Length; i++)
                        _offsets[i] = reader.ReadUInt32();

                    // Scripts
                    foreach (uint off in _offsets)
                        Scripts.Add(off, ParseScript(reader, off + HEADER_Size));

                    // Strings
                    stream.Seek(baseAddr + (EntryCount * 4).RoundUp(0x10), SeekOrigin.Begin);
                    for (int i = 0; i < StringCount; i++)
                        Strings.Add(new string(reader.ReadChars(StringSize)).TrimEnd('\0'));
                }
            }
        }
        public MSCScript ParseScript(BinaryReader reader, uint offset)
        {
            MSCScript script = new MSCScript();
            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            uint ident;
            while ((ident = reader.ReadByte()) != 0x3)
            {
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
            if (ident == 0x03)
                script.Add(new MSCCommand(3));
            return script;
        }
        public byte[] GetBytes(Endianness endian)
        {
            List<byte> data = new List<byte>();
            _offsets = new uint[Scripts.Count];
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
                _offsets[i] = (uint)data.Count - 0x40;
                data.AddRange(Scripts.Values[i].GetBytes(endian));
            }
            foreach (var off in _offsets)
                data.AddRange(BitConverter.GetBytes(off));
            // Strings
            foreach (var str in Strings)
            {
                data.AddRange(Encoding.ASCII.GetBytes(str));
                for (int i = str.Length; i % StringSize > 0; i++)
                    data.Add(0x00);
            }

            return data.ToArray();
        }
        public void Export(string path)
        {
            File.WriteAllBytes(path, GetBytes(Endianness.Little));
        }
    }
}
