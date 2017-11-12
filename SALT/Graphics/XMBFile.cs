using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SALT.Graphics
{
    public class XMBFile
    {
        public XMBFile()
        {

        }
        public XMBFile(string filepath)
        {
            parseXMB(filepath);
        }
        private int StringCount { get; set; }
        private int EntryCount { get; set; }
        private int NumValues { get; set; }
        private int Count4 { get; set; }

        private int pStrOffsets { get; set; }
        private int pEntriesTable { get; set; }
        private int pPropertiesTable { get; set; }
        private int extraEntry { get; set; }
        private int pStrTable1 { get; set; }
        private int pStrTable2 { get; set; }

        public List<string> Properties = new List<string>();
        public List<string> Values = new List<string>();

        public List<XMBEntry> Entries = new List<XMBEntry>();

        public void parseXMB(string filename)
        {
            List<XMBEntry> temp = new List<XMBEntry>();
            List<string> expressions = new List<string>();
            using (var stream = File.Open(filename, FileMode.Open))
            {
                using (var reader = new BinaryReader(stream))
                {
                    stream.Seek(4, SeekOrigin.Begin);
                    EntryCount = reader.ReadBint32();
                    NumValues = reader.ReadBint32();
                    StringCount = reader.ReadBint32();
                    Count4 = reader.ReadBint32();

                    pStrOffsets = reader.ReadBint32();
                    pEntriesTable = reader.ReadBint32();
                    pPropertiesTable = reader.ReadBint32();
                    extraEntry = reader.ReadBint32();
                    pStrTable1 = reader.ReadBint32();
                    pStrTable2 = reader.ReadBint32();

                    for (int i = 0; i < StringCount; i++)
                    {
                        stream.Seek(pStrOffsets + i * 4, SeekOrigin.Begin);
                        var stroff = reader.ReadBint32();
                        stream.Seek(pStrTable1 + stroff, SeekOrigin.Begin);
                        Properties.Add(reader.ReadStringNT());
                    }
                    for (int i = 0; i < EntryCount; i++)
                    {
                        stream.Seek(pEntriesTable + i * 0x10, SeekOrigin.Begin);

                        var entry = new XMBEntry();
                        int NameOffset = reader.ReadBint32();
                        entry.NumExpressions = reader.ReadBuint16();
                        entry.NumChildren = reader.ReadBuint16();
                        entry.FirstPropertyIndex = reader.ReadBuint16();
                        entry.unk1 = reader.ReadBuint16();
                        entry.ParentIndex = reader.ReadBint16();
                        entry.unk2 = reader.ReadBuint16();

                        stream.Seek(pStrTable1 + NameOffset, SeekOrigin.Begin);
                        entry.Name = reader.ReadStringNT();
                        temp.Add(entry);
                    }
                    for (int i = 0; i < NumValues; i++)
                    {
                        stream.Seek(pPropertiesTable + i * 8, SeekOrigin.Begin);
                        var stroff1 = reader.ReadBint32();
                        var stroff2 = reader.ReadBint32();
                        var str = "";
                        stream.Seek(pStrTable1 + stroff1, SeekOrigin.Begin);
                        str += $"{reader.ReadStringNT()} = ";
                        stream.Seek(pStrTable2 + stroff2, SeekOrigin.Begin);
                        var value = reader.ReadStringNT();
                        Values.Add(value);
                        str += value;
                        expressions.Add(str);
                    }
                    for (int x = 0; x < temp.Count; x++)
                    {
                        var entry = temp[x];
                        if (entry.NumExpressions > 0)
                        {
                            for (int i = 0; i < entry.NumExpressions; i++)
                            {
                                entry.Expressions.Add(expressions[entry.FirstPropertyIndex + i]);
                            }
                        }
                        if (entry.ParentIndex != -1)
                        {
                            entry.Parent = temp[entry.ParentIndex];
                            for (int i = 0; i < temp[entry.ParentIndex + i].NumChildren; i++)
                            {
                                entry.depth = temp[entry.ParentIndex + i].depth + 1; // for indent stuff and things
                                temp[entry.ParentIndex + i].Children.Add(entry);

                            }
                        }
                        else
                        {
                            Entries.Add(entry);
                        }
                    }
                }
            }
        }
        public void Deserialize(string filename)
        {
            using (var writer = File.CreateText(filename))
            {
                foreach (var entry in Entries)
                {
                    writer.Write(entry.deserialize());
                }
            }
        }
        public byte[] GetBytes()
        {
            List<byte> data = new List<byte>();
            return null;
        }
    }
    public class XMBEntry
    {
        public XMBEntry()
        {
            Expressions = new List<string>();
            Children = new List<XMBEntry>();
        }
        public string Name { get; set; }

        public ushort NumExpressions;
        public ushort NumChildren;
        public ushort FirstPropertyIndex;
        public ushort unk1;
        public short ParentIndex;
        public ushort unk2;

        public int depth = 0;
        public int Index = 0;
        public List<string> Expressions { get; set; }
        public List<XMBEntry> Children { get; set; }

        public XMBEntry Parent { get; set; }

        public string deserialize()
        {
            var sb = new StringBuilder();

            var indent = "";
            for (int i = 0; i < depth; i++)
                indent += "\t";
            
            sb.AppendLine($"{indent}{Name}({NumExpressions:X}, {NumChildren:X}, {FirstPropertyIndex:X}, {unk1:X}, {ParentIndex:X}, {unk2:X})");
            //sb.AppendLine($"{indent}{Name}");
            sb.AppendLine($"{indent}{{");
            foreach (var e in Expressions)
            {
                sb.AppendLine($"{indent}\t{e}");
            }
            foreach (var child in Children)
            {
                sb.AppendLine($"{child.deserialize()}");
            }
            sb.AppendLine($"{indent}}}");

            return sb.ToString();
        }
    }
}
