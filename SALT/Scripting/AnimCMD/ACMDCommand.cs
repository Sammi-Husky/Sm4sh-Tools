// Copyright (c) Sammi Husky. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace SALT.Moveset.AnimCMD
{
    public class ACMDCommand : ICommand
    {
        public ACMDCommand(uint crc)
        {
            this.Ident = crc;
            this.Parameters = new List<object>();
        }

        public uint Ident { get; set; }
        public bool Dirty { get; set; }
        public int Size { get { return this.WordSize * 4; } }
        public string Name { get { return ACMD_INFO.CMD_NAMES[this.Ident]; } }
        public int WordSize { get { return ACMD_INFO.CMD_SIZES[this.Ident]; } }
        public int[] ParamSpecifiers
        {
            get
            {
                if (!string.IsNullOrEmpty(ACMD_INFO.PARAM_FORMAT[this.Ident]))
                {
                    return ACMD_INFO.PARAM_FORMAT[this.Ident].Split(',').
                        Select(x => int.Parse(x)).ToArray();
                }
                else
                {
                    return new int[0];
                }
            }
        }

        public string[] ParamSyntax =>
              ACMD_INFO.PARAM_SYNTAX[this.Ident].Split(',');

        public List<object> Parameters { get; set; }
        public override string ToString()
        {
            string param = string.Empty;
            for (int i = 0; i < this.Parameters.Count; i++)
            {
                if (this.ParamSpecifiers.Length > 0)
                    param += $"{this.ParamSyntax[i]}=";

                if (this.Parameters[i] is int | this.Parameters[i] is bint)
                    param += string.Format("0x{0:X}{1}", this.Parameters[i], i + 1 != this.Parameters.Count ? ", " : string.Empty);
                else if (this.Parameters[i] is float | this.Parameters[i] is bfloat)
                    param += string.Format("{0}{1}", this.Parameters[i], i + 1 != this.Parameters.Count ? ", " : string.Empty);
                else if (this.Parameters[i] is decimal)
                    param += string.Format("{0}{1}", this.Parameters[i], i + 1 != this.Parameters.Count ? ", " : string.Empty);
                else if (this.Parameters[i] is FighterVariable)
                    param += $"{this.Parameters[i]}{(i + 1 != this.Parameters.Count ? ", " : string.Empty)}";
            }

            return $"{this.Name}({param})";
        }

        public virtual byte[] GetBytes(Endianness endian)
        {
            byte[] tmp = new byte[this.Size];
            Util.SetWord(ref tmp, this.Ident, 0, endian);
            for (int i = 0; i < this.ParamSpecifiers.Length; i++)
            {
                if (this.ParamSpecifiers[i] == 0)
                {
                    Util.SetWord(ref tmp, Convert.ToInt32(this.Parameters[i]), (i + 1) * 4, endian);
                }
                else if (this.ParamSpecifiers[i] == 1)
                {
                    double _hex = Convert.ToDouble(this.Parameters[i]);
                    float flt = (float)_hex;
                    byte[] bytes = BitConverter.GetBytes(flt);
                    int dec = BitConverter.ToInt32(bytes, 0);
                    string _hexval = dec.ToString("X");

                    Util.SetWord(ref tmp, int.Parse(_hexval, System.Globalization.NumberStyles.HexNumber), (i + 1) * 4, endian);
                }
                else if (this.ParamSpecifiers[i] == 2)
                {
                    Util.SetWord(ref tmp, (long)Convert.ToDecimal(this.Parameters[i]), (i + 1) * 4, endian);
                }
                else if (this.ParamSpecifiers[i] == 3)
                {
                    Util.SetWord(ref tmp, ((FighterVariable)this.Parameters[i]).Raw, (i + 1) * 4, endian);
                }
            }

            return tmp;
        }
    }
}
