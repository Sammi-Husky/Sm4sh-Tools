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
        static string outputDir = "";
        static string ACMDDir { get { return Path.Combine(outputDir, "bin", "ACMD"); } }
        static List<string> acmdfiles = new List<string>();
        static Endianness endian = Endianness.Big;
        //static List<string> mscfiles = new List<string>();

        static void Main(string[] args)
        {
            List<string> options = new List<string>();
            bool dispHelp = false;

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
                            endian = Endianness.Little;
                            break;
                        case "-be":
                            endian = Endianness.Big;
                            break;
                        case "-o":
                        case "--outdir":
                            if (i + 1 < args.Length)
                            {
                                outputDir = args[++i];
                            }
                            break;
                        case "-h":
                        case "--help":
                            dispHelp = true;
                            break;
                        case "--acmddir":
                            if (i + 1 < args.Length)
                            {
                                enumerate_acmd(args[++i]);
                            }
                            break;
                    }
                }
                else if (s.EndsWith(".acmd"))
                {
                    acmdfiles.Add(s);
                }
                //else if (s.EndsWith(".mscript"))
                //{
                //    mscfiles.Add(s);
                //}
            }
            if (args.Length == 0 | dispHelp)
            {
                print_help();
            }

            if (acmdfiles.Count > 0 && !dispHelp)
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

            Console.WriteLine("S4FC [options] [files]");
            Console.WriteLine("Options:\n" +
                              "\t-be: Sets the output mode to big endian (default)\n" +
                              "\t-le: Sets the output mode to little endian\n" +
                              "\t-o --output: sets the aplication output directory\n" +
                              "\t-h --help: Displays this help message");

        }
        public static void enumerate_acmd(string dir)
        {
            foreach (var path in Directory.EnumerateFiles(dir, "*.acmd", SearchOption.AllDirectories))
            {
                acmdfiles.Add(path);
            }
        }
        public static void compile_acmd()
        {
            foreach (var path in acmdfiles)
            {
                Directory.CreateDirectory(ACMDDir);

                var defs = ACMDCompiler.CompileFile(path);
                List<uint> hashes = new List<uint>();

                foreach (var move in defs)
                {
                    hashes.Add(move.CRC);
                    if (move["Main"] != null)
                    {
                        ACMDFile file = new ACMDFile();
                        ACMDScript script = new ACMDScript(move.CRC);
                        script.Commands = move["Main"].Cast<ICommand>().ToList();
                        file.Scripts.Add(move.CRC, script);
                        file.Export(Path.Combine(ACMDDir, "game.bin"), endian);
                    }
                    if (move["Sound"] != null)
                    {
                        ACMDFile file = new ACMDFile();
                        ACMDScript script = new ACMDScript(move.CRC);
                        script.Commands = move["Sound"].Cast<ICommand>().ToList();
                        file.Scripts.Add(move.CRC, script);
                        file.Export(Path.Combine(ACMDDir, "sound.bin"), endian);
                    }
                    if (move["Effect"] != null)
                    {
                        ACMDFile file = new ACMDFile();
                        ACMDScript script = new ACMDScript(move.CRC);
                        script.Commands = move["Effect"].Cast<ICommand>().ToList();
                        file.Scripts.Add(move.CRC, script);
                        file.Export(Path.Combine(ACMDDir, "effect.bin"), endian);
                    }
                    if (move["Expression"] != null)
                    {
                        ACMDFile file = new ACMDFile();
                        ACMDScript script = new ACMDScript(move.CRC);
                        script.Commands = move["Expression"].Cast<ICommand>().ToList();
                        file.Scripts.Add(move.CRC, script);
                        file.Export(Path.Combine(ACMDDir, "expression.bin"), endian);
                    }
                }
                var table = new MTable(hashes, endian);
                table.Export(Path.Combine(ACMDDir, "motion.mtable"));
            }
        }
    }
}
