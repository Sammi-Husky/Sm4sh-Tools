// Copyright (c) Sammi Husky. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SALT.PARAMS
{
    public class ParamEntry
    {
        public ParamEntry(object value, ParamType type)
        {
            this.Value = value;
            this.Type = type;
        }

        public ParamType Type { get; set; }
        public object Value { get; set; }
        public int Size
        {
            get
            {
                switch (this.Type)
                {
                    case ParamType.s8:
                    case ParamType.u8:
                        return 2;
                    case ParamType.s16:
                    case ParamType.u16:
                        return 3;
                    case ParamType.s32:
                    case ParamType.u32:
                    case ParamType.f32:
                        return 5;
                    case ParamType.str:
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
                case ParamType.s8:
                    data.Add((byte)this.Value);
                    return data.ToArray();
                case ParamType.u8:
                    data.Add((byte)this.Value);
                    return data.ToArray();
                case ParamType.s16:
                    data.AddRange(BitConverter.GetBytes((short)this.Value).Reverse());
                    return data.ToArray();
                case ParamType.u16:
                    data.AddRange(BitConverter.GetBytes((ushort)this.Value).Reverse());
                    return data.ToArray();
                case ParamType.s32:
                    data.AddRange(BitConverter.GetBytes((int)this.Value).Reverse());
                    return data.ToArray();
                case ParamType.u32:
                    data.AddRange(BitConverter.GetBytes((uint)this.Value).Reverse());
                    return data.ToArray();
                case ParamType.f32:
                    data.AddRange(BitConverter.GetBytes((float)this.Value).Reverse());
                    return data.ToArray();
                case ParamType.str:
                    data.AddRange(BitConverter.GetBytes(((string)this.Value).Length).Reverse());
                    data.AddRange(Encoding.ASCII.GetBytes((string)this.Value));
                    return data.ToArray();
                case ParamType.group:
                    data.AddRange(((ParamGroup)this.Value).GetBytes());
                    return data.ToArray();
                default:
                    return null;
            }
        }

        public override string ToString()
        {
            switch (this.Type)
            {
                case ParamType.s8:
                case ParamType.u8:
                case ParamType.s16:
                case ParamType.u16:
                case ParamType.s32:
                case ParamType.u32:
                    return $"0x{this.Value:X}";
                case ParamType.f32:
                    return $"{this.Value:F}";
                case ParamType.str:
                    return this.Value.ToString();
                default:
                    return this.Value.ToString();
            }
        }
    }
}
