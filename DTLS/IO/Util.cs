using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZLibNet;

namespace DTLS.IO
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
                    using (
                        var compressor = new ZLibStream(destStream, CompressionMode.Compress, CompressionLevel.Level6))
                    {
                        source.CopyTo(compressor);
                    }
                    return destStream.ToArray();
                }
            }
        }

        public static byte[] DeCompress(byte[] src) =>
            ZLibCompressor.DeCompress(src);
    }
}
