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
    public partial class CodeEditControl : UserControl
    {
        public CodeEditControl(CommandListGroup group)
        {
            InitializeComponent();
            pnlParams.Enabled = true;
            TabPage p = new TabPage("Main");
            p.Controls.Add(new ITSCodeBox(group.lists[0], Runtime.commandDictionary) { Dock = DockStyle.Fill });
            tabControl1.TabPages.Add(p);
            p = new TabPage("GFX");
            p.Controls.Add(new ITSCodeBox(group.lists[1], Runtime.commandDictionary) { Dock = DockStyle.Fill });
            tabControl1.TabPages.Add(p);
            p = new TabPage("SFX");
            p.Controls.Add(new ITSCodeBox(group.lists[2], Runtime.commandDictionary) { Dock = DockStyle.Fill });
            tabControl1.TabPages.Add(p);
            p = new TabPage("Expression");
            p.Controls.Add(new ITSCodeBox(group.lists[3], Runtime.commandDictionary) { Dock = DockStyle.Fill });
            tabControl1.TabPages.Add(p);
        }
        public CodeEditControl(CommandListNode node)
        {
            InitializeComponent();
            pnlParams.Enabled = false;
            TabPage p = new TabPage("Script");
            p.Controls.Add(new ITSCodeBox(node.CommandList, Runtime.commandDictionary) { Dock = DockStyle.Fill });
            tabControl1.TabPages.Add(p);
        }

        public ITSCodeBox this[ACMDType type]
        {
            get { return (ITSCodeBox)tabControl1.TabPages[(int)type].Controls[0]; }
        }

        private uint unkParam0;
        private uint UnkParam1;
        private uint IASAFrame;
        private uint unkParam3;
        private uint unkParam4;
        private uint unkParam5;
    }
}
