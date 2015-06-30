using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Sm4shCommand
{
    public static class Runtime
    {
        public static void GetCommandDictionary(string path)
        {
            using (StreamReader stream = new StreamReader(path))
            {
                List<string> raw = stream.ReadToEnd().Split('\n').Select(x => x.Trim('\r')).ToList();
                raw.RemoveAll(x => String.IsNullOrEmpty(x) || String.IsNullOrWhiteSpace(x) || x.Contains("//"));

                for (int i = 0; i < raw.Count; i += 4)
                {
 
                    CommandDefinition h = new CommandDefinition();
                    h.Identifier = uint.Parse(raw[i], System.Globalization.NumberStyles.HexNumber);
                    h.Name = raw[i + 1];
                    string[] tmp = raw[i + 2].Split(',').Where(x => x != "NONE").ToArray();
                    foreach (string s in tmp)
                        h.ParamSpecifiers.Add(Int32.Parse(s));
                    if (raw[i + 3] != "NONE")
                        h.EventDescription = raw[i + 3];
                    if (h.Identifier == 0x5766F889 || h.Identifier == 0x89F86657)
                        _endingCommand = h;

                    commandDictionary.Add(h);
                }
            }
        }

        public static List<CommandDefinition> commandDictionary = new List<CommandDefinition>();
        public static CommandDefinition _endingCommand;

        //public static eDictionary GetSyntaxInfo(string path)
        //{
        //    eDictionary CommandDictionary = new eDictionary();
        //    using (StreamReader stream = new StreamReader(path))
        //    {
        //        List<string> raw = stream.ReadToEnd().Split('\n').Select(x => x.Trim('\r')).ToList();
        //        raw.RemoveAll(x => String.IsNullOrEmpty(x) || x.Contains("//"));

        //        for (int i = 0; i < raw.Count; i += 3)
        //        {
        //            CommandDefinition h = new CommandDefinition();
        //            h.Identifier = uint.Parse(raw[i], System.Globalization.NumberStyles.HexNumber);
        //            h.Name = raw[i + 1];
        //            string[] tmp = raw[i + 2].Split(',').Where(x => x != "NONE").ToArray();
        //            foreach (string s in tmp)
        //                h.ParamSpecifiers.Add(Int32.Parse(s));
        //            CommandDictionary.Add(h);
        //        }
        //    }
        //    return CommandDictionary;
        //}
    }
}
