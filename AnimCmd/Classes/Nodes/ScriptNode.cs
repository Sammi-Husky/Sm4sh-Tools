using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Sm4shCommand.Classes;
using SALT.Scripting;

namespace Sm4shCommand.Nodes
{
    public class ScriptNode : BaseNode
    {
        private static ContextMenuStrip _menu;
        static ScriptNode()
        {
            _menu = new ContextMenuStrip();
        }
        public ScriptNode(string text)
        {
            this.Text = text;
            this.ContextMenuStrip = _menu;
            Scripts = new Dictionary<string, IScript>(4);
        }
        public Dictionary<string, IScript> Scripts { get; set; }
        public string ScriptName { get; set; }
    }
}
