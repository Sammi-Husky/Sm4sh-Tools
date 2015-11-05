using System;
using Sm4shCommand.Classes;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace Sm4shCommand
{
    public unsafe partial class ACMDMain : Form
    {
        public ACMDMain()
        {
            InitializeComponent();
            _manager = new WorkspaceManager();
        }

        internal WorkspaceManager Manager { get { return _manager != null ? _manager : new WorkspaceManager(); } }
        private WorkspaceManager _manager;

        public void OpenWorkspace(string wrkspce)
        {
            Manager.ReadWRKSPC(wrkspce);
            List<TreeNode> col = new List<TreeNode>();

            foreach (Project p in Manager._projects)
            {
                Runtime._curFighter = Manager.OpenFighter(p.ACMDPath);
                Runtime.AnimHashPairs = Manager.getAnimNames(p.AnimationFile);

                string name = String.Format("{0} - [{1}]",
                    p.ProjectName, p.ProjectType == ProjType.Fighter ? "Fighter" : "Weapon");

                TreeNode pNode = new TreeNode(name);

                TreeNode Actions = new TreeNode("MSCSB (ActionScript)");
                TreeNode ACMD = new TreeNode("ACMD (AnimCmd)");
                TreeNode Weapons = new TreeNode("Weapons");
                TreeNode Parameters = new TreeNode("Parameters");


                foreach (uint u in Runtime._curFighter.MotionTable)
                {
                    if (u == 0)
                        continue;

                    CommandListGroup g = new CommandListGroup(Runtime._curFighter, u);

                    if (Runtime.AnimHashPairs.ContainsKey(u))
                        g.Text = Runtime.AnimHashPairs[u];

                    ACMD.Nodes.Add(g);
                }

                pNode.Nodes.AddRange(new TreeNode[] { Actions, ACMD, Weapons, Parameters });
                col.Add(pNode);
            }
            FileTree.Nodes.AddRange(col.ToArray());
        }
        public void DisplayScript(CommandList list)
        {

            StringBuilder sb = new StringBuilder();
            foreach (Command cmd in list)
                sb.Append(cmd.ToString() + "\n");

            if (list.Empty)
                sb.Append("//    Empty List    //");

            ITSCodeBox box = (ITSCodeBox)tabControl1.SelectedTab.Controls[0];
            box.Text = sb.ToString();
            box.CommandList = list;
        }

        private void ACMDMain_Load(object sender, EventArgs e)
        {

            if (File.Exists(Application.StartupPath + "/Events.cfg"))
                Runtime.GetCommandInfo(Application.StartupPath + "/Events.cfg");
            else
                MessageBox.Show("Could not load Events.cfg");

            if (!String.IsNullOrEmpty(Manager.WorkspaceRoot))
                OpenWorkspace(Manager.WorkspaceRoot);
        }
        private void ACMDMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            Runtime.SaveCommandInfo(Application.StartupPath + "/Events.cfg");
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (TabPage p in tabControl1.TabPages)
            {
                ITSCodeBox box = (ITSCodeBox)p.Controls[0];
                if (!Runtime.isRoot)
                    Runtime._curFile.EventLists[box.CommandList.AnimationCRC] = box.ParseCodeBox();
                else
                    Runtime._curFighter[(int)box.CommandList._parent.Type].EventLists[box.CommandList.AnimationCRC] = box.ParseCodeBox();
            }

            if (Runtime.isRoot)
            {

                Runtime._curFighter.Main.Export(Runtime.rootPath + "/game.bin");
                Runtime._curFighter.GFX.Export(Runtime.rootPath + "/effect.bin");
                Runtime._curFighter.SFX.Export(Runtime.rootPath + "/sound.bin");
                Runtime._curFighter.Expression.Export(Runtime.rootPath + "/expression.bin");
                Runtime._curFighter.MotionTable.Export(Runtime.rootPath + "/Motion.mtable");
            }
            else
                Runtime._curFile.Export(Runtime.FileName);
        }
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (TabPage p in tabControl1.TabPages)
            {
                ITSCodeBox box = (ITSCodeBox)p.Controls[0];
                if (box.CommandList.Dirty)
                {
                    if (!Runtime.isRoot)
                        Runtime._curFile.EventLists[box.CommandList.AnimationCRC] = box.ParseCodeBox();
                    else
                        Runtime._curFighter[(int)box.CommandList._parent.Type].EventLists[box.CommandList.AnimationCRC] = box.ParseCodeBox();
                }
            }

            if (Runtime.isRoot)
            {
                FolderSelectDialog dlg = new FolderSelectDialog();
                DialogResult result = dlg.ShowDialog();
                if (result == DialogResult.OK)
                {
                    Runtime._curFighter.Main.Export(dlg.SelectedPath + "/game.bin");
                    Runtime._curFighter.GFX.Export(dlg.SelectedPath + "/effect.bin");
                    Runtime._curFighter.SFX.Export(dlg.SelectedPath + "/sound.bin");
                    Runtime._curFighter.Expression.Export(dlg.SelectedPath + "/expression.bin");
                    Runtime._curFighter.MotionTable.Export(dlg.SelectedPath + "/Motion.mtable");
                }
                dlg.Dispose();
            }
            else
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.Filter = "ACMD Binary (*.bin)|*.bin|All Files (*.*)|*.*";
                DialogResult result = dlg.ShowDialog();
                if (result == DialogResult.OK)
                    Runtime._curFile.Export(dlg.FileName);
                dlg.Dispose();
            }
        }

        private void workspaceToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            DialogResult result = dlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                Runtime.FileName = Runtime.rootPath = String.Empty;
                Runtime._curFile = null;
                cmdListTree.Nodes.Clear();
                FileTree.Nodes.Clear();
                tabControl1.TabPages.Clear();
                Runtime.isRoot = true;

                OpenWorkspace(dlg.FileName);
            }
            dlg.Dispose();
        }
        private void fileToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "ACMD Binary (*.bin)|*.bin| All Files (*.*)|*.*";
                DialogResult result = dlg.ShowDialog();
                if (result == DialogResult.OK)
                {
                    Runtime.FileName = Runtime.rootPath = String.Empty;
                    tabControl1.TabPages.Clear();
                    cmdListTree.Nodes.Clear();
                    Runtime.isRoot = cmdListTree.ShowLines = cmdListTree.ShowRootLines = false;

                    if ((Runtime._curFile = Manager.OpenFile(dlg.FileName)) != null)
                    {
                        foreach (CommandList list in Runtime._curFile.EventLists.Values)
                            cmdListTree.Nodes.Add(new CommandListNode(String.Format("[{0:X8}]", list.AnimationCRC), Runtime._curFile.EventLists[list.AnimationCRC]));

                        Runtime.FileName = dlg.FileName;
                        this.Text = String.Format("Main Form - {0}", Runtime.FileName);
                    }
                }
            }
        }
        private void btnHexView_Click(object sender, EventArgs e)
        {
            if (!(cmdListTree.SelectedNode is CommandListNode))
                return;

            byte[] data = ((CommandListNode)cmdListTree.SelectedNode).CommandList.ToArray();
            HexView f = new HexView(data);
            f.Text = String.Format("HexView - {0} - ReadOnly", cmdListTree.SelectedNode.Text);
            f.Show();
        }
        private void eventLibraryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EventLibrary dlg = new EventLibrary();
            dlg.Show();
        }
        private void dumpAsTextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!Runtime.isRoot)
                return;

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Plain Text (.txt) | *.txt";
            DialogResult result = dlg.ShowDialog();
            if (result == DialogResult.OK)
                using (StreamWriter writer = new StreamWriter(dlg.FileName, false, Encoding.UTF8))
                    writer.Write(Runtime._curFighter.ToString());
        }
        private void cmdListTree_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            Color color = SystemColors.Window;
            if (e.Node is CommandListNode)
                color = (e.Node as CommandListNode).CommandList.Dirty ? Color.Red : Color.Black;

            if (e.Node.IsSelected)
                e.DrawDefault = true;
            else
                e.Graphics.DrawString(e.Node.Text, Font, new SolidBrush(color), e.Bounds.X, e.Bounds.Y);
        }
        private void cmdListTree_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (cmdListTree.SelectedNode is CommandListNode)
            {
                CommandListNode w = (CommandListNode)cmdListTree.SelectedNode;

                // Get the name for node, depending on whether we have animation names or not.
                // We'll also use this + node index as a unique identifier.
                string name = string.IsNullOrEmpty(w.AnimationName) ? w.CRC.ToString("X8") : w.AnimationName;

                // If this list is already open, switch to it's tab.
                // else, add a new tab and display it's script.
                if (tabControl1.TabPages.ContainsKey(name + w.Index))
                    tabControl1.SelectTab(name + w.Index);
                else
                {
                    TabPage p = new TabPage(w.Text) { Name = name + w.Index };
                    p.Controls.Add(new ITSCodeBox(((CommandListNode)cmdListTree.SelectedNode).CommandList) { Dock = DockStyle.Fill, WordWrap = false });
                    tabControl1.TabPages.Insert(0, p);
                    tabControl1.SelectedIndex = 0;
                    DisplayScript(((CommandListNode)cmdListTree.SelectedNode).CommandList);
                }
            }
        }
        private void tabControl1_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index != tabControl1.SelectedIndex)
            {
                Rectangle r = tabControl1.GetTabRect(e.Index);
                e.Graphics.FillRectangle(SystemBrushes.InactiveCaption, r);
                e.Graphics.DrawString(tabControl1.SelectedTab.Text, Font, SystemBrushes.MenuText, r.Left + 2, r.Right + 2);
            }

            e.Graphics.FillEllipse(new SolidBrush(Color.IndianRed), e.Bounds.Right - 18, e.Bounds.Top + 3, e.Graphics.MeasureString("x", Font).Width + 4, Font.Height);
            e.Graphics.DrawEllipse(Pens.Black, e.Bounds.Right - 18, e.Bounds.Top + 3, e.Graphics.MeasureString("X", Font).Width + 3, Font.Height);
            e.Graphics.DrawString("X", new Font(e.Font, FontStyle.Bold), Brushes.Black, e.Bounds.Right - 17, e.Bounds.Top + 3);
            e.Graphics.DrawString(this.tabControl1.TabPages[e.Index].Text, e.Font, Brushes.Black, e.Bounds.Left + 17, e.Bounds.Top + 3);
            e.DrawFocusRectangle();
        }
        private void tabControl1_MouseClick(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < tabControl1.TabCount; i++)
            {
                TabPage p = tabControl1.TabPages[i];

                ITSCodeBox box = (ITSCodeBox)p.Controls[0];
                Rectangle r = tabControl1.GetTabRect(i);
                Rectangle closeButton = new Rectangle(r.Right - 17, r.Top + 4, 12, 10);
                if (closeButton.Contains(e.Location))
                {
                    if (!Runtime.isRoot)
                        Runtime._curFile.EventLists[box.CommandList.AnimationCRC] = box.ParseCodeBox();
                    else
                        Runtime._curFighter[(int)box.CommandList._parent.Type].EventLists[box.CommandList.AnimationCRC] = box.ParseCodeBox();
                    this.tabControl1.TabPages.Remove(p);
                    return;
                }
            }
        }

        private void parseAnimationsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            DialogResult result = dlg.ShowDialog();
            try
            {
                if (result == DialogResult.OK)
                {
                    TreeView tree = Runtime.isRoot ? FileTree : cmdListTree;
                    Runtime.AnimHashPairs = Manager.getAnimNames(dlg.FileName);

                    tree.BeginUpdate();
                    for (int i = 0; i < tree.Nodes.Count; i++)
                    {
                        if (tree.Nodes[i] is CommandListNode | tree.Nodes[i] is CommandListGroup)
                        {
                            uint crc;
                            if (tree.Nodes[i] is CommandListNode)
                                crc = ((CommandListNode)tree.Nodes[i]).CRC;
                            else
                                crc = ((CommandListGroup)tree.Nodes[i])._crc;

                            if (Runtime.AnimHashPairs.ContainsKey(crc))
                                tree.Nodes[i].Text = Runtime.AnimHashPairs[crc];
                        }
                    }
                    tree.EndUpdate();
                }
            }
            catch (Exception x) { MessageBox.Show("Error reading animation file\n: " + x.Message); }
        }
        private void FileTree_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (FileTree.SelectedNode is CommandListGroup)
            {
                cmdListTree.Nodes.Clear();
                CommandListGroup g = (CommandListGroup)FileTree.SelectedNode;

                cmdListTree.Nodes.Add(new CommandListNode("Main", g.lists[0]));
                cmdListTree.Nodes.Add(new CommandListNode("GFX", g.lists[1]));
                cmdListTree.Nodes.Add(new CommandListNode("SFX", g.lists[2]));
                cmdListTree.Nodes.Add(new CommandListNode("Expression", g.lists[3]));
            }
        }

        private void fighterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Runtime.FileName = Runtime.rootPath = String.Empty;
            Runtime._curFile = null;
            cmdListTree.Nodes.Clear();
            tabControl1.TabPages.Clear();
            Runtime.isRoot = true;

            FolderSelectDialog dlg = new FolderSelectDialog();

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                FileTree.BeginUpdate();
                Runtime._curFighter = _manager.OpenFighter(dlg.SelectedPath);
                for (int i = 0; i < 4; i++)
                    foreach (uint u in Runtime._curFighter.MotionTable)
                        if (u != 0)
                            FileTree.Nodes.Add(new CommandListGroup(Runtime._curFighter, u));
                FileTree.EndUpdate();
            }
        }
        private void workspaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WorkspaceWizard dlg = new WorkspaceWizard();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _manager.NewWorkspace(dlg.WorkspaceName, dlg.SourceDirectory, dlg.DestinationDirectory);

                Runtime.FileName = Runtime.rootPath = String.Empty;
                Runtime._curFile = null;
                cmdListTree.Nodes.Clear();
                tabControl1.TabPages.Clear();
                Runtime.isRoot = true;
                cmdListTree.ShowLines = cmdListTree.ShowRootLines = true;

                OpenWorkspace(dlg.DestinationDirectory + Path.DirectorySeparatorChar + dlg.WorkspaceName +
                                Path.DirectorySeparatorChar + dlg.WorkspaceName + ".WRKSPC");

            }
        }
    }

    public class CommandListNode : TreeNode
    {
        public CommandListNode(string Name, CommandList List) { Text = Name; _list = List; _crc = List.AnimationCRC; }
        public CommandListNode(CommandList List) { _list = List; _crc = List.AnimationCRC; }

        public uint CRC { get { return _crc; } set { _crc = value; } }
        private uint _crc;

        public string AnimationName { get { return _anim; } set { _anim = value; } }
        private string _anim;

        public CommandList CommandList { get { return _list; } set { _list = value; } }
        private CommandList _list;
    }

    public class CommandListGroup : TreeNode
    {

        public Fighter _fighter;
        public uint _crc;

        public CommandListGroup(Fighter fighter, uint CRC)
        {
            _fighter = fighter;
            _crc = CRC;
            Text = String.Format("[{0:X8}]", CRC);

            for (int i = 0; i < 4; i++)
            {
                if (fighter[i].EventLists.ContainsKey(CRC))
                    lists.Add(fighter[i].EventLists[CRC]);
                else
                {
                    CommandList cml = new CommandList(CRC, fighter[i]);
                    cml.Initialize();
                    lists.Add(cml);
                }
            }
        }

        public List<CommandList> lists = new List<CommandList>(4);
    }

    public enum Endianness
    {
        Little = 0,
        Big = 1
    }
}



