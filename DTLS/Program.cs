using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ZLibNet;

namespace DTLS.Types
{
    internal class Program
    {
        public static ResourceManager manager;
        public static string[] DtPaths;
        public static LSFile lsFile;

        private static void Main(string[] args)
        {
            if (args.Length >= 2)
            {
                try
                {
                    string[] options = args.Where(x => x.StartsWith("-")).ToArray();
                    args = args.Skip(options.Length).Take(args.Length - options.Length).ToArray();

                    if (options.Contains("-r", StringComparer.InvariantCultureIgnoreCase))
                    {
                        if (args.Length < 3)
                        {
                            PrintUsage();
                            return;
                        }

                        DtPaths = args.Take(args.Length - 2).ToArray();
                        string lspath = args[DtPaths.Length];
                        string patchFolder = args.Last();

                        lsFile = new LSFile(lspath);
                        manager = new ResourceManager(DtPaths, lsFile);
                        manager.BuildPartitions(patchFolder);
                    }
                    else
                    {
                        DtPaths = args.Take(args.Length - 1).ToArray();
                        string lspath = args[DtPaths.Length];

                        lsFile = new LSFile(lspath);
                        manager = new ResourceManager(DtPaths, lsFile);
                        manager.UnpackAll("content_rebuild");
                    }
                    if (lsFile != null)
                        lsFile.WorkingSource.Close();
                }
                catch (Exception x)
                {
                    Console.WriteLine(x.Message);
                    throw;
                }
            }
            else if (args.Length == 0)
                PrintUsage();
        }
        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("\tUnpack dt: <dt file(s)> <ls file>");
            //Console.WriteLine("\tUnpack Update: <resource file>");
            Console.WriteLine("\tPatch Archive: -r <dt file(s)> <ls file> <patch folder>");
        }
    }
}
