// Copyright (c) Sammi Husky. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SALT.Scripting.MSC
{
    public class MSCFile : IScriptCollection
    {
        public MSCFile(string filepath)
        {
            this.Strings = new List<string>();
            this.Scripts = new SortedList<uint, IScript>();
            this.Offsets = new List<uint>();
            this.Sizes = new SortedList<uint, int>();

            this.DoParse(filepath);
        }

        public const int HEADER_SIZE = 0x30;

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

        public int Size
        {
            get
            {
                int i = 0x30;
                i += 4 * this.Scripts.Count;
                i += this.StringSize * this.Strings.Count;
                foreach (var scr in this.Scripts)
                    i += scr.Value.Size;
                return i;
            }
        }
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
                    this.EntryOffsets = reader.ReadUInt32();
                    this.EntryPoint = reader.ReadUInt32();
                    this.EntryCount = reader.ReadInt32();
                    this.Unk = reader.ReadUInt32();
                    this.StringSize = reader.ReadInt32();
                    this.StringCount = reader.ReadInt32();
                    this.Unk1 = reader.ReadUInt32();
                    this.Unk2 = reader.ReadUInt32();

                    // Offsets
                    uint baseAddr = (this.EntryOffsets + HEADER_SIZE).RoundUp(0x10);
                    stream.Seek(baseAddr, SeekOrigin.Begin);
                    for (int i = 0; i < this.EntryCount; i++)
                        this.Offsets.Add(reader.ReadUInt32());
                    this.Offsets.Sort();

                    for (int i = 0; i < this.Offsets.Count; i++)
                    {
                        if (i + 1 != this.Offsets.Count)
                            this.Sizes.Add(this.Offsets[i], (int)(this.Offsets[i + 1] - this.Offsets[i]));
                        else
                            this.Sizes.Add(this.Offsets[i], (int)(this.EntryOffsets - this.Offsets[i]));
                    }

                    // Scripts
                    for (int i = 0; i < this.Offsets.Count; i++)
                    {
                        this.Scripts.Add(this.Offsets[i], this.ParseScript(reader, this.Offsets[i] + HEADER_SIZE, this.Sizes[this.Offsets[i]]));
                    }

                    // Strings
                    stream.Seek(baseAddr + (this.EntryCount * 4).RoundUp(0x10), SeekOrigin.Begin);
                    for (int i = 0; i < this.StringCount; i++)
                        this.Strings.Add(new string(reader.ReadChars(this.StringSize)).TrimEnd('\0'));
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
            this.Offsets = new List<uint>(this.EntryCount);

            // Header
            data.AddRange(BitConverter.GetBytes(MAGIC));
            data.AddRange(BitConverter.GetBytes(STUFF1));
            data.AddRange(BitConverter.GetBytes(STUFF2));
            data.AddRange(BitConverter.GetBytes(STUFF3));
            data.AddRange(BitConverter.GetBytes(this.EntryOffsets));
            data.AddRange(BitConverter.GetBytes(this.EntryPoint));
            data.AddRange(BitConverter.GetBytes(this.EntryCount));
            data.AddRange(BitConverter.GetBytes(this.Unk));
            data.AddRange(BitConverter.GetBytes(this.StringSize));
            data.AddRange(BitConverter.GetBytes(this.StringCount));
            data.AddRange(BitConverter.GetBytes(this.Unk1));
            data.AddRange(BitConverter.GetBytes(this.Unk2));
            data.AddRange(new byte[] { 0, 0, 0, 0 });
            data.AddRange(new byte[] { 0, 0, 0, 0 });
            data.AddRange(new byte[] { 0, 0, 0, 0 });
            data.AddRange(new byte[] { 0, 0, 0, 0 });

            // Scripts
            for (int i = 0; i < this.Scripts.Count; i++)
            {
                this.Offsets.Add((uint)data.Count - HEADER_SIZE);
                data.AddRange(this.Scripts.Values[i].GetBytes(endian));
            }

            while (data.Count % 0x10 > 0)
                data.Add(0);

            // Offsets
            foreach (var off in this.Offsets)
                data.AddRange(BitConverter.GetBytes(off));
            while (data.Count % 0x10 > 0)
                data.Add(0);

            // Strings
            foreach (var str in this.Strings)
            {
                data.AddRange(Encoding.ASCII.GetBytes(str));
                for (int i = str.Length; i % this.StringSize > 0; i++)
                    data.Add(0x00);
            }

            while (data.Count % 0x10 > 0)
                data.Add(0);

            return data.ToArray();
        }

        public void Export(string path)
        {
            this.Export(path, Endianness.Little);
        }

        public void Export(string path, Endianness endian)
        {
            File.WriteAllBytes(path, this.GetBytes(endian));
        }
    }
}
