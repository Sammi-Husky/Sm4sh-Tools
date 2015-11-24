using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using DTLS.IO;
using ZLibNet;

namespace DTLS
{
    internal class Program
    {
        public static string[] DtPaths;
        public static LSFile lsFile;

        public static StreamWriter Logstream = new StreamWriter("log.txt");

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
                        PatchArchive("resource", patchFolder);
                        PatchArchive("resource(us_en)", patchFolder);
                        PatchArchive("resource(us_fr)", patchFolder);
                        PatchArchive("resource(us_sp)", patchFolder);
                        lsFile.WorkingSource.Close();
                        return;
                    }

                    else
                    {
                        DtPaths = args.Take(args.Length - 1).ToArray();
                        string lspath = args[DtPaths.Length];

                        lsFile = new LSFile(lspath);
                        Unpack_default("resource");
                        lsFile.WorkingSource.Close();
                        return;
                    }

                }
                catch (Exception x)
                {
                    Console.WriteLine(x.Message);
                    Logstream.WriteLine(x.Message);
                    throw;
                }
            }
            if (args.Length == 1)
                Unpack_update(args[0]);
            else
                PrintUsage();
        }
        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("\tUnpack dt: <dt file(s)> <ls file>");
            Console.WriteLine("\tUnpack Update: <resource file>");
            Console.WriteLine("\tPatch Archive: -r <dt file(s)> <ls file> <patch folder>");
        }

        /// <summary>
        /// Unpacks data from the game archive using the default resource file.
        /// </summary>
        /// <param name="resourceStr">The resource file (embedded or otherwise) to use in extraction</param>
        private static void Unpack_default(string resourceStr)
        {
            string region = "";
            if (resourceStr.Contains("("))
                region = resourceStr.Substring(resourceStr.IndexOf("(", StringComparison.Ordinal), 7);
            int totalSize = 0;


            LSEntryObject _resource = lsFile.Entries[calc_crc(resourceStr)];

            File.WriteAllBytes(resourceStr,
                GetFileDataDecompressed(_resource.DTOffset + (uint)_resource.PaddingLength, _resource.Size,
                    _resource.DTIndex));

            Console.WriteLine($"Parsing {resourceStr} file..");
            RFFile rfFile = new RFFile(resourceStr);

            var pathParts = new string[20];
            var offsetParts = new LSEntryObject[20];
            foreach (ResourceEntryObject rsobj in rfFile.ResourceEntries)
            {
                if (rsobj == null)
                    continue;

                pathParts[rsobj.FolderDepth - 1] = rsobj.EntryString;
                Array.Clear(pathParts, rsobj.FolderDepth, pathParts.Length - (rsobj.FolderDepth + 1));
                var path = string.Join("", pathParts);

                LSEntryObject fileEntry;
                if (rsobj.HasPack)
                {
                    var crcPath = $"data/{path.TrimEnd('/') + (rsobj.Compressed ? "/packed" : "")}";
                    var crc = calc_crc(crcPath);
                    lsFile.Entries.TryGetValue(crc, out fileEntry);
                    totalSize += (int)fileEntry.Size;
                }
                else
                    fileEntry = null;

                offsetParts[rsobj.FolderDepth - 1] = fileEntry;
                Array.Clear(offsetParts, rsobj.FolderDepth, offsetParts.Length - (rsobj.FolderDepth + 1));

                var outfn = $"data{region}/{path}";
                if (path.EndsWith("/"))
                {
                    if (!Directory.Exists(outfn))
                        Directory.CreateDirectory(outfn);
                }
                else
                {
                    LSEntryObject lsentry = offsetParts.Last(x => x != null);

                    var fileData = new byte[0];

                    if (rsobj.CmpSize > 0)
                        fileData = GetFileDataDecompressed(lsentry.DTOffset + rsobj.OffInPack, rsobj.CmpSize,
                            lsentry.DTIndex);


                    Console.WriteLine(outfn);
                    Logstream.WriteLine($"{outfn} : size: {rsobj.DecSize:X8}");

                    if (fileData.Length != rsobj.DecSize)
                    {
                        Console.WriteLine("Error: File length doesn't match specified decompressed length, skipping");
                        Logstream.WriteLine("Error: File length doesn't match specified decompressed length, skipping");
                    }

                    File.WriteAllBytes(outfn, fileData);
                }
            }
            // clean up
            rfFile._workingSource.Close();
            Logstream.Close();

            Console.WriteLine("Extraction finished");

            if (File.Exists("resource.dec"))
                File.Delete("resource.dec");
            if (File.Exists("resource"))
                File.Delete("resource");
        }
        private static void Unpack_update(string resFile)
        {
            Console.WriteLine("Parsing resource file..");
            RFFile rfFile = new RFFile(resFile);
            var pathParts = new string[20];
            DataSource _curPacked = new DataSource();
            string mainfolder = "";
            string region = "";

            if (resFile.Contains("("))
                region = resFile.Substring(resFile.IndexOf("(", StringComparison.Ordinal), 7);

            foreach (ResourceEntryObject rsobj in rfFile.ResourceEntries)
            {
                if (rsobj == null)
                    continue;

                pathParts[rsobj.FolderDepth - 1] = rsobj.EntryString;
                Array.Clear(pathParts, rsobj.FolderDepth, pathParts.Length - (rsobj.FolderDepth + 1));
                var path = $"data{region}/{string.Join("", pathParts)}";


                if (rsobj.HasPack)
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
                        byte[] tmp = _curPacked.Slice((int)rsobj.OffInPack, 4);


                        if (tmp[0] == 0x78 && tmp[1] == 0x9c)
                            fileData = Util.DeCompress(_curPacked.Slice((int)rsobj.OffInPack, (int)rsobj.CmpSize));
                        else
                            fileData = _curPacked.Slice((int)rsobj.OffInPack, (int)rsobj.DecSize);
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
            rfFile._workingSource.Close();
            Console.WriteLine("Extraction finished.");

            if (File.Exists($"resource{region}.dec"))
                File.Delete($"resource{region}.dec");
        }
        private static unsafe void PatchArchive(string resourceString, string patchFolder)
        {
            LSEntryObject _resource = lsFile.Entries[calc_crc(resourceString)];
            byte[] resource = GetFileDataDecompressed(_resource.DTOffset + (uint)_resource.PaddingLength,
                _resource.Size,
                _resource.DTIndex);
            File.WriteAllBytes(resourceString, resource);

            Console.WriteLine($"Patching {resourceString}");
            RFFile rfFile = new RFFile(resourceString);

            var pathParts = new string[20];
            var offsetParts = new LSEntryObject[20];

            foreach (ResourceEntryObject rsobj in rfFile.ResourceEntries)
            {
                if (rsobj == null)
                    continue;

                pathParts[rsobj.FolderDepth - 1] = rsobj.EntryString;
                Array.Clear(pathParts, rsobj.FolderDepth, pathParts.Length - (rsobj.FolderDepth + 1));
                var path = string.Join("", pathParts);

                LSEntryObject fileEntry;
                if (rsobj.HasPack)
                {
                    var crcPath = $"data/{path.TrimEnd('/') + (rsobj.Compressed ? "/packed" : "")}";
                    var crc = calc_crc(crcPath);
                    lsFile.Entries.TryGetValue(crc, out fileEntry);
                }
                else
                    fileEntry = null;

                offsetParts[rsobj.FolderDepth - 1] = fileEntry;
                Array.Clear(offsetParts, rsobj.FolderDepth, offsetParts.Length - (rsobj.FolderDepth + 1));

                if (!path.EndsWith("/"))
                    if (File.Exists($"{patchFolder}/{path}"))
                    {
                        Console.WriteLine($"Patch found: {patchFolder}/{path}");
                        Logstream.WriteLine($"Patch found: {patchFolder}/{path}");

                        LSEntryObject lsentry = offsetParts.Last(x => x != null);
                        byte[] raw = File.ReadAllBytes($"{patchFolder}/{path}");
                        byte[] compressed = Util.Compress(raw);
                        if (compressed.Length > rsobj.CmpSize + 1)
                        {
                            Console.WriteLine("Patching files larger than original not yet supported, skipping");
                            continue;
                        }
                        rsobj.CmpSize = (uint)compressed.Length;
                        rsobj.DecSize = (uint)raw.Length;
                        uint difference = 0;
                        DataSource src = GetFileChunk(lsentry.DTOffset, lsentry.Size, lsentry.DTIndex, out difference);

                        VoidPtr addr = src.Address + difference;
                        addr += rsobj.OffInPack;
                        for (int i = 0; i < compressed.Length; i++)
                            *(byte*)(addr + i) = compressed[i];

                        // write 0xCC over unused bytes.
                        addr += compressed.Length;
                        int truncateBytes = (int)rsobj.CmpSize - compressed.Length;
                        for (int i = 0; i < truncateBytes; i++)
                            *(byte*)(addr + i) = 0xCC;

                        src.Close();
                    }
            }

            // Update resource and LS files.
            rfFile.UpdateEntries();
            byte[] dec = rfFile._workingSource.Slice((int)rfFile.Header.HeaderLen1,
                (int)(rfFile._workingSource.Length - rfFile.Header.HeaderLen1));
            byte[] cmp = Util.Compress(dec);
            rfFile.Header.CompressedLen = (uint)cmp.Length;
            rfFile.Header.DecompressedLen = (uint)dec.Length;
            byte[] header = rfFile.Header.ToArray();
            byte[] full = header.Concat(cmp).ToArray();
            lsFile.Entries[calc_crc(resourceString)].Size = (uint)full.Length;
            lsFile.UpdateEntries();

            // Patch the resource data back into the DT file.
            uint diff;
            DataSource rSource = GetFileChunk(_resource.DTOffset, _resource.Size, _resource.DTIndex, out diff);
            VoidPtr curAddr = rSource.Address + diff;
            for (int i = 0; i < full.Length; i++)
            {
                *(byte*)(curAddr + i) = full[i];
            }
            rSource.Close();
            rfFile._workingSource.Close();

            if (File.Exists(resourceString))
                File.Delete(resourceString);
            if (File.Exists(resourceString + ".dec"))
                File.Delete(resourceString + ".dec");
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
        private static DataSource GetFileChunk(uint start, uint size, int dtIndex)
        {
            uint chunk_start = start;
            uint chunk_len = size;
            if (start % 0x10000 != 0)
            {
                chunk_start = start.RoundDown(0x10000);
                var difference = start - chunk_start;
                chunk_len = difference + size;
            }
            return new DataSource(FileMap.FromFile(DtPaths[dtIndex], FileMapProtect.ReadWrite, chunk_start, (int)chunk_len));
        }
        private static DataSource GetFileChunk(uint start, uint size, int dtIndex, out uint difference)
        {
            uint chunk_start = start;
            uint chunk_len = size;
            difference = 0;
            if (start % 0x10000 != 0)
            {
                chunk_start = start.RoundDown(0x10000);
                difference = start - chunk_start;
                chunk_len = difference + size;
            }
            return new DataSource(FileMap.FromFile(DtPaths[dtIndex], FileMapProtect.ReadWrite, chunk_start, (int)chunk_len));
        }

        public static uint calc_crc(string filename)
        {
            var b = Encoding.ASCII.GetBytes(filename);
            for (var i = 0; i < 4; i++)
                b[i] = (byte)(~filename[i] & 0xff);

            return CrcCalculator.CaclulateCRC32(b) & 0xFFFFFFFF;
        }
    }
}
