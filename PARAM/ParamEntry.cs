using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parameters
{
    public class ParamEntry
    {
        public ParamEntry(object value, ParameterType type)
        {
            Value = value;
            Type = type;
        }
        public ParameterType Type { get; set; }
        public object Value { get; set; }
        public int Size
        {
            get
            {
                switch (Type)
                {
                    case ParameterType.u8:
                    case ParameterType.s8:
                        return 2;
                    case ParameterType.u16:
                    case ParameterType.s16:
                        return 3;
                    case ParameterType.u32:
                    case ParameterType.s32:
                    case ParameterType.f32:
                        return 5;
                    case ParameterType.str:
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
                case ParameterType.u8:
                    data.Add(1);
                    data.Add((byte)Value);
                    return data.ToArray();
                case ParameterType.s8:
                    data.Add(2);
                    data.Add((byte)Value);
                    return data.ToArray();
                case ParameterType.u16:
                    data.Add(3);
                    data.AddRange(BitConverter.GetBytes((ushort)Value).Reverse());
                    return data.ToArray();
                case ParameterType.s16:
                    data.Add(4);
                    data.AddRange(BitConverter.GetBytes((short)Value).Reverse());
                    return data.ToArray();
                case ParameterType.u32:
                    data.Add(5);
                    data.AddRange(BitConverter.GetBytes((uint)Value).Reverse());
                    return data.ToArray();
                case ParameterType.s32:
                    data.Add(6);
                    data.AddRange(BitConverter.GetBytes((int)Value).Reverse());
                    return data.ToArray();
                case ParameterType.f32:
                    data.Add(7);
                    data.AddRange(BitConverter.GetBytes((float)Value).Reverse());
                    return data.ToArray();
                case ParameterType.str:
                    data.Add(8);
                    data.AddRange(BitConverter.GetBytes(((string)Value).Length).Reverse());
                    data.AddRange(Encoding.ASCII.GetBytes((string)Value));
                    return data.ToArray();
                default:
                    return null;
            }
        }
    }
}
