using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Sm4shCommand.Classes;

namespace Sm4shCommand.Nodes
{
    public class CommandListNode : BaseNode
    {
        public CommandListNode(string name, CommandList list) { Text = name; _list = list; CRC = list.AnimationCRC; }
        public CommandListNode(CommandList list) { _list = list; CRC = list.AnimationCRC; }

        public new string Name
        {
            get
            {
                return $"[{CRC:X8}]";
            }
        }

        public CommandList CommandList { get { return _list; } set { _list = value; } }
        private CommandList _list;
    }
}
