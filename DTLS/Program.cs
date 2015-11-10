using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ZLibNet;
using DTLS.Types;

namespace DTLS
{
    internal class Program
    {
        public static DataSource ResourceDec;
        public static string[] dtPaths;

        public static byte[][] StrSegments;
        public static string[] Extensions;
        public static StreamWriter Logstream = new StreamWriter("log.txt");

        private static void Main(string[] args)
        {

            if (args.Length < 3)
                Console.WriteLine("Usage: <dt file> <ls file> <output path>");
            else
            {
                string lspath = args[args.Length - 2];
                string outpath = args.Last();
                dtPaths = args.Take(args.Length - 2).ToArray();

                try
                {
                    Unpack(dtPaths, lspath, outpath);
                }
                catch (Exception x)
                {
                    Console.WriteLine(x.Message);
                    Logstream.WriteLine(x.Message);
                    throw x;
                }
            }
        }

        private static unsafe void Unpack(string[] dt, string ls, string output)
        {
            LSFile lsFile = new LSFile(ls);

            LSEntryObject _resource = lsFile.Entries[calc_crc("resource")];
            RFHeader rfHeader;
            fixed (byte* ptr = GetFileDataDecompressed(
                _resource.DTOffset + (uint)_resource.PaddingLength, _resource.Size, _resource.DTIndex))
            {
                rfHeader = *(RFHeader*)ptr;
            }

            File.WriteAllBytes("resource.dec", GetFileDataDecompressed(
                _resource.DTOffset + rfHeader._headerLen1, _resource.Size, _resource.DTIndex));
            ResourceDec = new DataSource(FileMap.FromFile("resource.dec"));

            var segCount = *(int*)(ResourceDec.Address + rfHeader._strsPlus - rfHeader._headerLen1);
            StrSegments = new byte[segCount][];
            for (int i = 0, pos = 0; i < segCount; i++, pos += 0x2000)
                StrSegments[i] = ResourceDec.Slice((int)(rfHeader._strsPlus - rfHeader._headerLen1 + 4) + pos, 0x2000);

            uint offsetsPos = (uint)(rfHeader._strsPlus - rfHeader._headerLen1 + 4 + segCount * 0x2000);

            var extOffs = new uint[*(int*)(ResourceDec.Address + offsetsPos)];
            for (var i = 0; i < extOffs.Length; i++)
                extOffs[i] = *(uint*)(ResourceDec.Address + offsetsPos + 4 + i * 4);

            Extensions = new string[extOffs.Length];
            for (int i = 0; i < Extensions.Length; i++)
            {
                var ext = Encoding.ASCII.GetString(str_from_offset((int)extOffs[i], 64));
                Extensions[i] = ext.Remove(ext.IndexOf('\0'));
            }

            uint position = 0;
            position += (*(uint*)ResourceDec.Address) * 8 + 4;
            position += *(uint*)(ResourceDec.Address + position) + 4;

            var pathParts = new string[20];
            var offsetParts = new uint[20][];
            while (position < rfHeader._0x18EntriesLen)
            {
                ResourceEntry entry = *(ResourceEntry*)(ResourceDec.Address + position);
                position += 0x18;

                var ext = entry.nameOffsetEtc >> 24;
                var nameOffset = entry.nameOffsetEtc & 0xfffff;
                var strbytes = str_from_offset((int)nameOffset, 128);
                var name = Encoding.ASCII.GetString(str_from_offset((int)nameOffset, 128));

                if ((entry.nameOffsetEtc & 0x00800000) > 0)
                {
                    var reference = BitConverter.ToUInt16(strbytes, 0);

                    var referenceLen = (reference & 0x1f) + 4;
                    var refReloff = (reference & 0xe0) >> 6 << 8 | (reference >> 8);
                    name = Encoding.ASCII.GetString(str_from_offset((int)nameOffset - refReloff, referenceLen)) +
                           name.Substring(2);
                }
                if (name.Contains('\0'))
                    name = name.Substring(0, name.IndexOf('\0'));

                name += Extensions[(int)ext];

                var FolderDepth = entry.flags & 0xff;
                var localized = (entry.flags & 0x800) > 0;
                var final = (entry.flags & 0x400) > 0;
                var compressed = (entry.flags & 0x200) > 0;

                pathParts[(int)FolderDepth - 1] = name;
                Array.Clear(pathParts, (int)FolderDepth, pathParts.Length - ((int)FolderDepth + 1));
                var path = string.Join("", pathParts);

                LSEntryObject fileEntry;
                if (final)
                {
                    var crcPath = "data/" + path.TrimEnd('/') + (compressed ? "/packed" : "");
                    var crc = calc_crc(crcPath);
                    fileEntry = lsFile.Entries[crc];
                }
                else
                    fileEntry = null;

                offsetParts[(int)FolderDepth - 1] =
                     fileEntry != null ? new uint[] { fileEntry.DTOffset, fileEntry.Size, (uint)fileEntry.DTIndex } :
                     null;

                Array.Clear(offsetParts, (int)FolderDepth, offsetParts.Length - ((int)FolderDepth + 1));

                var outfn = $"{output}/{path}";
                if (path.EndsWith("/"))
                {
                    if (!Directory.Exists(outfn))
                        Directory.CreateDirectory(outfn);
                }
                else
                {
                    uint chunkStart = 0, chunkLen = 0, dtIndex = 0;

                    //Instead of reversing the array, we'll just traverse it backwards.
                    for (var i = 14; i > -1; i--)
                    {
                        if (offsetParts[i] == null)
                            continue;

                        if (offsetParts[i][1] == 0)
                            continue;

                        chunkStart = offsetParts[i][0];
                        chunkLen = offsetParts[i][1];
                        dtIndex = offsetParts[i][2];
                        break;
                    }

                    var fileData = new byte[0];

                    if (entry.cmpSize > 0)
                        fileData = GetFileDataDecompressed(chunkStart + entry.offInChunk, (uint)entry.cmpSize, (int)dtIndex);


                    Console.WriteLine(outfn);
                    Logstream.WriteLine($"{outfn} : size: {entry.decSize:X8}");

                    if (fileData.Length != entry.decSize)
                    {
                        Console.WriteLine("Error: File length doesn't match specified decompressed length, quiting");
                        Logstream.WriteLine("Error: File length doesn't match specified decompressed length, quiting");
                        Console.ReadLine();
                        return;
                    }

                    File.WriteAllBytes(outfn, fileData);
                }
            }

            // clean up
            ResourceDec.Close();
            Logstream.Close();

            if (File.Exists("resource.dec"))
                File.Delete("resource.dec");
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
                    b = DeCompress(b.Skip(z).ToArray());
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

        private static uint calc_crc(string filename)
        {
            var b = Encoding.ASCII.GetBytes(filename);
            for (var i = 0; i < 4; i++)
                b[i] = (byte)(~filename[i] & 0xff);

            return CrcCalculator.CaclulateCRC32(b) & 0xFFFFFFFF;
        }

        private static byte[] Compress(byte[] src)
        {
            using (var source = new MemoryStream(src))
            {
                using (var destStream = new MemoryStream())
                {
                    using (
                        var compressor = new ZLibStream(destStream, CompressionMode.Compress, CompressionLevel.Level6))
                    {
                        source.CopyTo(compressor);
                    }
                    return destStream.ToArray();
                }
            }
        }

        private static byte[] DeCompress(byte[] src) =>
            ZLibCompressor.DeCompress(src);

        private static byte[] str_from_offset(int start, int len)
        {
            var segOff = start & 0x1fff;
            var str = StrSegments[start / 0x2000].Skip(segOff).Take(len).ToArray();
            return str;
        }

    }
}