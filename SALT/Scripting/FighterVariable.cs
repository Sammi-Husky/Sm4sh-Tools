using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SALT.Moveset
{
    public class FighterVariable
    {
        public FighterVariable(uint raw)
        {
            Raw = raw;
        }
        public FighterVariable()
        {
            Raw = 0;
        }
        public FighterVariable(string text)
        {
            ParseFromText(text);
        }
        public uint Raw { get; set; }
        public VarDataType DataType
        {
            get
            {
                return (VarDataType)((Raw & 0xF0000000) >> 28);
            }
            set
            {
                Raw |= (uint)value << 28;
            }
        }
        public VarSourceType SourceType
        {
            get
            {
                return (VarSourceType)((Raw & 0x0F000000) >> 24);
            }
            set
            {
                Raw |= (uint)value << 24;
            }
        }
        public uint VariableID
        {
            get
            {
                return Raw & 0x00FFFFFF;
            }
            set
            {
                Raw |= value;
            }
        }
        public override string ToString()
        {
            try
            {
                var s1 = Enum.GetName(typeof(VarSourceType), SourceType);
                var s2 = Enum.GetName(typeof(VarDataType), DataType);
                return $"{s1}-{s2}[{VariableID}]";
            }
            catch { return "0x" + Raw.ToString("X8"); }
        }
        public void ParseFromText(string text)
        {
            if (text.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
            {
                Raw = uint.Parse(text.Substring(2), NumberStyles.HexNumber);
            }
            else
            {
                var strSType = text.Substring(0, text.IndexOf('-'));
                var strDType = text.Substring(text.IndexOf('-') + 1, text.IndexOf('[') - (text.IndexOf('-') + 1));
                var stype = (VarSourceType)Enum.Parse(typeof(VarSourceType), strSType);
                var dtype = (VarDataType)Enum.Parse(typeof(VarDataType), strDType);

                uint ID = 0;
                string strID = text.Substring(text.IndexOf('[') + 1, text.IndexOf(']') - (text.IndexOf('[') + 1));
                if (strID.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
                {
                    ID = uint.Parse(strID.Substring(2), NumberStyles.HexNumber);
                }
                else
                {
                    ID = uint.Parse(strID);
                }
                VariableID = ID;
                DataType = dtype;
                SourceType = stype;
            }
        }
    }
    public enum VarSourceType
    {
        // Standard types // 
        LA = 0,
        RA = 1,

        // parameter file groups //
        fighter_param_common = 2,
        fighter_param = 3,
        unk4 = 4, //share the same param group as SpecialLw, but no customs and more generic usage
        SpecialN = 5,
        SpecialS = 6,
        SpecialHi = 7,
        SpecialLw = 8,
        fighter_param_equipment_ability = 9,

        // uncomfirmed
        ActionStatus1 = 14,
        ActionStatus2 = 15,
        undefined = 255
    }
    public enum VarDataType
    {
        Float = 0,
        Basic = 1,
        Bit = 2
    }
}
