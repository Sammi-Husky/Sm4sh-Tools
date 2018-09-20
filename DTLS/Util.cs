using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZLibNet;

namespace DTLS
{
    public static class Util
    {
        public static uint calc_crc(string filename)
        {
            var b = Encoding.ASCII.GetBytes(filename);
            for (var i = 0; i < 4; i++)
                b[i] = (byte)(~filename[i] & 0xff);
            return CrcCalculator.CaclulateCRC32(b) & 0xFFFFFFFF;

        }
        public static byte[] Compress(byte[] src)
        {
            using (var source = new MemoryStream(src))
            {
                using (var destStream = new MemoryStream())
                {
                    using (var compressor = new ZLibStream(destStream, CompressionMode.Compress, CompressionLevel.Level9))
                    {
                        source.CopyTo(compressor);
                    }
                    return destStream.ToArray();
                }
            }
        }

        public static byte[] DeCompress(byte[] src) =>
            ZLibCompressor.DeCompress(src);

        public static void SetWord(ref byte[] data, long value, long offset)
        {
            if (offset % 4 != 0) throw new Exception("Odd word offset");
            if (offset >= data.Length)
            {
                Array.Resize<byte>(ref data, (int)offset + 4);
            }

            data[offset + 3] = (byte)((value & 0xFF000000) / 0x1000000);
            data[offset + 2] = (byte)((value & 0xFF0000) / 0x10000);
            data[offset + 1] = (byte)((value & 0xFF00) / 0x100);
            data[offset + 0] = (byte)((value & 0xFF) / 0x1);
        }
        public static void SetHalf(ref byte[] data, short value, long offset)
        {
            if (offset % 2 != 0) throw new Exception("Odd word offset");
            if (offset >= data.Length)
            {
                Array.Resize<byte>(ref data, (int)offset + 4);
            }

            data[offset + 1] = (byte)((value & 0xFF00) / 0x100);
            data[offset + 0] = (byte)((value & 0xFF) / 0x1);
        }
    }
}
