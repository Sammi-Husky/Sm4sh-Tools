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
            using (StreamWriter writer = new StreamWriter("out_desc.cs"))
            {
                writer.Write("public static Dictionary<uint, string> CMD_NAMES = new Dictionary<uint, string>()\n{ ");
                for (int i = 0; i < ACMD_INFO.CMD_NAMES.Count; i++)
                {
                    var id = ACMD_INFO.CMD_NAMES.Keys.ElementAt(i);
                    var format = $"\"{ACMD_INFO.CMD_DESC[id]}\"";
                    if (string.IsNullOrEmpty(format.Trim('"')))
                        format = "\"NONE\"";
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

                for (int i = 0; i < raw.Count; i += 5)
                {
                    var crc = uint.Parse(raw[i], System.Globalization.NumberStyles.HexNumber);
                    var Name = raw[i + 1];

                    string[] paramList = raw[i + 2].Split(',').Where(x => x != "NONE").ToArray();
                    string[] paramSyntax = raw[i + 3].Split(',').Where(x => x != "NONE").ToArray();
                    ACMD_INFO.SetCMDInfo(crc, paramList.Length + 1, Name, paramList.Select(x => int.Parse(x)).ToArray(), paramSyntax);
                }
            }
        }
    }
}
