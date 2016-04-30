using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SALT.PARAMS
{
    public class ParamFile
    {
        public ParamFile(string filepath)
        {
            Groups = new List<IParamCollection>();
            DoParse(filepath);
        }
        public List<IParamCollection> Groups { get; set; }

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
                                if (col.Values.Count > 0 && col is ParamGroup)
                                {
                                    ((ParamGroup)col).Chunk();
                                    Groups.Add(col);
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
                        Groups.Add(col);
                    }
                }
            }
        }
    }
}
