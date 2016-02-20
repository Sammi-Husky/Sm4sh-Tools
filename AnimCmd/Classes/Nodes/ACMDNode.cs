using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sm4shCommand.Classes;
using System.Windows.Forms;
using System.ComponentModel;

namespace Sm4shCommand.Nodes
{
    class ACMDNode : BaseNode
    {
        private static ContextMenuStrip _menu;
        static ACMDNode()
        {
            _menu = new ContextMenuStrip();
            _menu.Items.Add(new ToolStripMenuItem("New Script", null, NewScriptAction));
        }
        protected static void NewScriptAction(object sender, EventArgs e)
        {

        }
        public void AcmdMain(ACMDFile resource)
        {

        }
    }
}
