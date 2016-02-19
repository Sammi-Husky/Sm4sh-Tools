using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sm4shCommand.Classes;
using System.IO;

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

                    CommandInfo h = new CommandInfo
                    {
                        Identifier = uint.Parse(raw[i], System.Globalization.NumberStyles.HexNumber),
                        Name = raw[i + 1]
                    };
                    string[] paramList = raw[i + 2].Split(',').Where(x => x != "NONE").ToArray();
                    string[] paramSyntax = raw[i + 3].Split(',').Where(x => x != "NONE").ToArray();
                    foreach (string kw in paramSyntax)
                        h.ParamSyntax.Add(kw);
                    foreach (string s in paramList)
                        h.ParamSpecifiers.Add(int.Parse(s));
                    if (raw[i + 4] != "NONE")
                        h.EventDescription = raw[i + 4];
                    if (h.Identifier == 0x5766F889 || h.Identifier == 0x89F86657)
                        _endingCommand = h;

                    commandDictionary.Add(h);
                }
            }
        }
        public static void SaveCommandInfo(string path)
        {
            using (StreamWriter writer = new StreamWriter(path))
            {
                WriteConfigHelp(writer);
                foreach (CommandInfo def in commandDictionary)
                {
                    //Write Ident
                    writer.WriteLine(def.Identifier.ToString("X"));

                    //Write Name
                    writer.WriteLine(def.Name);

                    //Write Parameter Specifier List
                    if (def.ParamSpecifiers.Count != 0)
                        for (int i = 0; i < def.ParamSpecifiers.Count; i++)
                        {
                            writer.Write(def.ParamSpecifiers[i].ToString());
                            if (i != def.ParamSpecifiers.Count - 1)
                                writer.Write(",");
                        }
                    else
                        writer.Write("NONE");
                    writer.Write("\n");

                    //Write Parameter Syntax Keywords
                    if (def.ParamSyntax.Count != 0)
                        for (int i = 0; i < def.ParamSyntax.Count; i++)
                        {
                            writer.Write(def.ParamSyntax[i]);
                            if (i != def.ParamSyntax.Count - 1)
                                writer.Write(",");
                        }
                    else
                        writer.Write("NONE");
                    writer.Write("\n");

                    //Write Command Description
                    if (!string.IsNullOrEmpty(def.EventDescription))
                        writer.WriteLine(def.EventDescription);
                    else
                        writer.WriteLine("NONE");
                }
            }
        }
        public static void WriteConfigHelp(StreamWriter writer)
        {
            writer.WriteLine("//===========================================\\\\");
            writer.WriteLine("//**********How to use this File*************\\\\");
            writer.WriteLine("//===========================================\\\\");
            writer.WriteLine("//\tThe structure of this file is as follows:\\\\");
            writer.WriteLine("//\t\t-Command Identifier:				 \\\\");
            writer.WriteLine("//\t\t-Display Name / Dictionary Name:	 \\\\");
            writer.WriteLine("//\t\t-Parameters, separated by comma:	 \\\\");
            writer.WriteLine("//\t\t\t\t0 = Integer					 \\\\");
            writer.WriteLine("//\t\t\t\t1 = Float					 \\\\");
            writer.WriteLine("//\t\t\t\t2 = Decimal					 \\\\");
            writer.WriteLine("//\t\t\tNONE = no params				 \\\\");
            writer.WriteLine("//\t\t\t\t\t\t\t\t\t\t\t\\\\");
            writer.WriteLine("//\t\t-Parameters Keywords List			 \\\\");
            writer.WriteLine("//\t\t\t-NONE = no keywords				 \\\\");
            writer.WriteLine("//\t\t-Event Description / tooltip		 \\\\");
            writer.WriteLine("//\t\t\t-NONE = no description			 \\\\");
            writer.WriteLine("//===========================================\\\\");
            writer.WriteLine("//===========================================\\\\\n");


        }
        public static List<CommandInfo> commandDictionary = new List<CommandInfo>();
        public static CommandInfo _endingCommand;

        public static Endianness WorkingEndian { get { return _workingEndian; } set { _workingEndian = value; } }
        private static Endianness _workingEndian;

        public static bool isRoot = false;
        public static string FileName;
        public static string rootPath;

        public static ACMDFile _curFile;
        public static Fighter _curFighter;
        public static Dictionary<uint, string> AnimHashPairs = new Dictionary<uint, string>();
    }
}
