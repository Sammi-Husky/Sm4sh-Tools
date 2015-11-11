using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DTLS.IO;
using ZLibNet;
using DTLS.Types;

namespace DTLS
{
    internal class Program
    {
        public static DataSource ResourceDec;
        public static string[] dtPaths;

        public static StreamWriter Logstream = new StreamWriter("log.txt");

        private static void Main(string[] args)
        {

            if (args.Length >= 3)
            {
                string lspath = args[args.Length - 2];
                string outpath = args.Last();
                dtPaths = args.Take(args.Length - 2).ToArray();
                try
                {
                    Unpack_default(lspath, outpath);
                }
                catch (Exception x)
                {
                    Console.WriteLine(x.Message);
                    Logstream.WriteLine(x.Message);
                    throw;
                }
            }
            else if (args.Length == 1)
                Unpack_patch(args[0]);
            else
            {
                Console.WriteLine("Usage:\nUnpack dt :  <dt file(s)> <ls file> <output path>");
                Console.WriteLine("Unpack patch : <resource file>");
            }
        }

        /// <summary>
        /// Unpacks data from the game archive using the default resource file.
        /// </summary>
        /// <param name="ls">path to the ls file to use.</param>
        /// <param name="output">Output directory to place extracted contents</param>
        private static void Unpack_default(string ls, string output)
        {
            LSFile lsFile = new LSFile(ls);

            LSEntryObject _resource = lsFile.Entries[Util.calc_crc("resource")];
            byte[] resource = GetFileDataDecompressed(_resource.DTOffset + (uint)_resource.PaddingLength, _resource.Size,
                _resource.DTIndex);
            File.WriteAllBytes("resource", resource);

            Console.WriteLine("Parsing resource file..");
            RFFile RfFile = new RFFile("resource");

            var pathParts = new string[20];
            var offsetParts = new LSEntryObject[20];
            foreach (ResourceEntryObject rsobj in RfFile.ResourceEntries)
            {
                if (rsobj == null)
                    continue;

                pathParts[rsobj.FolderDepth - 1] = rsobj.EntryString;
                Array.Clear(pathParts, rsobj.FolderDepth, pathParts.Length - (rsobj.FolderDepth + 1));
                var path = string.Join("", pathParts);

                LSEntryObject fileEntry;
                if (rsobj.HasFiles)
                {
                    var crcPath = "data/" + path.TrimEnd('/') + (rsobj.Compressed ? "/packed" : "");
                    var crc = Util.calc_crc(crcPath);
                    fileEntry = lsFile.Entries[crc];
                }
                else
                    fileEntry = null;

                offsetParts[rsobj.FolderDepth - 1] = fileEntry;
                Array.Clear(offsetParts, rsobj.FolderDepth, offsetParts.Length - (rsobj.FolderDepth + 1));

                var outfn = $"{output}/{path}";
                if (path.EndsWith("/"))
                {
                    if (!Directory.Exists(outfn))
                        Directory.CreateDirectory(outfn);
                }
                else
                {
                    LSEntryObject lsentry = offsetParts.Last(x => x != null && x.Size > 0);

                    var fileData = new byte[0];

                    if (rsobj.CmpSize > 0)
                        fileData = GetFileDataDecompressed(lsentry.DTOffset + rsobj.OffInChunk, (uint)rsobj.CmpSize, lsentry.DTIndex);

                    Console.WriteLine(outfn);
                    Logstream.WriteLine($"{outfn} : size: {rsobj.DecSize:X8}");

                    if (fileData.Length != rsobj.DecSize)
                    {
                        Console.WriteLine("Error: File length doesn't match specified decompressed length, quiting");
                        Logstream.WriteLine("Error: File length doesn't match specified decompressed length, quiting");
                        return;
                    }

                    File.WriteAllBytes(outfn, fileData);
                }
            }

            // clean up
            ResourceDec.Close();
            Logstream.Close();

            Console.WriteLine("Extraction finished");

            if (File.Exists("resource.dec"))
                File.Delete("resource.dec");
            if (File.Exists("resource"))
                File.Delete("resource");
        }
        private static void Unpack_patch(string resPath)
        {
            Console.WriteLine("Parsing resource file..");
            RFFile RfFile = new RFFile(resPath);
            var pathParts = new string[20];
            DataSource _curPacked = new DataSource();
            string mainfolder = "";
            string region = "";

            if (resPath.Contains("("))
                region = resPath.Substring(resPath.IndexOf("(", StringComparison.Ordinal), 7);

            foreach (ResourceEntryObject rsobj in RfFile.ResourceEntries)
            {
                if (rsobj == null)
                    continue;

                pathParts[rsobj.FolderDepth - 1] = rsobj.EntryString;
                Array.Clear(pathParts, rsobj.FolderDepth, pathParts.Length - (rsobj.FolderDepth + 1));
                var path = $"data{region}/{string.Join("", pathParts)}";


                if (rsobj.HasFiles)
                {
                    path += (rsobj.Compressed ? "packed" : "");
                    if (File.Exists(path))
                    {
                        _curPacked = new DataSource(FileMap.FromFile(path));
                        mainfolder = path.Remove(path.Length - 6);
                    }
                    continue;
                }

                if (!(rsobj.inPatch && path.Contains(mainfolder) && !string.IsNullOrEmpty(mainfolder)))
                    continue;

                if (path.EndsWith("/"))
                {
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);
                }
                else
                {

                    var fileData = new byte[0];

                    if (rsobj.CmpSize > 0)
                    {
                        byte[] tmp = _curPacked.Slice((int)rsobj.OffInChunk, 4);


                        if (tmp[0] == 0x78 && tmp[1] == 0x9c)
                            fileData = Util.DeCompress(_curPacked.Slice((int) rsobj.OffInChunk, rsobj.CmpSize));
                        else
                            fileData = _curPacked.Slice((int) rsobj.OffInChunk, rsobj.DecSize);
                    }

                    Console.WriteLine(path);
                    Logstream.WriteLine($"{path} : size: {rsobj.DecSize:X8}");

                    if (fileData.Length != rsobj.DecSize)
                    {
                        Console.WriteLine("Error: File length doesn't match specified decompressed length, quiting");
                        Logstream.WriteLine("Error: File length doesn't match specified decompressed length, quiting");
                        return;
                    }

                    File.WriteAllBytes(path, fileData);
                }
            }
            Logstream.Close();
            Console.WriteLine("Extraction finished.");

            if (File.Exists($"resource{region}.dec"))
                File.Delete($"resource{region}.dec");
        }
        /// <summary>
        /// Returns file data from the dt file with an index of "dtIndex"
        /// </summary>
        /// <param name="start"></param>
        /// <param name="size"></param>
        /// <param name="dtIndex"></param>
        /// <returns></returns>
        private static byte[] GetFileDataDecompressed(uint start, uint size, int dtIndex)
        {
            uint chunk_start = start.RoundDown(0x10000);
            uint difference = start - chunk_start;
            uint chunk_len = difference + size;

            DataSource src = GetFileChunk(chunk_start, chunk_len, dtIndex);

            byte[] b = src.Slice((int)difference, (int)size);

            if (b.Length >= 4)
            {
                var z = 0;
                if (BitConverter.ToUInt32(b, 0) == 0xCCCCCCCC)
                {
                    while (b[z] != 0x78)
                        z += 2;
                }

                if (b[0] == 0x78 && b[1] == 0x9c)
                    b = Util.DeCompress(b.Skip(z).ToArray());
            }
            src.Close();

            return b;
        }
        /// <summary>
        /// Returns a DataSource object containing data at "Start". 
        /// </summary>
        /// <param name="start"> Start of chunk. Must be multiple of System Allocation Granularity</param>
        /// <param name="size"></param>
        /// <param name="dtIndex"></param>
        /// <returns></returns>
        private static unsafe DataSource GetFileChunk(uint start, uint size, int dtIndex)
        {
            uint chunk_start = start;
            uint chunk_len = size;
            if (start % 0x10000 != 0)
            {
                chunk_start = start.RoundDown(0x10000);
                var difference = start - chunk_start;
                chunk_len = difference + size;
            }
            return new DataSource(FileMap.FromFile(dtPaths[dtIndex], FileMapProtect.ReadWrite, chunk_start, (int)chunk_len));
        }
    }
}