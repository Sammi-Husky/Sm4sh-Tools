using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Sm4shCommand.Classes;
using Sm4shCommand.Nodes;

namespace Sm4shCommand
{
    public partial class CodeEditControl : TabPage
    {
        public CodeEditControl(ScriptGroupNode group)
        {
            InitializeComponent();
            TabPage p = new TabPage("Main");
            p.Controls.Add(new ACMD_EDITOR(group.lists[0]/*, Runtime.commandDictionary*/) { Dock = DockStyle.Fill });
            tabControl1.TabPages.Add(p);
            p = new TabPage("GFX");
            p.Controls.Add(new ACMD_EDITOR(group.lists[1]/*, Runtime.commandDictionary*/) { Dock = DockStyle.Fill });
            tabControl1.TabPages.Add(p);
            p = new TabPage("SFX");
            p.Controls.Add(new ACMD_EDITOR(group.lists[2]/*, Runtime.commandDictionary*/) { Dock = DockStyle.Fill });
            tabControl1.TabPages.Add(p);
            p = new TabPage("Expression");
            p.Controls.Add(new ACMD_EDITOR(group.lists[3]/*, Runtime.commandDictionary*/) { Dock = DockStyle.Fill });
            tabControl1.TabPages.Add(p);
        }
        public CodeEditControl(ScriptNode node)
        {
            InitializeComponent();
            TabPage p = new TabPage("Script");
            p.Controls.Add(new ACMD_EDITOR(node.CommandList/*, Runtime.commandDictionary*/) { Dock = DockStyle.Fill });
            tabControl1.TabPages.Add(p);
        }

        public ACMD_EDITOR this[int index]
        {
            get { return (ACMD_EDITOR)tabControl1.TabPages[index].Controls[0]; }
        }

        private uint unkParam0;
        private uint unkParam1;
        private uint IASAFrame;
        private uint unkParam3;
        private uint unkParam4;
        private uint unkParam5;
    }
}
