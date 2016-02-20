using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using DTLS.IO;
using ZLibNet;
using System.Collections.Generic;

namespace DTLS.Types
{
    internal class Program
    {
        public static string[] DtPaths;
        public static LSFile lsFile;

        private static string[] eu = { "eu_en", "eu_fr", "eu_sp", "eu_gr", "eu_it", "eu_ne", "eu_po", "eu_ru" };
        private static string[] us = { "us_en", "us_fr", "us_sp" };
        private static string[] jp = { "jp_jp" };
        public static string[][] regions = { eu, us, jp };

        private static StreamWriter Logstream;
        private static int regionCode = 0;

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
                        Logstream = new StreamWriter("log.txt");
                        PatchArchive("resource", patchFolder);
                        foreach (string reg in regions[regionCode])
                            PatchArchive($"resource({reg})", patchFolder);
                        lsFile.WorkingSource.Close();
                    }
                    else
                    {
                        DtPaths = args.Take(args.Length - 1).ToArray();
                        string lspath = args[DtPaths.Length];

                        lsFile = new LSFile(lspath);
                        Logstream = new StreamWriter("log.txt");
                        Unpack_All();
                        lsFile.WorkingSource.Close();
                    }

                }
                catch (Exception x)
                {
                    Console.WriteLine(x.Message);
                    Logstream.WriteLine(x.Message);
                    Logstream.Close();
                    throw;
                }
            }
            if (args.Length == 1)
                Unpack_update(args[0]);
            else if (args.Length == 0)
                PrintUsage();

            if(Logstream != null) Logstream.Close();
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
        /// <param name="resourceStr">The resource file to use in extraction</param>
        private static void Unpack_Resource(string resourceStr)
        {
            string region = "";
            if (resourceStr.Contains("("))
                region = resourceStr.Substring(resourceStr.IndexOf("(", StringComparison.Ordinal), 7);

            LSEntryObject _resource = lsFile.Entries[calc_crc(resourceStr)];
            File.WriteAllBytes(resourceStr,
                GetFileDataDecompressed(_resource.DTOffset + (uint)_resource.PaddingLength, _resource.Size,
                    _resource.DTIndex));

            Console.WriteLine($"Parsing {resourceStr} file..");
            RFFile rfFile = new RFFile(resourceStr);
            regionCode = rfFile.Header.RegionEtc & 3;

            var pathParts = new string[20];
            var offsetParts = new LSEntryObject[20];
            foreach (ResourceEntryObject rsobj in rfFile.ResourceEntries)
            {
                if (rsobj == null)
                    continue;

                pathParts[rsobj.FolderDepth - 1] = rsobj.EntryString;
                Array.Clear(pathParts, rsobj.FolderDepth, pathParts.Length - (rsobj.FolderDepth));
                var path = $"data{region}/{string.Join("", pathParts)}";

                LSEntryObject fileEntry;
                if (rsobj.HasPack)
                {
                    var crcPath = "";
                    crcPath = $"{path}{(rsobj.Compressed ? "packed" : "")}";
                    Console.WriteLine(crcPath);
                    Logstream.WriteLine(crcPath);
                    var crc = calc_crc(crcPath);
                    lsFile.Entries.TryGetValue(crc, out fileEntry);
                    lsFile.Entries.Remove(crc);
                }
                else
                    fileEntry = null;

                offsetParts[rsobj.FolderDepth - 1] = fileEntry;
                Array.Clear(offsetParts, rsobj.FolderDepth, offsetParts.Length - (rsobj.FolderDepth));

                if (!path.EndsWith("/"))
                {
                    LSEntryObject lsentry = offsetParts.LastOrDefault(x => x != null);
                    if (lsentry == null)
                        continue;

                    var fileData = new byte[0];
                    if (rsobj.CmpSize > 0)
                        fileData = GetFileDataDecompressed(lsentry.DTOffset + rsobj.OffInPack, rsobj.CmpSize,
                            lsentry.DTIndex);

                    if (fileData.Length != rsobj.DecSize)
                    {
                        Console.WriteLine("Error: File length doesn't match specified decompressed length, skipping");
                        Logstream.WriteLine(
                            "Error: File length doesn't match specified decompressed length, skipping");
                        continue;
                    }

                    var folder = Path.GetDirectoryName(path);
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);
                    File.WriteAllBytes(path, fileData);
                }
            }

            // clean up
            rfFile._workingSource.Close();
            if (File.Exists($"resource{region}.dec"))
                File.Delete($"resource{region}.dec");
            if (File.Exists($"resource{region}"))
                File.Delete($"resource{region}");
        }
        private static void Unpack_All()
        {
            Unpack_Resource("resource");
            foreach (string reg in regions[regionCode])
                Unpack_Resource($"resource({reg})");
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
        private static unsafe void PatchArchive(string resourceStr, string patchFolder)
        {
            LSEntryObject _resource = lsFile.Entries[calc_crc(resourceStr)];
            byte[] resource = GetFileDataDecompressed(_resource.DTOffset + (uint)_resource.PaddingLength,
                _resource.Size,
                _resource.DTIndex);
            File.WriteAllBytes(resourceStr, resource);

            Console.WriteLine($"Patching {resourceStr}");
            RFFile rfFile = new RFFile(resourceStr);
            regionCode = rfFile.Header.RegionEtc & 3;

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

                        uint difference = 0;
                        DataSource src = GetFileChunk(lsentry.DTOffset, lsentry.Size, lsentry.DTIndex, out difference);

                        VoidPtr addr = src.Address + difference;
                        addr += rsobj.OffInPack;
                        // write over old data.
                        for (int i = 0; i < rsobj.CmpSize; i++)
                            *(byte*)(addr + i) = 0xCC;

                        // Get usable space
                        int dataLen = 0;
                        while (*(byte*)(addr + dataLen) == 0xCC)
                            dataLen++;

                        if (compressed.Length > dataLen)
                        {
                            Console.WriteLine("Patching files larger than original not yet supported, skipping");
                            continue;
                        }
                        rsobj.CmpSize = compressed.Length;
                        rsobj.DecSize = raw.Length;

                        for (int i = 0; i < compressed.Length; i++)
                            *(byte*)(addr + i) = compressed[i];

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
            lsFile.Entries[calc_crc(resourceStr)].Size = full.Length;
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

            if (File.Exists(resourceStr))
                File.Delete(resourceStr);
            if (File.Exists(resourceStr + ".dec"))
                File.Delete(resourceStr + ".dec");
        }

        // Rebuilding attempts. Almost works, but needs to handle embedded LS entries.
        private static unsafe void RebuildArchive(string patchFolder)
        {
            LSEntryObject _resMain = lsFile.Entries[calc_crc("resource")];

            byte[] resource = GetFileDataDecompressed(_resMain.DTOffset + (uint)_resMain.PaddingLength,
                _resMain.Size,
                _resMain.DTIndex);
            File.WriteAllBytes("resource", resource);

            Console.WriteLine($"Rebuilding archive...");
            RFFile rfFile = new RFFile("resource");

            var pathParts = new string[20];
            var offsetParts = new LSEntryObject[20];
            using (FileStream strm = File.Create("dt_rebuild"))
            {
                int lsSize = 0;
                foreach (ResourceEntryObject rsobj in rfFile.ResourceEntries.Where(rsobj => rsobj != null))
                {
                    pathParts[rsobj.FolderDepth - 1] = rsobj.EntryString;
                    Array.Clear(pathParts, rsobj.FolderDepth, pathParts.Length - (rsobj.FolderDepth + 1));
                    var path = string.Join("", pathParts);

                    LSEntryObject fileEntry;
                    if (rsobj.HasPack)
                    {
                        // if this is the end of a pack, update info and write end padding
                        if (offsetParts.Where(x => x != null).ToArray().Length > 0)
                        {
                            LSEntryObject lsentry = offsetParts.Last(x => x != null);
                            int align = lsSize.RoundUp(0x10) - lsSize;

                            if (lsentry.DTOffset != strm.Position + 0x80)
                                for (int i = 0; i < 0x60 + align; i++)
                                    strm.WriteByte(0xbb);

                            lsentry.Size = lsSize;
                        }
                        // grab a new pack using the filepath we've built then write start padding.
                        lsSize = 0;
                        var crcPath = $"data/{path.TrimEnd('/') + (rsobj.Compressed ? "/packed" : "")}";
                        var crc = calc_crc(crcPath);
                        lsFile.Entries.TryGetValue(crc, out fileEntry);
                        fileEntry.DTOffset = (uint)strm.Position;
                        for (int i = 0; i < 0x80; i++, lsSize++)
                            strm.WriteByte(0xcc);
                    }
                    else
                        fileEntry = null;

                    offsetParts[rsobj.FolderDepth - 1] = fileEntry;
                    Array.Clear(offsetParts, rsobj.FolderDepth, offsetParts.Length - (rsobj.FolderDepth));

                    if (!path.EndsWith("/"))
                        if (File.Exists($"{patchFolder}/{path}"))
                        {
                            LSEntryObject lsentry = offsetParts.Last(x => x != null);

                            Console.WriteLine($"{patchFolder}/{path}");
                            Logstream.WriteLine($"{patchFolder}/{path}");

                            byte[] raw = File.ReadAllBytes($"{patchFolder}/{path}");
                            byte[] compressed = Util.Compress(raw);
                            int align = compressed.Length.RoundUp(0x10) - compressed.Length;
                            long off = strm.Position - lsentry.DTOffset;
                            strm.Write(compressed, 0, compressed.Length);
                            // write file borders
                            for (int i = 0; i < 0x20 + align; i++)
                                strm.WriteByte(0xcc);
                            lsSize += compressed.Length + 0x20 + align;

                            rsobj.CmpSize = compressed.Length;
                            rsobj.DecSize = raw.Length;
                            rsobj.OffInPack = (uint)off;
                        }
                }
                var entry = offsetParts.Last(x => x != null);
                entry.DTOffset = (uint)strm.Position - (uint)lsSize;
                entry.Size = lsSize;

                // Update resource and LS files.
                rfFile.UpdateEntries();
                byte[] dec = rfFile._workingSource.Slice((int)rfFile.Header.HeaderLen1,
                    (int)(rfFile._workingSource.Length - rfFile.Header.HeaderLen1));
                byte[] cmp = Util.Compress(dec);
                rfFile.Header.CompressedLen = (uint)cmp.Length;
                rfFile.Header.DecompressedLen = (uint)dec.Length;
                byte[] header = rfFile.Header.ToArray();
                byte[] full = header.Concat(cmp).ToArray();
                // Patch the resource data back into the DT file.
                long rOff = strm.Position;
                strm.Write(full, 0, full.Length);
                rfFile._workingSource.Close();
                lsFile.Entries[calc_crc("resource")].Size = full.Length;
                lsFile.Entries[calc_crc("resource")].DTOffset = (uint)rOff;
                lsFile.Entries[calc_crc("resource(us_en)")].Size = full.Length;
                lsFile.Entries[calc_crc("resource(us_en)")].DTOffset = (uint)rOff;
                lsFile.Entries[calc_crc("resource(us_fr)")].Size = full.Length;
                lsFile.Entries[calc_crc("resource(us_fr)")].DTOffset = (uint)rOff;
                lsFile.Entries[calc_crc("resource(us_sp)")].Size = full.Length;
                lsFile.Entries[calc_crc("resource(us_sp)")].DTOffset = (uint)rOff;
                lsFile.UpdateEntries();
            }

            if (File.Exists("resource"))
                File.Delete("resource");
            if (File.Exists("resource.dec"))
                File.Delete("resource.dec");
        }

        /// <summary>
        /// Returns file data from the dt file with an index of "dtIndex"
        /// </summary>
        /// <param name="start">Start offset of file data</param>
        /// <param name="size">Size of the data in bytes</param>
        /// <param name="dtIndex">Index of the dt file to access</param>
        /// <returns></returns>
        private static byte[] GetFileDataDecompressed(uint start, int size, int dtIndex)
        {
            uint diff = 0;
            DataSource src = GetFileChunk(start, size, dtIndex, out diff);

            byte[] b = src.Slice((int)diff, size);

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
        /// <param name="start"> Start of chunk.</param>
        /// <param name="size"> Size of the chunk in bytes</param>
        /// <param name="dtIndex">Index of the dt file to access</param>
        /// <returns></returns>
        private static DataSource GetFileChunk(uint start, int size, int dtIndex)
        {
            SYSTEM_INFO _info = new SYSTEM_INFO();
            GetSystemInfo(ref _info);

            uint chunk_start = start;
            int chunk_len = size;
            if (start % _info.allocationGranularity != 0)
            {
                chunk_start = start.RoundDown((int)_info.allocationGranularity);
                var difference = start - chunk_start;
                chunk_len = (int)difference + size;
            }
            return new DataSource(FileMap.FromFile(DtPaths[dtIndex], FileMapProtect.ReadWrite, chunk_start, chunk_len));
        }
        private static DataSource GetFileChunk(uint start, int size, int dtIndex, out uint difference)
        {
            SYSTEM_INFO _info = new SYSTEM_INFO();
            GetSystemInfo(ref _info);

            uint chunk_start = start;
            int chunk_len = size;
            difference = 0;
            if (start % _info.allocationGranularity != 0)
            {
                chunk_start = start.RoundDown((int)_info.allocationGranularity);
                difference = start - chunk_start;
                chunk_len = (int)difference + size;
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

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern void GetSystemInfo(ref SYSTEM_INFO lpSystemInfo);
        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_INFO
        {
            public ushort processorArchitecture;
            ushort reserved;
            public uint pageSize;
            public IntPtr minimumApplicationAddress;
            public IntPtr maximumApplicationAddress;
            public IntPtr activeProcessorMask;
            public uint numberOfProcessors;
            public uint processorType;
            public uint allocationGranularity;
            public ushort processorLevel;
            public ushort processorRevision;
        }
    }
}
