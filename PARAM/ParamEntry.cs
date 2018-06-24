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
                switch (this.Type)
                {
                    case ParameterType.s8:
                    case ParameterType.u8:
                        return 2;
                    case ParameterType.s16:
                    case ParameterType.u16:
                        return 3;
                    case ParameterType.s32:
                    case ParameterType.u32:
                    case ParameterType.f32:
                        return 5;
                    case ParameterType.str:
                        return ((string)this.Value).Length + 1;
                    default:
                        return 0;
                }
            }
        }
        public byte[] GetBytes()
        {
            List<byte> data = new List<byte>();
            data.Add((byte)this.Type);
            switch (this.Type)
            {
                case ParameterType.s8:
                    data.Add((byte)((sbyte)this.Value));
                    return data.ToArray();
                case ParameterType.u8:
                    data.Add((byte)this.Value);
                    return data.ToArray();
                case ParameterType.s16:
                    data.AddRange(BitConverter.GetBytes((short)this.Value).Reverse());
                    return data.ToArray();
                case ParameterType.u16:
                    data.AddRange(BitConverter.GetBytes((ushort)this.Value).Reverse());
                    return data.ToArray();
                case ParameterType.s32:
                    data.AddRange(BitConverter.GetBytes((int)this.Value).Reverse());
                    return data.ToArray();
                case ParameterType.u32:
                    data.AddRange(BitConverter.GetBytes((uint)this.Value).Reverse());
                    return data.ToArray();
                case ParameterType.f32:
                    data.AddRange(BitConverter.GetBytes((float)this.Value).Reverse());
                    return data.ToArray();
                case ParameterType.str:
                    data.AddRange(BitConverter.GetBytes(((string)this.Value).Length).Reverse());
                    data.AddRange(Encoding.ASCII.GetBytes((string)this.Value));
                    return data.ToArray();
                default:
                    return null;
            }
        }
    }
}
