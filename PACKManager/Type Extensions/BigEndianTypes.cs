using System;
using System.Runtime.InteropServices;

namespace System
{
    [StructLayout(LayoutKind.Sequential)]
    public struct bint : IConvertible
    {
        public int _data;
        public static implicit operator int(bint val) { return val._data.Reverse(); }
        public static implicit operator bint(int val) { return new bint { _data = val.Reverse() }; }
        public static explicit operator uint(bint val) { return (uint)val._data.Reverse(); }
        public static explicit operator bint(uint val) { return new bint { _data = (int)val.Reverse() }; }

        #region IConvertible Members

        public TypeCode GetTypeCode()
        {
            return Convert.GetTypeCode(_data.Reverse());
        }

        public bool ToBoolean(IFormatProvider provider)
        {
            return Convert.ToBoolean(_data.Reverse());
        }

        public byte ToByte(IFormatProvider provider)
        {
            return Convert.ToByte(_data.Reverse());
        }

        public char ToChar(IFormatProvider provider)
        {
            return Convert.ToChar(_data.Reverse());
        }

        public DateTime ToDateTime(IFormatProvider provider)
        {
            return Convert.ToDateTime(_data.Reverse());
        }

        public decimal ToDecimal(IFormatProvider provider)
        {
            return Convert.ToDecimal(_data.Reverse());
        }

        public double ToDouble(IFormatProvider provider)
        {
            return Convert.ToDouble(_data.Reverse());
        }

        public short ToInt16(IFormatProvider provider)
        {
            return Convert.ToInt16(_data.Reverse());
        }

        public int ToInt32(IFormatProvider provider)
        {
            return Convert.ToInt32(_data.Reverse());
        }

        public long ToInt64(IFormatProvider provider)
        {
            return Convert.ToInt64(_data.Reverse());
        }

        public sbyte ToSByte(IFormatProvider provider)
        {
            return Convert.ToSByte(_data.Reverse());
        }

        public float ToSingle(IFormatProvider provider)
        {
            return Convert.ToSingle(_data.Reverse());
        }

        public string ToString(IFormatProvider provider)
        {
            return Convert.ToString(_data.Reverse());
        }

        public object ToType(Type conversionType, IFormatProvider provider)
        {
            return Convert.ChangeType(_data.Reverse(), conversionType, provider);
        }

        public ushort ToUInt16(IFormatProvider provider)
        {
            return Convert.ToUInt16(_data.Reverse());
        }

        public uint ToUInt32(IFormatProvider provider)
        {
            return Convert.ToUInt32(_data.Reverse());
        }

        public ulong ToUInt64(IFormatProvider provider)
        {
            return Convert.ToUInt64(_data.Reverse());
        }

        #endregion
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct buint : IConvertible
    {
        public uint _data;
        public static implicit operator uint(buint val) { return val._data.Reverse(); }
        public static implicit operator buint(uint val) { return new buint { _data = val.Reverse() }; }
        public static explicit operator int(buint val) { return (int)val._data.Reverse(); }
        public static explicit operator buint(int val) { return new buint { _data = (uint)val.Reverse() }; }

        #region IConvertible Members

        public TypeCode GetTypeCode()
        {
            return Convert.GetTypeCode(_data.Reverse());
        }

        public bool ToBoolean(IFormatProvider provider)
        {
            return Convert.ToBoolean(_data.Reverse());
        }

        public byte ToByte(IFormatProvider provider)
        {
            return Convert.ToByte(_data.Reverse());
        }

        public char ToChar(IFormatProvider provider)
        {
            return Convert.ToChar(_data.Reverse());
        }

        public DateTime ToDateTime(IFormatProvider provider)
        {
            return Convert.ToDateTime(_data.Reverse());
        }

        public decimal ToDecimal(IFormatProvider provider)
        {
            return Convert.ToDecimal(_data.Reverse());
        }

        public double ToDouble(IFormatProvider provider)
        {
            return Convert.ToDouble(_data.Reverse());
        }

        public short ToInt16(IFormatProvider provider)
        {
            return Convert.ToInt16(_data.Reverse());
        }

        public int ToInt32(IFormatProvider provider)
        {
            return Convert.ToInt32(_data.Reverse());
        }

        public long ToInt64(IFormatProvider provider)
        {
            return Convert.ToInt64(_data.Reverse());
        }

        public sbyte ToSByte(IFormatProvider provider)
        {
            return Convert.ToSByte(_data.Reverse());
        }

        public float ToSingle(IFormatProvider provider)
        {
            return Convert.ToSingle(_data.Reverse());
        }

        public string ToString(IFormatProvider provider)
        {
            return Convert.ToString(_data.Reverse());
        }

        public object ToType(Type conversionType, IFormatProvider provider)
        {
            return Convert.ChangeType(_data.Reverse(), conversionType, provider);
        }

        public ushort ToUInt16(IFormatProvider provider)
        {
            return Convert.ToUInt16(_data.Reverse());
        }

        public uint ToUInt32(IFormatProvider provider)
        {
            return Convert.ToUInt32(_data.Reverse());
        }

        public ulong ToUInt64(IFormatProvider provider)
        {
            return Convert.ToUInt64(_data.Reverse());
        }

        #endregion
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct bfloat : IConvertible
    {
        public float _data;
        public static implicit operator float(bfloat val) { return val._data.Reverse(); }
        public static implicit operator bfloat(float val) { return new bfloat { _data = val.Reverse() }; }

        #region IConvertible Members

        public TypeCode GetTypeCode()
        {
            return Convert.GetTypeCode(_data.Reverse());
        }

        public bool ToBoolean(IFormatProvider provider)
        {
            return Convert.ToBoolean(_data.Reverse());
        }

        public byte ToByte(IFormatProvider provider)
        {
            return Convert.ToByte(_data.Reverse());
        }

        public char ToChar(IFormatProvider provider)
        {
            return Convert.ToChar(_data.Reverse());
        }

        public DateTime ToDateTime(IFormatProvider provider)
        {
            return Convert.ToDateTime(_data.Reverse());
        }

        public decimal ToDecimal(IFormatProvider provider)
        {
            return Convert.ToDecimal(_data.Reverse());
        }

        public double ToDouble(IFormatProvider provider)
        {
            return Convert.ToDouble(_data.Reverse());
        }

        public short ToInt16(IFormatProvider provider)
        {
            return Convert.ToInt16(_data.Reverse());
        }

        public int ToInt32(IFormatProvider provider)
        {
            return Convert.ToInt32(_data.Reverse());
        }

        public long ToInt64(IFormatProvider provider)
        {
            return Convert.ToInt64(_data.Reverse());
        }

        public sbyte ToSByte(IFormatProvider provider)
        {
            return Convert.ToSByte(_data.Reverse());
        }

        public float ToSingle(IFormatProvider provider)
        {
            return Convert.ToSingle(_data.Reverse());
        }

        public string ToString(IFormatProvider provider)
        {
            return Convert.ToString(_data.Reverse());
        }

        public object ToType(Type conversionType, IFormatProvider provider)
        {
            return Convert.ChangeType(_data.Reverse(), conversionType, provider);
        }

        public ushort ToUInt16(IFormatProvider provider)
        {
            return Convert.ToUInt16(_data.Reverse());
        }

        public uint ToUInt32(IFormatProvider provider)
        {
            return Convert.ToUInt32(_data.Reverse());
        }

        public ulong ToUInt64(IFormatProvider provider)
        {
            return Convert.ToUInt64(_data.Reverse());
        }

        #endregion
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct bshort : IConvertible
    {
        public short _data;
        public static implicit operator short(bshort val) { return val._data.Reverse(); }
        public static implicit operator bshort(short val) { return new bshort { _data = val.Reverse() }; }
        public static explicit operator ushort(bshort val) { return (ushort)val._data.Reverse(); }
        public static explicit operator bshort(ushort val) { return new bshort { _data = (short)val.Reverse() }; }

        #region IConvertible Members

        public TypeCode GetTypeCode()
        {
            return Convert.GetTypeCode(_data.Reverse());
        }

        public bool ToBoolean(IFormatProvider provider)
        {
            return Convert.ToBoolean(_data.Reverse());
        }

        public byte ToByte(IFormatProvider provider)
        {
            return Convert.ToByte(_data.Reverse());
        }

        public char ToChar(IFormatProvider provider)
        {
            return Convert.ToChar(_data.Reverse());
        }

        public DateTime ToDateTime(IFormatProvider provider)
        {
            return Convert.ToDateTime(_data.Reverse());
        }

        public decimal ToDecimal(IFormatProvider provider)
        {
            return Convert.ToDecimal(_data.Reverse());
        }

        public double ToDouble(IFormatProvider provider)
        {
            return Convert.ToDouble(_data.Reverse());
        }

        public short ToInt16(IFormatProvider provider)
        {
            return Convert.ToInt16(_data.Reverse());
        }

        public int ToInt32(IFormatProvider provider)
        {
            return Convert.ToInt32(_data.Reverse());
        }

        public long ToInt64(IFormatProvider provider)
        {
            return Convert.ToInt64(_data.Reverse());
        }

        public sbyte ToSByte(IFormatProvider provider)
        {
            return Convert.ToSByte(_data.Reverse());
        }

        public float ToSingle(IFormatProvider provider)
        {
            return Convert.ToSingle(_data.Reverse());
        }

        public string ToString(IFormatProvider provider)
        {
            return Convert.ToString(_data.Reverse());
        }

        public object ToType(Type conversionType, IFormatProvider provider)
        {
            return Convert.ChangeType(_data.Reverse(), conversionType, provider);
        }

        public ushort ToUInt16(IFormatProvider provider)
        {
            return Convert.ToUInt16(_data.Reverse());
        }

        public uint ToUInt32(IFormatProvider provider)
        {
            return Convert.ToUInt32(_data.Reverse());
        }

        public ulong ToUInt64(IFormatProvider provider)
        {
            return Convert.ToUInt64(_data.Reverse());
        }

        #endregion
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct bushort : IConvertible
    {
        public ushort _data;
        public static implicit operator ushort(bushort val) { return val._data.Reverse(); }
        public static implicit operator bushort(ushort val) { return new bushort { _data = val.Reverse() }; }
        public static explicit operator short(bushort val) { return (short)val._data.Reverse(); }
        public static explicit operator bushort(short val) { return new bushort { _data = (ushort)val.Reverse() }; }

        #region IConvertible Members

        public TypeCode GetTypeCode()
        {
            return Convert.GetTypeCode(_data.Reverse());
        }

        public bool ToBoolean(IFormatProvider provider)
        {
            return Convert.ToBoolean(_data.Reverse());
        }

        public byte ToByte(IFormatProvider provider)
        {
            return Convert.ToByte(_data.Reverse());
        }

        public char ToChar(IFormatProvider provider)
        {
            return Convert.ToChar(_data.Reverse());
        }

        public DateTime ToDateTime(IFormatProvider provider)
        {
            return Convert.ToDateTime(_data.Reverse());
        }

        public decimal ToDecimal(IFormatProvider provider)
        {
            return Convert.ToDecimal(_data.Reverse());
        }

        public double ToDouble(IFormatProvider provider)
        {
            return Convert.ToDouble(_data.Reverse());
        }

        public short ToInt16(IFormatProvider provider)
        {
            return Convert.ToInt16(_data.Reverse());
        }

        public int ToInt32(IFormatProvider provider)
        {
            return Convert.ToInt32(_data.Reverse());
        }

        public long ToInt64(IFormatProvider provider)
        {
            return Convert.ToInt64(_data.Reverse());
        }

        public sbyte ToSByte(IFormatProvider provider)
        {
            return Convert.ToSByte(_data.Reverse());
        }

        public float ToSingle(IFormatProvider provider)
        {
            return Convert.ToSingle(_data.Reverse());
        }

        public string ToString(IFormatProvider provider)
        {
            return Convert.ToString(_data.Reverse());
        }

        public object ToType(Type conversionType, IFormatProvider provider)
        {
            return Convert.ChangeType(_data.Reverse(), conversionType, provider);
        }

        public ushort ToUInt16(IFormatProvider provider)
        {
            return Convert.ToUInt16(_data.Reverse());
        }

        public uint ToUInt32(IFormatProvider provider)
        {
            return Convert.ToUInt32(_data.Reverse());
        }

        public ulong ToUInt64(IFormatProvider provider)
        {
            return Convert.ToUInt64(_data.Reverse());
        }

        #endregion
    }
}
