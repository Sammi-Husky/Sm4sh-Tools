using System;
using System.Collections.Generic;
using System.Linq;
using SALT.Scripting.AnimCMD;
using System.IO;
using SALT.Scripting;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace FitCompiler
{
    class Program
    {
        static string targetDir = "";

        static List<string> acmd_sources = new List<string>();
        static Endianness Endian = Endianness.Big;

        static void Main(string[] args)
        {
            bool decompile = false;

            string motionFolder = "";
            string decompDir = "";
            string mlist = "";

            for (int i = 0; i < args.Length; i++)
            {
                string s = args[i];

                // options
                if (s.StartsWith("-"))
                {
                    s = args[i];
                    switch (s)
                    {
                        case "-le":
                            Endian = Endianness.Little;
                            break;
                        case "-be":
                            Endian = Endianness.Big;
                            break;
                        case "-o":
                        case "--outdir":
                            if (i + 1 < args.Length)
                            {
                                targetDir = args[++i];
                            }
                            break;
                        case "-h":
                        case "--help":
                            print_help();
                            return;
                        case "-d":
                            if (i + 1 < args.Length)
                            {
                                decompile = true;
                                decompDir = args[++i];
                            }
                            break;
                        case "-mot":
                            if (i + 1 < args.Length)
                            {
                                motionFolder = args[++i];
                            }
                            break;
                    }
                }
                else if (s.EndsWith(".mlist"))
                {
                    mlist = s;
                }
            }
            if (!string.IsNullOrEmpty(mlist))
            {
                compile_acmd(mlist);
            }
            else if (decompile)
            {
                decompile_acmd(decompDir, motionFolder);
            }
            else
            {
                print_help();
            }
        }

        public static void print_help()
        {
            Console.WriteLine($"\nFITC v0.5.0 - Smash 4 Fighter Compiler platform.\n" +
                              "Licensed under the MIT License\n" +
                              "Copyright(c) 2016 Sammi Husky\n");

            Console.WriteLine("S4FC [options] [file(s)]");
            Console.WriteLine("Options:\n" +
                              "\t-be: Sets the output mode to big endian (default)\n" +
                              "\t-le: Sets the output mode to little endian\n" +
                              "\t-o --outdir: sets the aplication output directory\n" +
                              "\t-d: Decompile binary files into source code equivalent.\n" +
                              "\t-mot: Sets animation folder for decompilation processes\n" +
                              "\t-h --help: Displays this help message");

        }

        public static void compile_acmd(string mlist)
        {
            ACMDFile game = new ACMDFile(),
                     effect = new ACMDFile(),
                     sound = new ACMDFile(),
                     expression = new ACMDFile();



            targetDir = string.IsNullOrEmpty(targetDir) ? "bin" : targetDir;
            Directory.CreateDirectory(targetDir);

            List<uint> hashes = new List<uint>();
            foreach (var line in File.ReadAllLines(mlist))
            {
                if (line.StartsWith("0x"))
                    hashes.Add(Convert.ToUInt32(line.Substring(2), 16));
                else
                    hashes.Add(Crc32.Compute(line.ToLower()));
            }

            foreach (var path in Directory.EnumerateFiles(Path.Combine(Path.GetDirectoryName(mlist), "acmd"), "*", SearchOption.AllDirectories))
            {
                var defs = ACMDCompiler.CompileFile(path);
                foreach (var move in defs)
                {
                    Console.WriteLine($"Compiling {move.AnimName}..");

                    if (move["Main"] != null)
                    {
                        ACMDScript script = new ACMDScript(move.CRC);
                        script.Commands = move["Main"].Cast<ICommand>().ToList();
                        game.Scripts.Add(move.CRC, script);
                    }
                    if (move["Sound"] != null)
                    {
                        ACMDScript script = new ACMDScript(move.CRC);
                        script.Commands = move["Sound"].Cast<ICommand>().ToList();
                        sound.Scripts.Add(move.CRC, script);
                    }
                    if (move["Effect"] != null)
                    {
                        ACMDScript script = new ACMDScript(move.CRC);
                        script.Commands = move["Effect"].Cast<ICommand>().ToList();
                        effect.Scripts.Add(move.CRC, script);
                    }
                    if (move["Expression"] != null)
                    {
                        ACMDScript script = new ACMDScript(move.CRC);
                        script.Commands = move["Expression"].Cast<ICommand>().ToList();
                        expression.Scripts.Add(move.CRC, script);
                    }
                }
            }
            var table = new MTable(hashes, Endian);
            table.Export(Path.Combine(targetDir, "motion.mtable"));
            game.Export(Path.Combine(targetDir, "game.bin"), Endian);
            sound.Export(Path.Combine(targetDir, "sound.bin"), Endian);
            effect.Export(Path.Combine(targetDir, "effect.bin"), Endian);
            expression.Export(Path.Combine(targetDir, "expression.bin"), Endian);
        }

        public static void decompile_acmd(string acmddir, string motionFolder)
        {
            targetDir = string.IsNullOrEmpty(targetDir) ? "source" : targetDir;
            Directory.CreateDirectory(Path.Combine(targetDir, "acmd"));

            Endianness endian = Endianness.Big;
            SortedList<string, ACMDFile> files = new SortedList<string, ACMDFile>();

            Dictionary<uint, string> animations = new Dictionary<uint, string>();
            if (!string.IsNullOrEmpty(motionFolder))
                animations = ParseAnimations(motionFolder);

            foreach (string path in Directory.EnumerateFiles(acmddir, "*.bin"))
            {
                var file = new ACMDFile(path);
                files.Add(Path.GetFileNameWithoutExtension(path), file);
                endian = file.Endian;
            }
            var table = new MTable(Path.Combine(acmddir, "motion.mtable"), endian);
            var hashes = new List<uint>(table.ToList());

            write_mlist(table, animations, Path.Combine(targetDir, "fighter.mlist"));

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

                Console.WriteLine($"Decompiling {animName}..");

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

                write_movedef(game, effect, sound, expression, animName, !table.Contains(u), targetDir);
            }
        }

        private static void write_movedef(ACMDScript game, ACMDScript effect, ACMDScript sound, ACMDScript expression, string animname, bool unlisted, string outdir)
        {
            using (StreamWriter writer = new StreamWriter(Path.Combine(outdir, "acmd", $"{animname}.acm")))
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
    }
}
