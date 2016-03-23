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
            _menu.Items.Add(new ToolStripMenuItem("Export", null, ExportAction));
        }
        public ACMDNode()
        {
            ContextMenuStrip = _menu;
        }
        protected static void NewScriptAction(object sender, EventArgs e)
        {
            GetInstance<ACMDNode>().NewScript();
        }
        protected static void ExportAction(object sender, EventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog() { Filter = "ACMD Binary (*.bin)|*.bin|All Files (*.*)|*.*" };
            if (dlg.ShowDialog() == DialogResult.OK)
                GetInstance<ACMDNode>().Export(dlg.FileName);
        }
        public void Export(string path)
        {
            if (Runtime.isRoot)
                Runtime._curFighter.Export(path);
            else
                Runtime._curFile.Export(path);
        }
        public void NewScript()
        {
            RenameForm frm = new RenameForm("AnimName");
            if (frm.ShowDialog() == DialogResult.OK)
            {
                uint crc = System.Security.Cryptography.Crc32.Compute(Encoding.ASCII.GetBytes(frm.NewName));
                var cml = new ACMDScript(crc);
                if (Runtime.isRoot)
                {
                    Runtime._curFighter[(ACMDType)0].EventLists.Add(crc, cml);
                    Runtime._curFighter[(ACMDType)1].EventLists.Add(crc, cml);
                    Runtime._curFighter[(ACMDType)2].EventLists.Add(crc, cml);
                    Runtime._curFighter[(ACMDType)3].EventLists.Add(crc, cml);
                }
                else
                    Runtime._curFile.EventLists.Add(crc, cml);

                TreeNode n = null;
                if (Runtime.isRoot)
                    n = new ScriptGroupNode(Runtime._curFighter, cml.AnimationCRC) { Text = $"{this.Nodes.Count:X} - {frm.NewName}" };
                else
                   n = new ScriptNode(cml) { Text = $"{this.Nodes.Count:X} - {frm.NewName}" };
                this.Nodes.Add(n);
                Runtime.Instance.FileTree.SelectedNode = n;
            }
        }
    }
}
