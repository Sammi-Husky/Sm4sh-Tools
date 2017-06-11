using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Text;
using SALT.Graphics;

namespace xmbtests
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"\n> XMBD v0.5 - Smash 4 xmb file dumper.\n" +
                   "> Licensed under the MIT License\n" +
                   "> Copyright(c) 2017 Sammi Husky\n");

            string output = "output.xms";
            bool decomp = false;
            bool recomp = false;
            string target = "";
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg.Equals("-o", StringComparison.InvariantCultureIgnoreCase))
                {
                    output = args[++i];
                }
                else if (arg.EndsWith(".xms",StringComparison.InvariantCultureIgnoreCase))
                {
                    recomp = true;
                    target = arg;
                    output = Path.GetFileNameWithoutExtension(arg) + ".xmb";
                }
                else if (arg.EndsWith(".xmb", StringComparison.InvariantCultureIgnoreCase))
                {
                    decomp = true;
                    target = arg;
                    output = Path.GetFileNameWithoutExtension(arg) + ".xms";
                }
            }
            if (decomp && !recomp)
            {
                Console.WriteLine($">\t Decompiling {Path.GetFileName(target)}.. -> \"{output}\"");
                XMBFile f = new XMBFile(target);
                f.Deserialize(output);
            }
            else if(recomp)
            {
                Console.WriteLine($">\t Compiling {Path.GetFileName(target)}.. -> \"{output}\"");
                XMBFile f = from_text(target);
                //f.Export(output);

            }
        }
        static XMBFile from_text(string filepath)
        {
            var file = new XMBFile();
            using (var reader = File.OpenText(filepath))
            {
                List<XMBEntry> tmp = new List<XMBEntry>();
                int index = 0;
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine().Trim();
                    if (string.IsNullOrEmpty(line))
                        continue;

                    var entry = new XMBEntry();
                    entry.Name = line.TrimEnd('{');
                    entry.ParentIndex = -1;
                    entry.Index = index;
                    if (line.EndsWith("{") | reader.ReadLine().EndsWith("{"))
                    {
                        bool endScope = false;
                        while (!endScope)
                        {
                            line = reader.ReadLine().Trim();
                            if (string.IsNullOrEmpty(line) | line.EndsWith("{"))
                                continue;

                            if (line.Contains('='))
                            {
                                entry.Expressions.Add(line.Trim());
                            }
                            else if (line.EndsWith("}"))
                            {
                                endScope = true;
                                continue;
                            }
                            else
                            {
                                var child = parse_entry(reader, line, ref index);
                                child.ParentIndex = (short)entry.Index;
                                entry.Children.Add(child);
                            }
                        }
                    }
                    file.Entries.Add(entry);
                }
            }
            return file;
        }
        static XMBEntry parse_entry(StreamReader reader, string name, ref int index)
        {
            index++;
            int index2 = -1; // children indexes are relative to parent
            var ret = new XMBEntry();
            ret.Index = index;
            ret.Name = name;

            bool endScope = false;
            while (!endScope)
            {
                var line = reader.ReadLine().Trim();
                if (string.IsNullOrEmpty(line) | line.EndsWith("{"))
                    continue;

                if (line.Contains('='))
                {
                    ret.Expressions.Add(line.Trim());
                }
                else if (line.EndsWith("}"))
                {
                    endScope = true;
                    continue;
                }
                else
                {

                    var child = parse_entry(reader, line, ref index2);
                    child.ParentIndex = (short)ret.Index;
                    ret.Children.Add(child);
                }
            }
            return ret;
        }
        static void print_help()
        {
            Console.WriteLine("> XMBD [xmb file] [output file]");
        }
    }
}
