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

                if (args[0].Equals("-r", StringComparison.InvariantCultureIgnoreCase) && args.Length >= 2)
                {
                    if (args.Length == 3 && args[1].Equals("-el")) { new PACKManager(Endianness.little).Pack(args[2]); }
                    else { new PACKManager(Endianness.big).Pack(args[1]); }

                    Console.WriteLine("Files successfully rePACKed.");
                }
                else
                {
                    try
                    {
                        if (args.Length == 2)
                            new PACKManager(args[0]).Unpack(args[1]);
                        else { new PACKManager(args[0]).Unpack("Output"); }

                        Console.WriteLine("Files successfully unpacked.");
                    }
                    catch (Exception x) { Console.WriteLine(x.Message); }
                }
            }
            else
            {
                Console.WriteLine("\nUsage: 'unpacker.exe [options] <Path>' \nOptions\n-r: rePACK. Default byte sex is Big Endian."
                    + "\n-eb: Endianness = big.\n-el: Endianness = little");
            }
        }

    }
    public enum Endianness
    {
        little = 0,
        big = 1
    }
}
