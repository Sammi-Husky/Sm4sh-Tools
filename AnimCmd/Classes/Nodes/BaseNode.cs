using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Sm4shCommand.Classes;
using System.Security.Cryptography;

namespace Sm4shCommand.Nodes
{
    public class BaseNode : TreeNode
    {
        private static ContextMenuStrip _emptyMenu = new ContextMenuStrip();

        public Fighter Fighter { get { return _fighter; } set { _fighter = value; } }
        private Fighter _fighter;

        public uint CRC { get { return _crc; } set { _crc = value; } }
        private uint _crc;

        public new string Name
        {
            get
            {
                string s = "";
                _fighter?.AnimationHashPairs.TryGetValue(_crc, out s);
                if (string.IsNullOrEmpty(s))
                    s = $"[{_crc:X8}]";
                return s;
            }
            set
            {
                _crc = Crc32.Compute(Encoding.ASCII.GetBytes(value.ToLower()));
                if ((bool)_fighter?.AnimationHashPairs.ContainsKey(_crc))
                    return;
                else
                    _fighter?.AnimationHashPairs.Add(_crc, value);
            }
        }
    }
}
