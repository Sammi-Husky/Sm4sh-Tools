using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SALT.Scripting.AnimCMD;
using System.IO;
using SALT.Scripting;

namespace FitCompiler
{
    class Program
    {
        static string targetDir = "bin";
        static List<string> acmd_sources = new List<string>();
        static List<CLI_OPTION> options = new List<CLI_OPTION>();

        static Endianness Endian = Endianness.Big;
        static bool decompile = false;

        //static List<string> mscfiles = new List<string>();

        static void Main(string[] args)
        {
            List<string> options = new List<string>();
            bool dispHelp = false;

            var test = args.TakeWhile(x => x.StartsWith("-"));
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
                            dispHelp = true;
                            break;
                        case "-dec":
                        case "--decompile":
                            if (i + 1 < args.Length)
                            {
                                decompile = true;
                                decompile_acmd(args[++i], "");
                                return;
                            }
                            break;
                    }
                }
                else if (s.EndsWith(".acmd"))
                {
                    acmd_sources.Add(s);
                }
                //else if (s.EndsWith(".mscript"))
                //{
                //    mscfiles.Add(s);
                //}
                else if (!s.Contains(".") && Directory.Exists(s))
                {
                    enumerate_acmd(s);
                }
            }

            if (args.Length == 0 | dispHelp)
            {
                print_help();
            }

            if (acmd_sources.Count > 0 && !dispHelp)
            {
                compile_acmd();
            }
            //if (mscfiles.Count > 0 && !dispHelp)
            //{
            //    compile_msc();
            //}
        }

        public static void print_help()
        {
            Console.WriteLine("\nS4FC - Smash 4 Fighter Compiler platform.\n" +
                              "Licensed under the MIT License\n" +
                              "Copyright(c) 2016 Sammi Husky\n");

            Console.WriteLine("S4FC [options] [files/directory]");
            Console.WriteLine("Options:\n" +
                              "\t-be: Sets the output mode to big endian (default)\n" +
                              "\t-le: Sets the output mode to little endian\n" +
                              "\t-o --outdir: sets the aplication output directory\n" +
                              "\t-h --help: Displays this help message");

        }
        public static void enumerate_acmd(string dir)
        {
            foreach (var path in Directory.EnumerateFiles(dir, "*.acmd", SearchOption.AllDirectories))
            {
                acmd_sources.Add(path);
            }
        }
        public static void compile_acmd()
        {
            ACMDFile game = new ACMDFile(),
                     effect = new ACMDFile(),
                     sound = new ACMDFile(),
                     expression = new ACMDFile();

            List<uint> hashes = new List<uint>();

            Directory.CreateDirectory(targetDir);
            foreach (var path in acmd_sources)
            {
                Console.WriteLine($"Compiling {Path.GetFileNameWithoutExtension(path)}..");

                var defs = ACMDCompiler.CompileFile(path);

                foreach (var move in defs)
                {
                    hashes.Add(move.CRC);
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

        public static void decompile_acmd(string acmddir, string animfile)
        {
            Endianness endian = Endianness.Big;
            SortedList<string, ACMDFile> files = new SortedList<string, ACMDFile>();

            foreach (string path in Directory.EnumerateFiles(acmddir, "*.bin"))
            {
                var file = new ACMDFile(path);
                files.Add(Path.GetFileNameWithoutExtension(path), file);
                endian = file.Endian;
            }
            var table = new MTable(Path.Combine(acmddir, "Motion.mtable"), endian);

            foreach (uint u in table)
            {
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

                Directory.CreateDirectory("source");
                write_movedef(game, effect, sound, expression, Path.Combine("source", $"{u.ToString("X8")}.acmd"), u.ToString("X8"));
            }
        }

        private static void write_movedef(ACMDScript game, ACMDScript effect, ACMDScript sound, ACMDScript expression, string path, string animname)
        {
            using (StreamWriter writer = new StreamWriter(path))
            {
                uint num = 0;
                if (uint.TryParse(animname, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out num))
                    animname = $"0x{num:X8}";

                writer.WriteLine($"MoveDef {animname}");
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

        public struct CLI_OPTION
        {
            public string Option;
            public string value;
        }
    }
}
