using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Text;

namespace xmbtests
{
    class Program
    {
        public static int count1;
        public static int count2;
        public static int count3;
        public static int count4;

        public static int strOffsets;
        public static int entriesTable;
        public static int fieldsTable;
        public static int strTable1;
        public static int strTable2;
        public static int extraEntry;

        public static List<string> strings1 = new List<string>();
        public static List<string> strings2 = new List<string>();
        public static List<XMBEntry> entries = new List<XMBEntry>();
        public static List<string> expressions = new List<string>();

        static void Main(string[] args)
        {
            Console.WriteLine($"\n> XMBD v0.5 - Smash 4 xmb file dumper.\n" +
                   "> Licensed under the MIT License\n" +
                   "> Copyright(c) 2016 Sammi Husky\n");

            string output = "output.txt";
            if (args.Length == 2)
                output = args[1];
            else if (args.Length == 0 || args.Length > 2)
                print_help();

            parseXMB(args[0]);
            outputTXT(output);
        }

        static void parseXMB(string filename)
        {
            List<XMBEntry> temp = new List<XMBEntry>();
            using (var stream = File.Open(filename, FileMode.Open))
            {
                using (var reader = new BinaryReader(stream))
                {
                    stream.Seek(4, SeekOrigin.Begin);
                    count1 = reader.ReadBint32();
                    count2 = reader.ReadBint32();
                    count3 = reader.ReadBint32();
                    count4 = reader.ReadBint32();

                    strOffsets = reader.ReadBint32();
                    entriesTable = reader.ReadBint32();
                    fieldsTable = reader.ReadBint32();
                    extraEntry = reader.ReadBint32();
                    strTable1 = reader.ReadBint32();
                    strTable2 = reader.ReadBint32();

                    for (int i = 0; i < count3; i++)
                    {
                        stream.Seek(strOffsets + i * 4, SeekOrigin.Begin);
                        var stroff = reader.ReadBint32();
                        stream.Seek(strTable1 + stroff, SeekOrigin.Begin);
                        strings1.Add(reader.ReadStringNT());
                    }
                    for (int i = 0; i < count1; i++)
                    {
                        stream.Seek(entriesTable + i * 0x10, SeekOrigin.Begin);

                        var entry = new XMBEntry();
                        entry.NameOffset = reader.ReadBint32();
                        entry.NumProperties = reader.ReadBuint16();
                        entry.NumChildren = reader.ReadBuint16();
                        entry.FirstPropertyIndex = reader.ReadBuint16();
                        entry.unk1 = reader.ReadBuint16();
                        entry.ParentIndex = reader.ReadBuint16();
                        entry.unk2 = reader.ReadBuint16();

                        stream.Seek(strTable1 + entry.NameOffset, SeekOrigin.Begin);
                        entry.Name = reader.ReadStringNT();
                        temp.Add(entry);
                    }
                    for (int i = 0; i < count2; i++)
                    {
                        stream.Seek(fieldsTable + i * 8, SeekOrigin.Begin);
                        var stroff1 = reader.ReadBint32();
                        var stroff2 = reader.ReadBint32();
                        var str = "";
                        stream.Seek(strTable1 + stroff1, SeekOrigin.Begin);
                        str += $"{reader.ReadStringNT()} = ";
                        stream.Seek(strTable2 + stroff2, SeekOrigin.Begin);
                        str += $"{reader.ReadStringNT()}";
                        expressions.Add(str);
                    }
                    for (int x = 0; x < temp.Count; x++)
                    {
                        var entry = temp[x];
                        if (entry.NumProperties > 0)
                        {
                            for (int i = 0; i < entry.NumProperties; i++)
                            {
                                entry.Expressions.Add(expressions[entry.FirstPropertyIndex + i]);
                            }
                        }
                        if (entry.ParentIndex != 0xffff)
                        {
                            for (int i = 0; i < temp[entry.ParentIndex + i].NumChildren; i++)
                            {
                                entry.depth = temp[entry.ParentIndex + i].depth + 1; // for indent stuff and things
                                temp[entry.ParentIndex + i].Children.Add(entry);

                            }
                        }
                        else
                            entries.Add(entry);
                    }
                }
            }
        }
        static void outputTXT(string filename)
        {
            using (var writer = File.CreateText(filename))
            {
                foreach (var entry in entries)
                {
                    writer.Write(entry.deserialize());
                }
            }
        }
        static void print_help()
        {
            Console.WriteLine("> XMBD [xmb file] [output file]");
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

        public int NameOffset;
        public ushort NumProperties;
        public ushort NumChildren;
        public ushort FirstPropertyIndex;
        public ushort unk1;
        public ushort ParentIndex;
        public ushort unk2;

        public int depth = 0;

        public List<string> Expressions { get; set; }
        public List<XMBEntry> Children { get; set; }

        public string deserialize()
        {
            var sb = new StringBuilder();
            var indent = "";
            for (int i = 0; i < depth; i++)
                indent += "\t";
            sb.AppendLine($"{indent}{Name}:");
            foreach (var e in Expressions)
            {
                sb.AppendLine($"{indent}\t{e}");
            }
            foreach (var child in Children)
            {
                sb.AppendLine($"{child.deserialize()}");
            }

            return sb.ToString();
        }
    }
}
