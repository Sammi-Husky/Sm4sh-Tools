using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace System
{
    static class StreamExtensions
    {
        public static void WriteTo(this Stream source, Stream target)
        {
            source.Seek(0, SeekOrigin.Begin);
            byte[] buffer = new byte[0x10000];
            int n;
            while ((n = source.Read(buffer, 0, buffer.Length)) != 0)
                target.Write(buffer, 0, n);
        }
    }
}
