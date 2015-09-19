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
        }

        //=================================================\\
        // Current file that is open and free for editing. \\
        //=================================================\\

        ACMDFile _curFile;
        Dictionary<uint, string> AnimHashPairs = new Dictionary<uint, string>();
        Fighter _curFighter;


        // Misc runtime variables.
        bool isRoot = false;
        string FileName;
        string rootPath;
        Endianness workingEndian;


        // Parses an MTable file. Basically copies all data into a list of uints.
        public MTable ParseMTable(DataSource source, Endianness endian)
        {
            List<uint> CRCTable = new List<uint>();

            for (int i = 0; i < source.Length; i += 4)
                //if((uint)Util.GetWordUnsafe((source.Address + i), endian) != 0)
                CRCTable.Add((uint)Util.GetWordUnsafe((source.Address + i), endian));

            return new MTable(CRCTable, endian);
        }

        public ACMDFile OpenFile(string Filepath)
        {
            DataSource source = new DataSource(FileMap.FromFile(Filepath));

            if (*(byte*)(source.Address + 0x04) == 0x02)
                workingEndian = Endianness.Little;
            else if ((*(byte*)(source.Address + 0x04) == 0x00))
                workingEndian = Endianness.Big;
            else
            {
                MessageBox.Show("Could not determine endianness of file. Unsupported file version or file header is corrupt.");
                return null;
            }

            return new ACMDFile(source, workingEndian);
        }
        public Fighter OpenWorkspace(string dirPath)
        {
            Fighter f = new Fighter();
            try
            {

                f.Main = OpenFile(dirPath + "/game.bin");
                f.GFX = OpenFile(dirPath + "/effect.bin");
                f.SFX = OpenFile(dirPath + "/sound.bin");
                f.Expression = OpenFile(dirPath + "/expression.bin");

                f.Main.Type = ACMDType.Main;
                f.GFX.Type = ACMDType.GFX;
                f.SFX.Type = ACMDType.SFX;
                f.Expression.Type = ACMDType.Expression;

                f.MotionTable = ParseMTable(new DataSource(FileMap.FromFile(dirPath + "/motion.mtable")), workingEndian);
            }
            catch (FileNotFoundException x) { MessageBox.Show(x.Message); return null; }

            int counter = 0;
            foreach (uint u in f.MotionTable)
            {
                if (u == 0)
                    continue;

                TreeNode n = new TreeNode(String.Format("{0:X} [{1:X8}]", counter, u));

                if (f.Main.EventLists.ContainsKey(u))
                    n.Nodes.Add(new CommandListNode("Main", f[0].EventLists[u]));
                if (f.GFX.EventLists.ContainsKey(u))
                    n.Nodes.Add(new CommandListNode("GFX", f[1].EventLists[u]));
                if (f.SFX.EventLists.ContainsKey(u))
                    n.Nodes.Add(new CommandListNode("Sound", f[2].EventLists[u]));
                if (f.Expression.EventLists.ContainsKey(u))
                    n.Nodes.Add(new CommandListNode("Expression", f[3].EventLists[u]));

                cmdListTree.Nodes.Add(n);
                counter++;
            }
            isRoot = true;
            rootPath = dirPath;
            this.Text = String.Format("Main Form - {0}", dirPath);
            return f;
        }
        public void Rebuild()
        {
            foreach (TabPage p in tabControl1.TabPages)
            {
                ITSCodeBox box = (ITSCodeBox)p.Controls[0];
                if (box.CommandList.Dirty)
                {
                    if (!isRoot)
                        _curFile.EventLists[box.CommandList.AnimationCRC] = box.ParseCodeBox();
                    else
                        _curFighter[(int)box.CommandList._parent.Type].EventLists[box.CommandList.AnimationCRC] = box.ParseCodeBox();
                }
            }
        }

        #region Display related methods
        // Displays the list of commands as plain text in the code editor.
        public void DisplayScript(CommandList list)
        {
            StringBuilder sb = new StringBuilder();
            foreach (Command cmd in list)
                sb.Append(cmd.ToString() + "\n");

            ITSCodeBox box = (ITSCodeBox)tabControl1.SelectedTab.Controls[0];
            box.Text = sb.ToString();
            box.CommandList = list;
        }
        #endregion

        #region Event Handler Methods
        private void ACMDMain_Load(object sender, EventArgs e)
        {
            if (File.Exists(Application.StartupPath + "/Events.cfg"))
            {
                Runtime.GetCommandInfo(Application.StartupPath + "/Events.cfg");

                if (tabControl1.SelectedTab == null)
                    return;
            }
            else
                MessageBox.Show("Could not load Events.cfg");
        }
        private void ACMDMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            Runtime.SaveCommandInfo(Application.StartupPath + "/Events.cfg");
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Rebuild();
            if (isRoot)
            {

                _curFighter.Main.Export(rootPath + "/game.bin");
                _curFighter.GFX.Export(rootPath + "/effect.bin");
                _curFighter.SFX.Export(rootPath + "/sound.bin");
                _curFighter.Expression.Export(rootPath + "/expression.bin");
                _curFighter.MotionTable.Export(rootPath + "/Motion.mtable");
            }
            else
                _curFile.Export(FileName);
        }
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Rebuild();

            if (isRoot)
            {
                FolderSelectDialog dlg = new FolderSelectDialog();
                DialogResult result = dlg.ShowDialog();
                if (result == DialogResult.OK)
                {
                    _curFighter.Main.Export(dlg.SelectedPath + "/game.bin");
                    _curFighter.GFX.Export(dlg.SelectedPath + "/effect.bin");
                    _curFighter.SFX.Export(dlg.SelectedPath + "/sound.bin");
                    _curFighter.Expression.Export(dlg.SelectedPath + "/expression.bin");
                    _curFighter.MotionTable.Export(dlg.SelectedPath + "/Motion.mtable");
                }
                dlg.Dispose();
            }
            else
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.Filter = "ACMD Binary (*.bin)|*.bin|All Files (*.*)|*.*";
                DialogResult result = dlg.ShowDialog();
                if (result == DialogResult.OK)
                    _curFile.Export(dlg.FileName);
                dlg.Dispose();
            }
        }

        private void workspaceToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            FolderSelectDialog dlg = new FolderSelectDialog();
            DialogResult result = dlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                FileName = rootPath = String.Empty;
                _curFile = null;
                cmdListTree.Nodes.Clear();
                tabControl1.TabPages.Clear();
                isRoot = true;

                cmdListTree.ShowLines = cmdListTree.ShowRootLines = true;
                _curFighter = OpenWorkspace(dlg.SelectedPath);
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
                    FileName = rootPath = String.Empty;
                    tabControl1.TabPages.Clear();
                    cmdListTree.Nodes.Clear();
                    isRoot = cmdListTree.ShowLines = cmdListTree.ShowRootLines = false;

                    if ((_curFile = OpenFile(dlg.FileName)) != null)
                    {
                        foreach (CommandList list in _curFile.EventLists.Values)
                            cmdListTree.Nodes.Add(new CommandListNode(String.Format("[{0:X8}]", list.AnimationCRC), _curFile.EventLists[list.AnimationCRC]));

                        FileName = dlg.FileName;
                        this.Text = String.Format("Main Form - {0}", FileName);
                    }
                }
            }
        }

        private void cmdListTree_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            if (cmdListTree.SelectedNode == null)
                return;

            if (cmdListTree.SelectedNode is CommandListNode)
                if (((CommandListNode)cmdListTree.SelectedNode).CommandList.Dirty)
                    cmdListTree.SelectedNode.BackColor = Color.PaleVioletRed;
                else
                    cmdListTree.SelectedNode.BackColor = SystemColors.Window;
        }
        private void cmdListTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (!(cmdListTree.SelectedNode is CommandListNode))
                return;

            CommandListNode w = (CommandListNode)cmdListTree.SelectedNode;

            // Get the name for node, depending on whether we have animation names or not.
            // We'll also use this + node index as a unique identifier.
            string name = string.IsNullOrEmpty(w.AnimationName) ? w.CRC.ToString("X8") : w.AnimationName;

            // If this list is already open, switch to it's tab.
            // else, add a new tab and display it's script.
            if (tabControl1.TabPages.ContainsKey(name + e.Node.Index))
                tabControl1.SelectTab(name + e.Node.Index);
            else
            {
                TabPage p = new TabPage(name) { Name = name + e.Node.Index };
                p.Controls.Add(new ITSCodeBox(((CommandListNode)cmdListTree.SelectedNode).CommandList) { Dock = DockStyle.Fill, WordWrap = false });
                tabControl1.TabPages.Insert(0, p);
                tabControl1.SelectTab(0);
                DisplayScript(((CommandListNode)cmdListTree.SelectedNode).CommandList);
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
            if (!isRoot)
                return;

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Plain Text (.txt) | *.txt";
            DialogResult result = dlg.ShowDialog();
            if (result == DialogResult.OK)
                using (StreamWriter writer = new StreamWriter(dlg.FileName, false, Encoding.UTF8))
                    writer.Write(_curFighter.ToString());
        }
        #endregion

        private void parseAnimationsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            DialogResult result = dlg.ShowDialog();
            try
            {
                if (result == DialogResult.OK)
                {
                    byte[] filebytes = File.ReadAllBytes(dlg.FileName);
                    int count = (int)Util.GetWord(filebytes, 8, workingEndian);

                    for (int i = 0; i < count; i++)
                    {
                        uint off = (uint)Util.GetWord(filebytes, 0x10 + (i * 4), workingEndian);
                        string FileName = Util.GetString(filebytes, off, workingEndian);
                        string AnimName = Regex.Match(FileName, @"(.*)([A-Z])([0-9][0-9])(.*)\.omo").Groups[4].ToString();
                        AnimHashPairs.Add(Crc32.Compute(Encoding.ASCII.GetBytes(AnimName.ToLower())), AnimName);
                    }

                    cmdListTree.BeginUpdate();
                    for (int i = 0; i < cmdListTree.Nodes.Count; i++)
                    {
                        string s = cmdListTree.Nodes[i].Text.Substring(cmdListTree.Nodes[i].Text.IndexOf('[') + 1, 8);
                        uint hash = uint.Parse(s, System.Globalization.NumberStyles.HexNumber);
                        if (AnimHashPairs.ContainsKey(hash))
                            cmdListTree.Nodes[i].Text = AnimHashPairs[hash];
                    }
                    cmdListTree.EndUpdate();
                }
            }
            catch { MessageBox.Show("Could not read .omo files from " + dlg.FileName); }
        }

        #region Tab Control
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
                Rectangle r = tabControl1.GetTabRect(tabControl1.SelectedIndex);
                Rectangle closeButton = new Rectangle(r.Right - 17, r.Top + 4, 11, 9);
                if (closeButton.Contains(e.Location))
                {
                    if (!isRoot)
                        _curFile.EventLists[box.CommandList.AnimationCRC] = box.ParseCodeBox();
                    else
                        _curFighter[(int)box.CommandList._parent.Type].EventLists[box.CommandList.AnimationCRC] = box.ParseCodeBox();

                    this.tabControl1.TabPages.Remove(p);
                }
            }
        }
        #endregion
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

public enum Endianness
{
    Little = 0,
    Big = 1
}

