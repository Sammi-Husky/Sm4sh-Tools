using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.IO
{
    public static class StreamExtensions
    {
        public static Int32 ReadBint32(this BinaryReader reader)
        {
            return reader.ReadInt32().Reverse();
        }
        public static Int16 ReadBint16(this BinaryReader reader)
        {
            return reader.ReadInt16().Reverse();
        }
        public static UInt16 ReadBuint16(this BinaryReader reader)
        {
            return reader.ReadUInt16().Reverse();
        }
        public static UInt32 ReadBuint32(this BinaryReader reader)
        {
            return reader.ReadUInt32().Reverse();
        }
    }
}
