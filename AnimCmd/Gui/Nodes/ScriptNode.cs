using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Sm4shCommand.Classes;

namespace Sm4shCommand.Nodes
{
    public class ScriptNode : BaseNode
    {
        public ScriptNode(string name, ACMDScript list) { Text = name; _list = list; CRC = list.AnimationCRC; }
        public ScriptNode(ACMDScript list) { _list = list; CRC = list.AnimationCRC; }

        public new string Name
        {
            get
            {
                return $"[{CRC:X8}]";
            }
        }
        public bool Dirty { get { return _list.Dirty; } }
        public ACMDScript CommandList { get { return _list; } set { _list = value; } }
        private ACMDScript _list;
    }
}
