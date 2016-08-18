// Copyright (c) Sammi Husky. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace System.IO
{
    public static class StreamExtensions
    {
        public static int ReadInt32(this BinaryReader reader, Endianness endian)
        {
            if (endian == Endianness.Big)
                return reader.ReadBint32();
            else
                return reader.ReadInt32();
        }

        public static uint ReadUInt16(this BinaryReader reader, Endianness endian)
        {
            if (endian == Endianness.Big)
                return reader.ReadBuint16();
            else
                return reader.ReadUInt16();
        }

        public static uint ReadUInt32(this BinaryReader reader, Endianness endian)
        {
            if (endian == Endianness.Big)
                return reader.ReadBuint32();
            else
                return reader.ReadUInt32();
        }

        public static int ReadBint32(this BinaryReader reader)
        {
            return reader.ReadInt32().Reverse();
        }

        public static short ReadBint16(this BinaryReader reader)
        {
            return reader.ReadInt16().Reverse();
        }

        public static ushort ReadBuint16(this BinaryReader reader)
        {
            return reader.ReadUInt16().Reverse();
        }

        public static uint ReadBuint32(this BinaryReader reader)
        {
            return reader.ReadUInt32().Reverse();
        }
        public static string ReadStringNT(this BinaryReader reader)
        {
            string str = "";
            char ch;
            while ((int)(ch = reader.ReadChar()) != 0)
                str = str + ch;
            return str;
        }
        public static float ReadBfloat(this BinaryReader reader)
        {
            return reader.ReadSingle().Reverse();
        }
    }
}
