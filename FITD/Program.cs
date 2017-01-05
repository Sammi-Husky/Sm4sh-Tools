using System;
using System.Collections.Generic;
using System.Linq;
using SALT.Scripting.AnimCMD;
using System.IO;
using SALT.Scripting.MSC;
using SALT.PARAMS;
using System.Text.RegularExpressions;
using System.Security.Cryptography;


namespace FITDccc
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"\n> FITD v0.77 - Smash 4 Fighter Decompiler\n" +
                               "> Licensed under the MIT License\n" +
                               "> Copyright(c) 2016 Sammi Husky\n");

            string target = "";
            string motion = "";
            string output = "output";
            string attributes = "";

            int charID = -1;
            if (args.Length == 0)
            {
                print_help();
                return;
            }

            for (int i = 0; i < args.Length; i++)
            {
                var str = args[i];
                if (str.StartsWith("-"))
                {
                    switch (str)
                    {
                        case "-m":
                            if (i + 1 < args.Length)
                            {
                                motion = args[++i];
                            }
                            break;
                        case "-o":
                            if (i + 1 < args.Length)
                            {
                                output = args[++i];
                            }
                            break;
                        case "-h":
                        case "--help":
                            print_help();
                            return;
                        //case "-i":
                        //case "--id":
                        //    if (i + 1 < args.Length)
                        //    {
                        //        charID = Convert.ToInt32(args[++i]);
                        //    }
                        //    break;
                    }
                }
                else if (str.EndsWith(".mtable"))
                {
                    target = str;
                }
                else if (str.EndsWith(".mscsb"))
                {
                    target = str;
                }
            }
            
            if (!string.IsNullOrEmpty(target) && target.EndsWith(".mtable"))
            {
                decompile_acmd(target, motion, output);
            }
            else if(!string.IsNullOrEmpty(target) && target.EndsWith(".mscsb"))
            {
                decompile_msc(target, output);
            }

            Console.WriteLine("> All tasks finished");
        }

        public static void print_help()
        {
            Console.WriteLine("> S4FC [options] [.mtable file]");
            Console.WriteLine("> Options:\n" +
                              //"> \t-i --id: Sets the Character ID (required for attributes)\n" +
                              //"> \t-p: Sets the Fighter Attributes Param file path\n" +
                              "> \t-o: Sets the aplication output directory\n" +
                              "> \t-m: Sets animation folder for parsing animation names\n" +
                              "> \t-h --help: Displays this help message");
        }

        public static void decompile_acmd(string mtable, string motionFolder, string output)
        {
            
            string script_dir = Path.Combine(output, "animcmd");
            Directory.CreateDirectory(script_dir);

            Console.WriteLine($">\tDecompile ACMD.. -> \"{script_dir}\"");

            Endianness endian = Endianness.Big;
            SortedList<string, ACMDFile> files = new SortedList<string, ACMDFile>();

            Dictionary<uint, string> animations = new Dictionary<uint, string>();
            if (!string.IsNullOrEmpty(motionFolder))
                animations = ParseAnimations(motionFolder);

            string dir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (mtable.Contains(Path.DirectorySeparatorChar))
                dir = Path.GetDirectoryName(mtable);

            foreach (string path in Directory.EnumerateFiles(dir, "*.bin"))
            {
                var file = new ACMDFile(path);
                files.Add(Path.GetFileNameWithoutExtension(path), file);
                endian = file.Endian;
            }
            var table = new MTable(mtable, endian);
            var hashes = new List<uint>(table.ToList());

            write_mlist(table, animations, Path.Combine(output, "fighter.mlist"));

            // workaround for unlisted moves
            foreach (var f in files)
                foreach (var s in f.Value.Scripts)
                    if (!hashes.Contains(s.Key))
                        hashes.Add(s.Key);


            foreach (uint u in hashes)
            {
                string animName = $"0x{u:X8}";
                if (animations.ContainsKey(u))
                    animName = animations[u];

#if DEBUG
                Console.WriteLine($">\tDecompiling {animName}..");
#endif

                ACMDScript game = null, effect = null, sound = null, expression = null;
                if (files.ContainsKey("game") && files["game"].Scripts.ContainsKey(u))
                {
                    game = (ACMDScript)files["game"].Scripts[u];
                }
                if (files.ContainsKey("effect") && files["effect"].Scripts.ContainsKey(u))
                {
                    effect = (ACMDScript)files["effect"].Scripts[u];
                }
                if (files.ContainsKey("sound") && files["sound"].Scripts.ContainsKey(u))
                {
                    sound = (ACMDScript)files["sound"].Scripts[u];
                }
                if (files.ContainsKey("expression") && files["expression"].Scripts.ContainsKey(u))
                {
                    expression = (ACMDScript)files["expression"].Scripts[u];
                }

                write_movedef(game, effect, sound, expression, animName, !table.Contains(u), script_dir);
            }
            Console.WriteLine(">\tFinished\n");
        }
        public static void decompile_msc(string file, string output)
        {
            MSCFile f = new MSCFile(file);
            using (var writer = File.CreateText(output + "/out.txt"))
            {
                foreach (var script in f.Scripts)
                {
                    writer.Write($"func_{script.Key:X}\n"+((MSCScript)script.Value).Decompile() + "\n\n");
                }
            }
        }
        public static Dictionary<uint, string> ParseAnimations(string motionFolder)
        {
            var dict = new Dictionary<uint, string>();
            var files = Directory.EnumerateFiles(motionFolder, "*.*", SearchOption.AllDirectories).
                Where(x => x.EndsWith(".pac", StringComparison.InvariantCultureIgnoreCase) ||
                x.EndsWith(".bch", StringComparison.InvariantCultureIgnoreCase)).Select(x => x);
            foreach (var path in files)
                ParseAnim(path, ref dict);
            return dict;
        }

        public static void ParseAnim(string path, ref Dictionary<uint, string> dict)
        {
            if (path.EndsWith(".pac"))
            {
                byte[] filebytes = File.ReadAllBytes(path);
                int count = (int)Util.GetWord(filebytes, 8, Endianness.Big);

                for (int i = 0; i < count; i++)
                {
                    uint off = (uint)Util.GetWord(filebytes, 0x10 + (i * 4), Endianness.Big);
                    string FileName = Util.GetString(filebytes, off, Endianness.Big);
                    string AnimName = Regex.Match(FileName, @"(.*)([A-Z])([0-9][0-9])(.*)\.omo").Groups[4].ToString();
                    if (string.IsNullOrEmpty(AnimName))
                        continue;

                    AddAnimHash(AnimName, ref dict);
                    AddAnimHash(AnimName + "_C2", ref dict);
                    AddAnimHash(AnimName + "_C3", ref dict);
                    AddAnimHash(AnimName + "L", ref dict);
                    AddAnimHash(AnimName + "R", ref dict);


                    if (AnimName.EndsWith("s4s", StringComparison.InvariantCultureIgnoreCase) ||
                       AnimName.EndsWith("s3s", StringComparison.InvariantCultureIgnoreCase))
                        AddAnimHash(AnimName.Substring(0, AnimName.Length - 1), ref dict);
                }
            }
            else if (path.EndsWith(".bch"))
            {
                using (var stream = File.Open(path, FileMode.Open, FileAccess.Read))
                using (var reader = new BinaryReader(stream))
                {
                    stream.Seek(0xC, SeekOrigin.Begin);
                    int off = reader.ReadInt32();
                    stream.Seek(off, SeekOrigin.Begin);

                    while (reader.PeekChar() != '\0')
                    {
                        var tmp = reader.ReadStringNT();
                        string AnimName = Regex.Match(tmp, @"(.*)([A-Z])([0-9][0-9])(.*)").Groups[4].ToString();
                        if (string.IsNullOrEmpty(AnimName))
                        {
                            continue;
                        }

                        AddAnimHash(AnimName, ref dict);
                        AddAnimHash(AnimName + "_C2", ref dict);
                        AddAnimHash(AnimName + "_C3", ref dict);
                        AddAnimHash(AnimName + "L", ref dict);
                        AddAnimHash(AnimName + "R", ref dict);


                        if (AnimName.EndsWith("s4s", StringComparison.InvariantCultureIgnoreCase) ||
                           AnimName.EndsWith("s3s", StringComparison.InvariantCultureIgnoreCase))
                            AddAnimHash(AnimName.Substring(0, AnimName.Length - 1), ref dict);
                    }
                }
            }
        }
        public static void AddAnimHash(string name, ref Dictionary<uint, string> dict)
        {
            uint crc = Crc32.Compute(name.ToLower());
            if (dict.ContainsValue(name) || dict.ContainsKey(crc))
                return;

            dict.Add(crc, name);
        }

        private static void write_movedef(ACMDScript game, ACMDScript effect, ACMDScript sound, ACMDScript expression, string animname, bool unlisted, string outdir)
        {
            using (StreamWriter writer = new StreamWriter(Path.Combine(outdir, $"{animname}.acm")))
            {
                writer.Write($"MoveDef {animname}");
                if (unlisted)
                    writer.Write(" : Unlisted\n");
                else
                    writer.Write("\n");
                writer.WriteLine("{");

                writer.WriteLine("\tMain()\n\t{");
                if (game != null)
                {
                    var commands = game.Deserialize().Split('\n');
                    foreach (string cmd in commands)
                        writer.WriteLine("\t\t" + cmd.Trim());
                }
                writer.WriteLine("\t}\n");
                writer.WriteLine("\tEffect()\n\t{");
                if (effect != null)
                {
                    var commands = effect.Deserialize().Split('\n');
                    foreach (string cmd in commands)
                        writer.WriteLine("\t\t" + cmd.Trim());
                }
                writer.WriteLine("\t}\n");
                writer.WriteLine("\tSound()\n\t{");
                if (sound != null)
                {
                    var commands = sound.Deserialize().Split('\n');
                    foreach (string cmd in commands)
                        writer.WriteLine("\t\t" + cmd.Trim());
                }
                writer.WriteLine("\t}\n");
                writer.WriteLine("\tExpression()\n\t{");
                if (expression != null)
                {
                    var commands = expression.Deserialize().Split('\n');
                    foreach (string cmd in commands)
                        writer.WriteLine("\t\t" + cmd.Trim());
                }
                writer.WriteLine("\t}\n");
                writer.WriteLine("}");
            }
        }
        private static void write_mlist(MTable table, Dictionary<uint, string> anims, string filename)
        {
            using (var writer = File.CreateText(filename))
            {
                foreach (var u in table)
                {
                    if (anims.ContainsKey(u))
                        writer.WriteLine(anims[u]);
                    else
                        writer.WriteLine($"0x{u:X8}");
                }
            }
        }
    }
}
