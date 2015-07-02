using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sm4shCommand
{
    public unsafe static class Util
    {
        /// <summary>
        /// Retrieves a word from an array of bytes.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="endian"></param>
        /// <returns></returns>
        public static long GetWord(byte[] data, long offset, Endianness endian)
        {
            if (offset % 4 != 0) throw new Exception("Odd word offset.");
            if (offset >= data.Length) throw new Exception("Offset outside of expected value range.");

            if (endian == Endianness.Little)
            {
                return (uint)(data[offset + 3] * 0x1000000)
                     + (uint)(data[offset + 2] * 0x10000)
                     + (uint)(data[offset + 1] * 0x100)
                     + (uint)(data[offset + 0] * 0x1);
            }
            else
            {
                return (uint)(data[offset + 0] * 0x1000000)
                     + (uint)(data[offset + 1] * 0x10000)
                     + (uint)(data[offset + 2] * 0x100)
                     + (uint)(data[offset + 3] * 0x1);
            }
        }
        /// <summary>
        /// Retrieves an 32 bit integer from the specified address.
        /// </summary>
        /// <param name="Address"></param>
        /// <param name="endian"></param>
        /// <returns></returns>
        public static int GetWordUnsafe(VoidPtr Address, Endianness endian)
        {
            if (Address % 4 != 0)
                return 0;

            if (endian == Endianness.Big)
                return *(bint*)Address;
            else
                return *(int*)Address;
        }

        /// <summary>
        /// Sets a value into an array of bytes, resizing if necessary.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="value"></param>
        /// <param name="offset"></param>
        /// <param name="endian"></param>
        public static void SetWord(ref byte[] data, long value, long offset, Endianness endian)
        {
            if (offset % 4 != 0) throw new Exception("Odd word offset");
            if (offset >= data.Length)
            {
                Array.Resize<byte>(ref data, (int)offset + 4);
            }
            if (endian == Endianness.Little)
            {

                data[offset + 3] = (byte)((value & 0xFF000000) / 0x1000000);
                data[offset + 2] = (byte)((value & 0xFF0000) / 0x10000);
                data[offset + 1] = (byte)((value & 0xFF00) / 0x100);
                data[offset + 0] = (byte)((value & 0xFF) / 0x1);
            }
            else if (endian == Endianness.Big)
            {
                data[offset + 0] = (byte)((value & 0xFF000000) / 0x1000000);
                data[offset + 1] = (byte)((value & 0xFF0000) / 0x10000);
                data[offset + 2] = (byte)((value & 0xFF00) / 0x100);
                data[offset + 3] = (byte)((value & 0xFF) / 0x1);
            }
        }
        /// <summary>
        /// Sets a value into memory at the specified address.
        /// </summary>
        /// <param name="Address"></param>
        /// <param name="value"></param>
        /// <param name="endian"></param>
        public static void SetWordUnsafe(VoidPtr Address, int value, Endianness endian)
        {
            if (Address % 4 != 0)
                return;

            if (endian == Endianness.Big)
                *(bint*)Address = (bint)value;
            else
                *(int*)Address = value;
        }
        /// <summary>
        /// Sets a floating point value into an array of bytes, resizing if necessary.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="value"></param>
        /// <param name="offset"></param>
        /// <param name="endian"></param>
        public static void SetFloat(ref byte[] data, float value, long offset, Endianness endian)
        {
            SetWord(ref data, FloatToHex(value), offset, endian);
        }
        /// <summary>
        /// Sets a floating point value into memory at the specified address.
        /// </summary>
        /// <param name="Address"></param>
        /// <param name="value"></param>
        /// <param name="endian"></param>
        public static void SetFloatUnsafe(VoidPtr Address, float value, Endianness endian)
        {
            if (Address % 4 != 0)
                return;

            if (endian == Endianness.Big)
                *(bfloat*)Address = value;
            else
                *(float*)Address = value;
        }
        /// <summary>
        /// Gets a floating point value from a specified adress.
        /// </summary>
        /// <param name="Address"></param>
        /// <param name="endian"></param>
        /// <returns></returns>
        public static float GetFloatUnsafe(VoidPtr Address, Endianness endian)
        {
            if (Address % 4 != 0)
                return 0;

            if (endian == Endianness.Big)
                return *(bfloat*)Address;
            else
                return *(float*)Address;
        }

        /// <summary>
        /// Returns the hexadecimal representation of the passed in float value.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Returns the floating point value of the passed in hexadecimal value.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static float HexToFloat(long val)
        {
            if (val == 0) return 0;
            float sign = ((val & 0x80000000) == 0 ? 1 : -1);
            int exponent = ((int)(val & 0x7F800000) / 0x800000) - 0x7F;
            float mantissa = (val & 0x7FFFFF);
            long mantissaBits = 23;

            if (mantissa != 0)
                while (((long)mantissa & 0x1) != 1)
                { mantissa /= 2; mantissaBits--; }
            mantissa /= (float)Math.Pow(2, mantissaBits);
            mantissa += 1;

            mantissa *= (float)Math.Pow(2, exponent);
            return mantissa *= sign;
        }
    }
    public class FormProvider
    {
        public static ACMDMain MainWindow
        {
            get
            {
                if (_mainForm == null)
                {
                    _mainForm= new ACMDMain();
                }
                return _mainForm;
            }
        }
        private static ACMDMain _mainForm;
    }
}
