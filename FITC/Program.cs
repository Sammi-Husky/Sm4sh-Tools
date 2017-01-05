using System;
using System.Collections.Generic;
using System.Linq;
using SALT.Scripting.AnimCMD;
using System.IO;
using SALT.Scripting;
using SALT.PARAMS;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace FitCompiler
{
    class Program
    {
        static string outputTarget = "output";

        static List<string> acmd_sources = new List<string>();
        static Endianness Endian = Endianness.Big;

        static void Main(string[] args)
        {
            Console.WriteLine($"\n> FITC v0.77 - Smash 4 Fighter Compiler platform.\n" +
                               "> Licensed under the MIT License\n" +
                               "> Copyright(c) 2016 Sammi Husky\n");

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
                                outputTarget = args[++i];
                            }
                            break;
                        case "-h":
                        case "--help":
                            print_help();
                            return;
                    }
                }
                else if (s.EndsWith(".mlist"))
                {
                    mlist = s;
                }
            }
            if (!string.IsNullOrEmpty(mlist))
            {
                compile_acmd(mlist, outputTarget);
            }
            else
            {
                print_help();
            }
        }

        public static void print_help()
        {
            Console.WriteLine("> S4FC [options] [file(s)]");
            Console.WriteLine("> Options:\n" +
                              "> \t-be: Sets the output mode to big endian (default)\n" +
                              "> \t-le: Sets the output mode to little endian\n" +
                              "> \t-o --outdir: sets the aplication output directory\n" +
                              "> \t-h --help: Displays this help message");
        }

        public static void compile_acmd(string mlist, string output)
        {
            ACMDFile game = new ACMDFile(),
                     effect = new ACMDFile(),
                     sound = new ACMDFile(),
                     expression = new ACMDFile();


            Directory.CreateDirectory(output);
            Console.WriteLine($">\tCompiling ACMD.. -> \"{output}\"");

            List<uint> hashes = new List<uint>();
            foreach (var line in File.ReadAllLines(mlist))
            {
                if (line.StartsWith("0x"))
                    hashes.Add(Convert.ToUInt32(line.Substring(2), 16));
                else
                    hashes.Add(Crc32.Compute(line.ToLower()));
            }

            foreach (var path in Directory.EnumerateFiles(Path.Combine(Path.GetDirectoryName(mlist), "animcmd"), "*", SearchOption.AllDirectories))
            {
                var defs = ACMDCompiler.CompileFile(path);
                foreach (var move in defs)
                {

#if DEBUG
                    Console.WriteLine($">\tCompiling {move.AnimName}..");
#endif

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
            table.Export(Path.Combine(output, "motion.mtable"));
            game.Export(Path.Combine(output, "game.bin"), Endian);
            sound.Export(Path.Combine(output, "sound.bin"), Endian);
            effect.Export(Path.Combine(output, "effect.bin"), Endian);
            expression.Export(Path.Combine(output, "expression.bin"), Endian);
            Console.WriteLine(">\tFinished");
        }

        public static void compile_attributes(string path, int charID, string output)
        {
            throw new NotImplementedException();
        }
    }
}
