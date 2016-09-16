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
                TabPage p = new TabPage(pair.Key);
                p.Controls.Add(new ITS_EDITOR(pair.Value, autocomplete) { Dock = DockStyle.Fill });
                tabControl1.TabPages.Add(p);
            }
        }
    }
}
