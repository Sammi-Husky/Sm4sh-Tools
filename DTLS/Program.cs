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
        public static DataSource DtSource;
        public static DataSource LsSource;

        public static int LsCount;

        public static Dictionary<uint, uint[]> DtOffsets = new Dictionary<uint, uint[]>();
        public static DataSource ResourceData;
        public static DataSource ResourceDec;

        public static byte[][] StrSegments;
        public static string[] Extensions;
        public static StreamWriter Logstream = new StreamWriter("log.txt");

        private static void Main(string[] args)
        {
            if (args.Length == 3)
                try
                {
                    Unpack(args[0], args[1], args[2]);
                }
                catch (Exception x)
                {
                    Console.WriteLine(x.Message);
                    Logstream.WriteLine(x.Message);
                }
            else
                Console.WriteLine("Usage: <dt file> <ls file> <output path>");
        }

        private static unsafe void Unpack(string dt, string ls, string output)
        {
            DtSource = new DataSource(FileMap.FromFile(dt));
            LsSource = new DataSource(FileMap.FromFile(ls));

            LsCount = *(int*)(LsSource.Address + 4);

            for (var i = 0; i < LsCount; i++)
            {
                LSEntry entry = *(LSEntry*)(LsSource.Address + 8 + 0x0C * i);
                DtOffsets.Add(entry._crc, new[] { entry._start, entry._size });
            }

            var resource = DtOffsets[calc_crc("resource")];
            ResourceData = new DataSource(DtSource.Address + resource[0], (int)resource[1]);
            RFHeader RFHeader = *(RFHeader*)ResourceData.Address;

            var z = 0;
            while (*(byte*)(ResourceData.Address + z) != 0x78)
                z += 2;

            File.WriteAllBytes("resource.dec", DeCompress(ResourceData.Slice(z, ResourceData.Length - z)));
            ResourceDec = new DataSource(FileMap.FromFile("resource.dec"));

            var segCount = *(int*)(ResourceDec.Address + RFHeader._strsPlus - RFHeader._headerLen1);
            StrSegments = new byte[segCount][];
            for (int i = 0, pos = 0; i < segCount; i++, pos += 0x2000)
                StrSegments[i]= ResourceDec.Slice((int)(RFHeader._strsPlus - RFHeader._headerLen1 + 4) + pos, 0x2000);

            uint offsetsPos = (uint)(RFHeader._strsPlus - RFHeader._headerLen1 + 4 + segCount * 0x2000);

            var numOffsets = *(int*)(ResourceDec.Address + offsetsPos);
            var extOffs = new uint[numOffsets];
            for (var i = 0; i < numOffsets; i++)
                extOffs[i] = *(uint*)(ResourceDec.Address + offsetsPos + 4 + i * 4);

            Extensions = new string[numOffsets];
            for (int i = 0; i < Extensions.Length; i++)
            {
                var ext = Encoding.ASCII.GetString(str_from_offset((int)extOffs[i], 64));
                Extensions[i] = ext.Remove(ext.IndexOf('\0'));
            }

            uint position = 0;
            var num8Sized = *(uint*)ResourceDec.Address;
            position += num8Sized * 8 + 4;
            position += *(uint*)(ResourceDec.Address + position) + 4;

            var pathParts = new string[20];
            var offsetParts = new uint[20][];
            while (position < RFHeader._0x18EntriesLen)
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

                var nestingLevel = entry.nesting & 0xff;
                var localized = (entry.nesting & 0x800) > 0;
                var final = (entry.nesting & 0x400) > 0;
                var compressed = (entry.nesting & 0x200) > 0;

                pathParts[(int)nestingLevel - 1] = name;
                Array.Clear(pathParts, (int)nestingLevel, pathParts.Length - ((int)nestingLevel + 1));
                var path = string.Join("", pathParts);

                uint[] offset;
                if (final)
                {
                    var crcPath = "data/" + path.TrimEnd('/') + (compressed ? "/packed" : "");
                    var crc = calc_crc(crcPath);
                    offset = DtOffsets[crc];
                }
                else
                    offset = null;

                offsetParts[(int)nestingLevel - 1] = offset;
                Array.Clear(offsetParts, (int)nestingLevel, offsetParts.Length - ((int)nestingLevel + 1));

                var outfn = $"{output}/{path}";
                if (path.EndsWith("/"))
                {
                    if (!Directory.Exists(outfn))
                        Directory.CreateDirectory(outfn);
                }
                else
                {
                    uint chunkStart = 0, chunkLen = 0;

                    //Instead of reversing the array, we'll just traverse it backwards.
                    for (var i = 14; i > -1; i--)
                    {
                        if (offsetParts[i] == null)
                            continue;

                        if (offsetParts[i][0] == 0 || offsetParts[i][1] == 0)
                            continue;

                        chunkStart = offsetParts[i][0];
                        chunkLen = offsetParts[i][1];
                        break;
                    }


                    var cmpData = DtSource.Slice((int)(chunkStart + entry.offInChunk), entry.cmpSize);
                    var fileData = new byte[0];

                    Console.WriteLine(outfn);
                    Logstream.WriteLine($"{outfn} : size: {entry.decSize}");

                    if (cmpData.Length > 0)
                        fileData = cmpData[0] == 0x78 && cmpData[1] == 0x9c ?
                            DeCompress(cmpData) : cmpData;

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
            ResourceData.Close();
            ResourceDec.Close();
            DtSource.Close();
            LsSource.Close();
            Logstream.Close();

            // clean up
            if (File.Exists("resource.dec"))
                File.Delete("resource.dec");
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