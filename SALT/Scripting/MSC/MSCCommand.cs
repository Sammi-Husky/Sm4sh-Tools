using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SALT.Scripting.MSC
{
    public class MSCCommand : ICommand
    {
        public MSCCommand(uint ident)
        {
            Ident = ident;
            Parameters = new List<object>();
        }
        public uint Ident { get; set; }
        public string Name { get { return MSC_INFO.NAMES[Ident]; } }
        public int Size { get { return MSC_INFO.Sizes[Ident]; } }
        public List<object> Parameters { get; set; }
        public string[] ParamSyntax { get { return MSC_INFO.SYNTAX[Ident].Split(','); } }
        public string[] ParamSpecifiers { get { return MSC_INFO.FORMATS[Ident].Split(','); } }

        public byte[] GetBytes(System.IO.Endianness endian)
        {
            List<byte> data = new List<byte>();
            data.Add((byte)Ident);
            for (int i = 0; i < ParamSpecifiers.Length; i++)
            {
                var str = ParamSpecifiers[i];
                switch (str)
                {
                    case "B":
                        data.Add((byte)Parameters[i]);
                        break;
                    case "I":
                        data.AddRange(BitConverter.GetBytes((int)Parameters[i]));
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
            string str = "";
            if (Name == "unk")
                str += $"unk_{Ident:X}";
            else
                str = Name;
            List<string> tmp = new List<string>();
            for (int i = 0; i < ParamSpecifiers.Length; i++)
            {

                if (ParamSpecifiers[i] == "B")
                    tmp.Add("0x" + ((byte)Parameters[i]).ToString("X"));
                else if (ParamSpecifiers[i] == "I")
                    tmp.Add("0x" + ((int)Parameters[i]).ToString("X"));
            }
            str += $"({string.Join(",", tmp)})";
            return str;
        }
    }
}
