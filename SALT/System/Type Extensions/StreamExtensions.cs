// Copyright (c) Sammi Husky. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace System.IO
{
    public static class StreamExtensions
    {
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
    }
}
