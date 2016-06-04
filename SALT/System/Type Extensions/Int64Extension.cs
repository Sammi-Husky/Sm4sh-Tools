namespace System
{
    public static class Int64Extension
    {
        public static long Align(this long value, int align)
        {
            if (value < 0) return 0;
            if (align <= 1) return value;
            long temp = value % align;
            if (temp != 0) value += align - temp;
            return value;
        }

        public static long Clamp(this long value, long min, long max)
        {
            if (value <= min) return min;
            if (value >= max) return max;
            return value;
        }
    }
}

