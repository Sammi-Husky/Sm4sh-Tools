using System;

namespace System
{
    public static class StringExtension
    {
        public static string TruncateAndFill(this string s, int length, char fillChar)
        {
            int min = length;
            min = Math.Max(min, 0); 
            min = Math.Min(min, s.Length);

            return s.Substring(0, min).PadRight(length, fillChar);
        }

        public static string TruncateAndTerminate(this string s, int length)
        {
            int min = length - 1;
            min = Math.Max(min, 0);
            min = Math.Min(min, s.Length);

            return s.Substring(0, min).PadRight(length, '\0');
        }

        public static string Terminate(this string s, params char[] terminators)
        {
            int index = s.IndexOfAny(terminators);

            if (index == -1)
                return s;

            return s.Substring(0, index);
        }

        public static unsafe int IndexOfOccurance(this string s, char c, int index)
        {
            int len = s.Length;
            fixed (char* cPtr = s)
            {
                for (int i = 0, count = 0; i < len; i++)
                    if ((cPtr[i] == c) && (count++ == index))
                        return i;
            }
            return -1;
        }
    }
}
