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
        ACMDFile _workingFile;
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

        // Crawls the code box and applies changes to the linked command list.
        public void ParseCodeBox()
        {
            // Don't bother selectively processing events, just clear and repopulate the whole thing.
            ITSCodeBox box = (ITSCodeBox)tabControl1.SelectedTab.Controls[0];
            string[] lines = box.Lines.Where(x => !string.IsNullOrWhiteSpace(x) && !x.Contains("//")).ToArray();
            _workingFile.EventLists[box.CommandList.AnimationCRC].Clear();


            if (String.IsNullOrEmpty(box.Text))
            {
                _workingFile.EventLists[box.CommandList.AnimationCRC].isEmpty = true;
                return;
            }

            UnknownCommand unkC = null;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("0x"))
                {
                    if (unkC == null)
                        unkC = new UnknownCommand();
                    unkC.data.Add(Int32.Parse(lines[i].Substring(2, 8), System.Globalization.NumberStyles.HexNumber));
                    continue;
                }
                foreach (CommandInfo e in Runtime.commandDictionary)
                    if (lines[i].StartsWith(e.Name))
                    {
                        if (unkC != null)
                        {
                            _workingFile.EventLists[box.CommandList.AnimationCRC].Add(unkC);
                            unkC = null;
                        }
                        string temp = lines[i].Substring(lines[i].IndexOf('(')).Trim(new char[] { '(', ')' });
                        List<string> Params = temp.Replace("0x", "").Split(',').ToList();
                        Command c = new Command(e);
                        for (int counter = 0; counter < e.ParamSpecifiers.Count; counter++)
                        {
                            // parameter - it's syntax keyword(s), and then parse.
                            if (e.ParamSyntax.Count > 0)
                                Params[counter] = Params[counter].Substring(Params[counter].IndexOf('=') + 1);

                            if (e.ParamSpecifiers[counter] == 0)
                                c.parameters.Add(Int32.Parse(Params[counter], System.Globalization.NumberStyles.HexNumber));
                            else if (e.ParamSpecifiers[counter] == 1)
                                c.parameters.Add(float.Parse(Params[counter]));
                            else if (e.ParamSpecifiers[counter] == 2)
                                c.parameters.Add(decimal.Parse(Params[counter]));
                        }
                        _workingFile.EventLists[box.CommandList.AnimationCRC].Add(c);
                    }
            }

        }

        public ACMDFile OpenFile(string Filepath)
        {
            //try
            //{
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
            ACMDFile file = new ACMDFile(source, workingEndian);

            return file;
            //}
            //catch (Exception x) { MessageBox.Show(x.Message); return false; }
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
                    n.Nodes.Add(new NodeWrapper("Main", f[0].EventLists[u]));
                if (f.GFX.EventLists.ContainsKey(u))
                    n.Nodes.Add(new NodeWrapper("GFX", f[1].EventLists[u]));
                if (f.SFX.EventLists.ContainsKey(u))
                    n.Nodes.Add(new NodeWrapper("Sound", f[2].EventLists[u]));
                if (f.Expression.EventLists.ContainsKey(u))
                    n.Nodes.Add(new NodeWrapper("Expression", f[3].EventLists[u]));

                treeView1.Nodes.Add(n);
                counter++;
            }
            isRoot = true;
            rootPath = dirPath;
            this.Text = String.Format("Main Form - {0}", dirPath);
            return f;
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
            ParseCodeBox();
            if (isRoot)
            {
                _curFighter.Main.Export(rootPath + "/game.bin");
                _curFighter.GFX.Export(rootPath + "/effect.bin");
                _curFighter.SFX.Export(rootPath + "/sound.bin");
                _curFighter.Expression.Export(rootPath + "/expression.bin");
                _curFighter.MotionTable.Export(rootPath + "/Motion.mtable");
            }
            else
                _workingFile.Export(FileName);
        }
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (isRoot)
            {
                if (_curFighter.Main.Dirty |
                    _curFighter.GFX.Dirty |
                    _curFighter.SFX.Dirty |
                    _curFighter.Expression.Dirty)
                    ParseCodeBox();

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
                if (_workingFile.Dirty)
                    ParseCodeBox();

                SaveFileDialog dlg = new SaveFileDialog();
                dlg.Filter = "ACMD Binary (*.bin)|*.bin|All Files (*.*)|*.*";
                DialogResult result = dlg.ShowDialog();
                if (result == DialogResult.OK)
                    _workingFile.Export(dlg.FileName);
                dlg.Dispose();
            }
        }

        private void workspaceToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            FolderSelectDialog dlg = new FolderSelectDialog();
            DialogResult result = dlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                FileName =
                rootPath = String.Empty;
                _workingFile = null;
                treeView1.Nodes.Clear();
                isRoot = true;

                treeView1.ShowLines = treeView1.ShowRootLines = true;
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
                    FileName =
                    rootPath = String.Empty;

                    treeView1.Nodes.Clear();
                    isRoot = treeView1.ShowLines = treeView1.ShowRootLines = false;

                    if ((_workingFile = OpenFile(dlg.FileName)) != null)
                    {
                        foreach (CommandList list in _workingFile.EventLists.Values)
                            treeView1.Nodes.Add(new NodeWrapper(String.Format("[{0:X8}]", list.AnimationCRC), _workingFile.EventLists[list.AnimationCRC]));

                        FileName = dlg.FileName;
                        this.Text = String.Format("Main Form - {0}", FileName);
                    }
                }
            }
        }

        private void treeView1_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            //if (tabControl1.TabCount > 0)
            //    ParseCodeBox();

            if (treeView1.SelectedNode == null)
                return;

            if (treeView1.SelectedNode is NodeWrapper)
                if (((NodeWrapper)treeView1.SelectedNode).CommandList.Dirty)
                    treeView1.SelectedNode.BackColor = Color.PaleVioletRed;
                else
                    treeView1.SelectedNode.BackColor = SystemColors.Window;
        }
        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (!(treeView1.SelectedNode is NodeWrapper))
                return;
            NodeWrapper w = (NodeWrapper)treeView1.SelectedNode;

            if (tabControl1.TabPages.ContainsKey(w.CRC.ToString("X8")))
            {
                tabControl1.SelectTab(w.CRC.ToString("X8"));
                tabControl1.SelectedTab.Controls[0].Invalidate();
            }
            else
            {
                TabPage p = new TabPage(w.CRC.ToString("X8")) { Name = w.CRC.ToString("X8") };
                p.Controls.Add(new ITSCodeBox(((NodeWrapper)treeView1.SelectedNode).CommandList) { Dock = DockStyle.Fill, WordWrap = false });
                tabControl1.TabPages.Add(p);
                tabControl1.SelectTab(p);
                DisplayScript(((NodeWrapper)treeView1.SelectedNode).CommandList);
            }
        }

        private void btnHexView_Click(object sender, EventArgs e)
        {
            TreeNode n = treeView1.SelectedNode;
            while (n.Level != 0)
                n = n.Parent;

            uint CRC;
            if (n.Text.Contains('['))
            {
                string id = n.Text.Substring(n.Text.IndexOf('[') + 1).TrimEnd(new char[] { ']' });
                CRC = uint.Parse(id, System.Globalization.NumberStyles.HexNumber);
            }
            else
                CRC = Crc32.Compute(Encoding.ASCII.GetBytes(n.Text.ToLower()));

            byte[] data = ((NodeWrapper)treeView1.SelectedNode).CommandList.ToArray();
            HexView f = new HexView(data);
            f.Text = String.Format("HexView - {0} - ReadOnly", treeView1.SelectedNode.Text);
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

                    treeView1.BeginUpdate();
                    for (int i = 0; i < treeView1.Nodes.Count; i++)
                    {
                        string s = treeView1.Nodes[i].Text.Substring(treeView1.Nodes[i].Text.IndexOf('[') + 1, 8);
                        uint hash = uint.Parse(s, System.Globalization.NumberStyles.HexNumber);
                        if (AnimHashPairs.ContainsKey(hash))
                            treeView1.Nodes[i].Text = AnimHashPairs[hash];
                    }
                    treeView1.EndUpdate();
                }
            }
            catch { MessageBox.Show("Could not read .omo files from " + dlg.FileName); }
        }

        #region Tab Control
        private void tabControl1_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.Graphics.FillRectangle(new SolidBrush(Color.IndianRed), e.Bounds.Right - 18, e.Bounds.Top + 3, e.Graphics.MeasureString("x", Font).Width + 4, Font.Height);
            e.Graphics.DrawRectangle(Pens.Black, e.Bounds.Right - 18, e.Bounds.Top + 3, e.Graphics.MeasureString("X", Font).Width + 3, Font.Height);

            e.Graphics.DrawString("X", new Font(e.Font, FontStyle.Bold), Brushes.Black, e.Bounds.Right - 17, e.Bounds.Top + 3);
            e.Graphics.DrawString(this.tabControl1.TabPages[e.Index].Text, e.Font, Brushes.Black, e.Bounds.Left + 17, e.Bounds.Top + 3);
            e.DrawFocusRectangle();
        }
        private void tabControl1_MouseClick(object sender, MouseEventArgs e)
        {
            TabPage p = tabControl1.SelectedTab;

            ITSCodeBox box = (ITSCodeBox)p.Controls[0];
            Rectangle r = tabControl1.GetTabRect(tabControl1.SelectedIndex);
            Rectangle closeButton = new Rectangle(r.Right - 17, r.Top + 4, 11, 9);
            if (closeButton.Contains(e.Location))
            {
                if (box.CommandList.Dirty)
                {
                    if (MessageBox.Show("Save Changes?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        this.tabControl1.TabPages.Remove(p);

                }
                else
                    this.tabControl1.TabPages.Remove(p);
            }
        }
    }
    #endregion
}

public class NodeWrapper : TreeNode
{
    public NodeWrapper(string Name, CommandList List) { Text = Name; _list = List; _crc = List.AnimationCRC; }
    public NodeWrapper(CommandList List) { _list = List; _crc = List.AnimationCRC; }

    public uint CRC { get { return _crc; } set { _crc = value; } }
    private uint _crc;

    public CommandList CommandList { get { return _list; } set { _list = value; } }
    private CommandList _list;

}

public enum Endianness
{
    Little = 0,
    Big = 1
}

