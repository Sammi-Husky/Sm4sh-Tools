using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sm4shCommand.Classes;
using System.IO;
using SALT.Scripting;
using SALT.Scripting.AnimCMD;
using System.ComponentModel;
using System.Windows.Forms;

namespace Sm4shCommand
{
    static class Runtime
    {
        public static AcmdMain Instance { get { return _instance; } }
        private static readonly AcmdMain _instance = new AcmdMain();

        public static void GetCommandInfo(string path)
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
                    LogMessage($"{ACMD_INFO.CMD_NAMES[crc]} -> {Name}");
                    ACMD_INFO.SetCMDInfo(crc, paramList.Length + 1, Name, paramList.Select(x => int.Parse(x)).ToArray(), paramSyntax);

                }
            }
        }
        public static Endianness WorkingEndian { get { return _workingEndian; } set { _workingEndian = value; } }
        private static Endianness _workingEndian;

        public static void LogMessage(string message)
        {
            Instance.Invoke(
                new MethodInvoker(
                    delegate { Instance.richTextBox1.AppendText($">   {message}\n"); }));
        }

        public static bool isRoot = false;
    }
}
