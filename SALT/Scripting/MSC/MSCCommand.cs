// Copyright (c) Sammi Husky. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace SALT.Scripting.MSC
{
    public class MSCCommand : ICommand
    {
        public MSCCommand(uint raw)
        {
            this.Ident = raw;
            this.Parameters = new List<object>();
        }

        private uint Raw { get; set; }

        public uint Ident { get { return Raw & 0x7F; } set { Raw ^= value; } }
        public bool Returns { get { return (Raw & 0x80) > 0; } set { Raw ^= (uint)(value ? 0x80 : 0); } }
        public int FileOffset { get; set; }
        public string Name { get { return MSC_INFO.NAMES[this.Ident]; } }
        public int Size { get { return MSC_INFO.Sizes[this.Ident]; } }
        public List<object> Parameters { get; set; }
        public string[] ParamSyntax { get { return MSC_INFO.SYNTAX[this.Ident].Split(','); } }
        public string[] ParamSpecifiers { get { return MSC_INFO.FORMATS[this.Ident].Split(','); } }

        public byte[] GetBytes(System.IO.Endianness endian)
        {
            List<byte> data = new List<byte>();
            data.Add((byte)this.Ident);
            for (int i = 0; i < this.ParamSpecifiers.Length; i++)
            {
                var str = this.ParamSpecifiers[i];
                switch (str)
                {
                    case "B":
                        data.Add((byte)this.Parameters[i]);
                        break;
                    case "I":
                        data.AddRange(BitConverter.GetBytes((int)this.Parameters[i]).Reverse());
                        break;
                    case "H":
                        data.AddRange(BitConverter.GetBytes((int)this.Parameters[i]).Reverse());
                        break;
                }
            }

            if (endian == System.IO.Endianness.Big)
                return data.ToArray().Reverse().ToArray();
            else
                return data.ToArray();
        }
        public override string ToString()
        {
            return ToString(false);
        }
        public string ToString(bool declaration)
        {
            string str = string.Empty;
            if (this.Name == "unk")
                str += $"unk_{this.Ident:X}";
            else
                str = this.Name;
            List<string> tmp = new List<string>();
            for (int i = 0; i < this.ParamSpecifiers.Length; i++)
            {

                if (this.ParamSpecifiers[i] == "B")
                    tmp.Add("0x" + ((byte)this.Parameters[i]).ToString("X"));
                else if (this.ParamSpecifiers[i] == "I")
                    tmp.Add("0x" + ((int)this.Parameters[i]).ToString("X"));
                else if (this.ParamSpecifiers[i] == "H")
                    tmp.Add("0x" + ((short)this.Parameters[i]).ToString("X"));
            }

            if (declaration)
                str += " = ";
            else
                str += $"({string.Join(",", tmp)})";
            return str;
        }
    }
}
