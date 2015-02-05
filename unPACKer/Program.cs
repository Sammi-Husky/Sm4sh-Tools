using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DPack
{
    class Program
    {
        unsafe static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                List<string> strings = new List<string>();
                List<int> stringOffsets = new List<int>();
                List<int> dataOffsets = new List<int>();
                List<int> sizes = new List<int>();

                DataSource source = new DataSource(FileMap.FromFile(args[0]));
                string magic = new String((sbyte*)source.Address);
                Endianness endian = Endianness.little;

                if (magic == "PACK")
                    endian = Endianness.little;
                else if (magic == "KCAP")
                    endian = Endianness.big;
                else
                    Console.WriteLine("Not a valid PACK file");

                    try
                    {
                        if (args.Length == 2)
                            new Unpacker(source).Unpack(endian,args[1]);
                        else { new Unpacker(source).Unpack(endian); }

                        Console.WriteLine("Files successfully unpacked.");
                    }
                    catch (Exception x) { Console.WriteLine(x.Message); }

                source.Close();
            }
            else
            {
                Console.WriteLine("\n \n Usage: 'unpacker.exe <File Path> <Output Path>' \n If no output path"+
                    " is specified, files will be extracted to 'Output'.");
            }
        }
    }
    public enum Endianness
    {
        little = 0,
        big = 1
    }
}
