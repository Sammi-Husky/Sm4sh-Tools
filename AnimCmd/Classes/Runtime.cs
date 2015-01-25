using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Sm4shCommand
{
    public class Runtime
    {
        public static List<CommandDefinition> GetCommandDictionary(string path)
        {
            List<CommandDefinition> CommandDictionary = new List<CommandDefinition>();
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
                    if(raw[i+3] != "NONE")
                        h.EventDescription = raw[i + 3];
                    CommandDictionary.Add(h);
                }
            }
            return CommandDictionary;
        }

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
