using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sm4shCommand
{
    public static class Util
    {

        //  Retrieve a word from the specified array of bytes.
        public static long GetWord(byte[] data, long offset)
        {
            if (offset % 4 != 0) throw new Exception("Odd word offset");
            if (offset >= data.Length) throw new Exception("Offset out of range.");

            return (uint)(data[offset + 0] * 0x1000000)
                 + (uint)(data[offset + 1] * 0x10000)
                 + (uint)(data[offset + 2] * 0x100)
                 + (uint)(data[offset + 3] * 0x1);
        }

        //  Set a word into an array of bytes. Resize the array if needed.
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
        public static long FloatToHex(float val)
        {
            if (val == 0) return 0;
            long sign = (val >= 0 ? 0 : 8);
            long exponent = 0x7F;
            float mantissa = Math.Abs(val);

            if (mantissa > 1)
                while (mantissa > 2)
                { mantissa /= 2; exponent++; }
            else
                while (mantissa < 1)
                { mantissa *= 2; exponent--; }
            mantissa -= 1;
            mantissa *= (float)Math.Pow(2, 23);

            return (
                  sign * 0x10000000
                + exponent * 0x800000
                + (long)mantissa);
        }
    }
    public class FormProvider
    {
        public static MainForm MainWindow
        {
            get
            {
                if (_mainForm == null)
                {
                    _mainForm= new MainForm();
                }
                return _mainForm;
            }
        }
        private static MainForm _mainForm;
    }
}
