using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Text
{
    public unsafe static class EncodingExtension
    {
        public static string GetString(this Encoding encoding, sbyte* ptr)
        {
            int count = 0;
            while (*(ptr + count++) != '\0') ;

            return new string((sbyte*)ptr, 0, count - 1, encoding);
        }

        public static string GetString(this Encoding encoding, VoidPtr ptr)
        {
            return encoding.GetString((sbyte*)ptr);
        }
    }
}
