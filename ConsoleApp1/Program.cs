using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SALT.Scripting.AnimCMD;
namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            readInfo(args[0]);
            using (StreamWriter writer = new StreamWriter("out_names.cs"))
            {
                writer.Write("public static Dictionary<uint, string> CMD_NAMES = new Dictionary<uint, string>()\n{ ");
                for (int i = 0; i < ACMD_INFO.CMD_NAMES.Count; i++)
                {
                    var id = ACMD_INFO.CMD_NAMES.Keys.ElementAt(i);
                    var format = $"\"{ACMD_INFO.CMD_NAMES[id]}\"";
                    if (string.IsNullOrEmpty(format.Trim('"')))
                        format = "String.Empty";
                    writer.WriteLine($"{{0x{id:X8},{format}}},");
                }
                writer.Write("};");
            }
        }
        private static void readInfo(string path)
        {
            using (StreamReader stream = new StreamReader(path))
            {
                List<string> raw = stream.ReadToEnd().Split('\n').Select(x => x.Trim('\r')).ToList();
                raw.RemoveAll(x => String.IsNullOrEmpty(x) || String.IsNullOrWhiteSpace(x) || x.Contains("//"));

                foreach(string line in raw)
                {
                    var args = line.Split(':');
                    args[0] = args[0].Trim();
                    args[1] = args[1].Trim();
                    if (args[1] == "(UNKNOWN)")
                        continue;
                    var id = uint.Parse(args[0].Substring(2), System.Globalization.NumberStyles.HexNumber);
                    ACMD_INFO.CMD_NAMES[id] = args[1];
                }
            }
        }
    }
}
