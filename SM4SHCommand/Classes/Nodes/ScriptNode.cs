using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Sm4shCommand.Classes;
using SALT.Moveset;

namespace Sm4shCommand.Nodes
{
    public class ScriptNode : BaseNode
    {
        private static ContextMenuStrip _menu;
        static ScriptNode()
        {
            _menu = new ContextMenuStrip();
        }

        public ScriptNode()
        {
            this.ContextMenuStrip = _menu;
            Scripts = new Dictionary<string, IScript>(4);

        }
        public ScriptNode(uint ident, string text) : this()
        {
            Identifier = ident;
            this.Text = text;
        }
        public ScriptNode(uint ident, string text, IScript script) : this(ident, text)
        {
            Identifier = ident;
            Scripts.Add("Script", script);
        }

        public Dictionary<string, IScript> Scripts { get; set; }
        public uint Identifier { get; set; }
    }
}
