// Copyright (c) Sammi Husky. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace SALT.PARAMS
{
    public class ParamFile : BaseFile
    {
        public ParamFile(string filepath)
        {
            this.Groups = new List<IParamCollection>();
            Filepath = filepath;

            this.DoParse(filepath);
        }

        public List<IParamCollection> Groups { get; set; }
        public string Filepath { get; set; }

        private void DoParse(string path)
        {
            using (var stream = File.Open(path, FileMode.Open))
            {
                stream.Seek(0x08, SeekOrigin.Begin);
                using (var reader = new BinaryReader(stream))
                {
                    IParamCollection col = new ParamList();
                    while (stream.Position != stream.Length)
                    {
                        ParamType type = (ParamType)stream.ReadByte();
                        switch (type)
                        {
                            case ParamType.u8:
                                col.Add(new ParamEntry(reader.ReadByte(), type));
                                break;
                            case ParamType.s8:
                                col.Add(new ParamEntry(reader.ReadByte(), type));
                                break;
                            case ParamType.u16:
                                col.Add(new ParamEntry(reader.ReadUInt16().Reverse(), type));
                                break;
                            case ParamType.s16:
                                col.Add(new ParamEntry(reader.ReadInt16().Reverse(), type));
                                break;
                            case ParamType.u32:
                                col.Add(new ParamEntry(reader.ReadUInt32().Reverse(), type));
                                break;
                            case ParamType.s32:
                                col.Add(new ParamEntry(reader.ReadInt32().Reverse(), type));
                                break;
                            case ParamType.f32:
                                col.Add(new ParamEntry(reader.ReadSingle().Reverse(), type));
                                break;
                            case ParamType.str:
                                int len = reader.ReadInt32().Reverse();
                                col.Add(new ParamEntry(new string(reader.ReadChars(len)), type));
                                break;
                            case ParamType.group:
                                if (col.Values.Count > 0)
                                {
                                    if (col is ParamGroup)
                                        ((ParamGroup)col).Chunk();
                                    this.Groups.Add(col);
                                }

                                col = new ParamGroup();
                                int count = reader.ReadInt32().Reverse();
                                ((ParamGroup)col).EntryCount = count;
                                break;
                            default:
                                throw new NotImplementedException($"unk typecode: {type} at offset: {stream.Position:X}");
                        }
                    }

                    if (col.Values.Count > 0)
                    {
                        if (col is ParamGroup)
                            ((ParamGroup)col).Chunk();
                        this.Groups.Add(col);
                    }
                }
            }
        }

        public override void Export(string filepath)
        {
            File.WriteAllBytes(filepath, GetBytes());
        }
        public override int CalcSize() { return Groups.Sum(x => x.CalcSize()); }
        public override byte[] GetBytes()
        {
            using (var stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(0x0000FFFF);
                    writer.Write(0);
                    foreach (IParamCollection col in this.Groups)
                    {
                        byte[] data = null;
                        data = col.GetBytes();

                        if (data != null)
                            writer.Write(data, 0, data.Length);
                    }
                }
                return stream.ToArray();
            }
        }
    }
}
