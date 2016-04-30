using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SALT.PARAMS
{
    public class ParamEntry
    {
        public ParamEntry(object value, ParamType type)
        {
            Value = value;
            Type = type;
        }

        public ParamType Type { get; set; }
        public object Value { get; set; }
        public int Size
        {
            get
            {
                switch (Type)
                {
                    case ParamType.u8:
                    case ParamType.s8:
                        return 2;
                    case ParamType.u16:
                    case ParamType.s16:
                        return 3;
                    case ParamType.u32:
                    case ParamType.s32:
                    case ParamType.f32:
                        return 5;
                    case ParamType.str:
                        return ((string)Value).Length + 1;
                    default:
                        return 0;
                }
            }
        }

        public byte[] GetBytes()
        {
            List<byte> data = new List<byte>();
            switch (Type)
            {
                case ParamType.u8:
                    data.Add(1);
                    data.Add((byte)Value);
                    return data.ToArray();
                case ParamType.s8:
                    data.Add(2);
                    data.Add((byte)Value);
                    return data.ToArray();
                case ParamType.u16:
                    data.Add(3);
                    data.AddRange(BitConverter.GetBytes((ushort)Value).Reverse());
                    return data.ToArray();
                case ParamType.s16:
                    data.Add(4);
                    data.AddRange(BitConverter.GetBytes((short)Value).Reverse());
                    return data.ToArray();
                case ParamType.u32:
                    data.Add(5);
                    data.AddRange(BitConverter.GetBytes((uint)Value).Reverse());
                    return data.ToArray();
                case ParamType.s32:
                    data.Add(6);
                    data.AddRange(BitConverter.GetBytes((int)Value).Reverse());
                    return data.ToArray();
                case ParamType.f32:
                    data.Add(7);
                    data.AddRange(BitConverter.GetBytes((float)Value).Reverse());
                    return data.ToArray();
                case ParamType.str:
                    data.Add(8);
                    data.AddRange(BitConverter.GetBytes(((string)Value).Length).Reverse());
                    data.AddRange(Encoding.ASCII.GetBytes((string)Value));
                    return data.ToArray();
                case ParamType.group:
                    data.Add(0x20);
                    data.AddRange(((ParamGroup)Value).GetBytes());
                    return data.ToArray();
                default:
                    return null;
            }
        }
    }
}
