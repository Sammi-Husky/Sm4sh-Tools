using Sm4shCommand.GUI;
using Sm4shCommand.Nodes;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using static Sm4shCommand.Runtime;

namespace Sm4shCommand
{
    public partial class AcmdMain : Form
    {
        public AcmdMain()
        {
            InitializeComponent();
            Manager = new WorkspaceManager();
        }

        private WorkspaceManager Manager { get; set; }
        public Project ActiveProject
        {
            get
            {
                if (FileTree.SelectedNode == null)
                    return null;

                var node = FileTree.SelectedNode;
                while (!((node = node.Parent) is Project))
                    node = node.Parent;

                if (node is Project)
                    return (Project)node;
                else
                    return null;
            }
        }

        private void ACMDMain_Load(object sender, EventArgs e)
        {
            Runtime.LogMessage("Checking for external event dictionary..");
            if (File.Exists(Path.Combine(Application.StartupPath, "Events.cfg")))
            {
                Runtime.LogMessage("Event dictionary found, applying overrides..");
                GetCommandInfo(Path.Combine(Application.StartupPath, "Events.cfg"));
            }
            Runtime.LogMessage("Done.");
        }
        private void tabControl1_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index > tabControl1.TabPages.Count - 1)
                return;

            if (e.Index == tabControl1.SelectedIndex)
                e.Graphics.FillRectangle(Brushes.ForestGreen, e.Bounds);
            else
                e.Graphics.FillRectangle(SystemBrushes.ActiveBorder, e.Bounds);

            e.Graphics.FillEllipse(new SolidBrush(Color.IndianRed), e.Bounds.Right - 18, e.Bounds.Top + 3, e.Graphics.MeasureString("x", Font).Width + 4, Font.Height);
            e.Graphics.DrawEllipse(Pens.Black, e.Bounds.Right - 18, e.Bounds.Top + 3, e.Graphics.MeasureString("X", Font).Width + 3, Font.Height);
            e.Graphics.DrawString("X", new Font(e.Font, FontStyle.Bold), Brushes.Black, e.Bounds.Right - 17, e.Bounds.Top + 3);
            e.Graphics.DrawString(tabControl1.TabPages[e.Index].Text, e.Font, Brushes.Black, e.Bounds.Left + 17, e.Bounds.Top + 3);
            e.DrawFocusRectangle();
        }


        private void cboMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            Runtime.WorkingEndian = (Endianness)cboMode.SelectedIndex;
        }
        private void aboutToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            var abtBox = new AboutBox();
            abtBox.ShowDialog();
        }
        private void fileToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Filter = "All Supported Files (*.bin, *.mscsb)|*.bin;*.mscsb|" +
                              "ACMD Binary (*.bin)|*.bin|" +
                              "MotionScript Binary (*.mscsb)|*.mscsb|" +
                              "All Files (*.*)|*.*";
                //if (dlg.ShowDialog() == DialogResult.OK)
                //Manager.OpenFile(dlg.FileName);

            }
        }

        private void ProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Filter = "All Files (*.*)|*.*|Project (*.fitproj)|*.fitproj";
                if (dlg.ShowDialog() == DialogResult.OK)
                    Manager.OpenProject(dlg.FileName);
            }
        }

        private void FileTree_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node is ScriptNode)
            {
                var n = e.Node as ScriptNode;
                var ce = new CodeEditor(n);
                ce.Name = n.Text + e.Node.Index;
                ce.Text = e.Node.Text;
                if (tabControl1.TabPages.ContainsKey(ce.Name))
                {
                    tabControl1.SelectTab(ce.Name);
                    return;
                }
                else
                {
                    tabControl1.TabPages.Insert(0, ce);
                    tabControl1.SelectTab(0);
                }
            }

        }
        private void FileTree_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            FileTree.SelectedNode = FileTree.GetNodeAt(e.Location);
        }

        private void tabControl1_MouseClick(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < tabControl1.TabCount; i++)
            {
                var p = tabControl1.TabPages[i] as CodeEditor;

                TabControl tmp = (TabControl)p.Controls[0];

                Rectangle r = tabControl1.GetTabRect(i);
                Rectangle closeButton = new Rectangle(r.Right - 18, r.Top + 3, 13, Font.Height);
                if (closeButton.Contains(e.Location))
                {
                    for (int x = 0; x < tmp.TabCount; x++)
                    {
                        ITS_EDITOR box = (ITS_EDITOR)tmp.TabPages[x].Controls[0];
                        box.ApplyChanges();
                    }
                    tabControl1.TabPages.Remove(p);
                    return;
                }
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {

            using (var dlg = new FolderSelectDialog())
                if (dlg.ShowDialog() == DialogResult.OK)
                    Manager.SaveProject("Wario",dlg.SelectedPath);

        }
    }
}