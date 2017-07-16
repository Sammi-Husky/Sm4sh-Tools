using Sm4shCommand.Nodes;
using System.Windows.Forms;

namespace Sm4shCommand.GUI
{
    public partial class CodeEditor : TabPage
    {
        public CodeEditor(ScriptNode node)
        {
            InitializeComponent();
            foreach (var pair in node.Scripts)
            {
                TabPage p = new TabPage(pair.Key);
                p.Controls.Add(new ITS_EDITOR(pair.Value) { Dock = DockStyle.Fill });
                tabControl1.TabPages.Add(p);
            }
        }
        public CodeEditor(ScriptNode node, string[] autocomplete)
        {
            InitializeComponent();
            foreach (var pair in node.Scripts)
            {
                if (pair.Value is SALT.Moveset.MSC.MSCScript)
                {
                    TabPage p = new TabPage(pair.Key);
                    p.Controls.Add(new ITS_EDITOR((SALT.Moveset.MSC.MSCScript)pair.Value,false) { Dock = DockStyle.Fill });
                    tabControl1.TabPages.Add(p);

                    p = new TabPage(pair.Key);
                    p.Controls.Add(new ITS_EDITOR((SALT.Moveset.MSC.MSCScript)pair.Value, true) { Dock = DockStyle.Fill });
                    tabControl1.TabPages.Add(p);
                }
                else
                {
                    TabPage p = new TabPage(pair.Key);
                    p.Controls.Add(new ITS_EDITOR((SALT.Moveset.AnimCMD.ACMDScript)pair.Value) { Dock = DockStyle.Fill });
                    tabControl1.TabPages.Add(p);
                }
            }
        }
    }
}
