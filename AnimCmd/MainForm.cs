using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Collections;
using Be.Windows.Forms;

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

        //==================================\\
        // List of motion table identifiers \\
        //==================================\\
        MTable MotionTable = new MTable();

        //===========================================================================\\
        // Main, gfx, sound, and expression event lists. Null if in single file mode.\\
        //===========================================================================\\
        List<ACMDFile> CharacterFiles = new List<ACMDFile>(4);

        // Misc runtime variables.
        bool isRoot = false;
        CommandList _linked;
        string FileName;
        string rootPath;
        Endianness workingEndian;


        // Parses an MTable file. Basically copies all data into a list of uints.
        public MTable ParseMTable(DataSource source, Endianness endian)
        {
            List<uint> ActionFlags = new List<uint>();

            for (int i = 0; i < source.Length; i += 4)
                ActionFlags.Add((uint)Util.GetWordUnsafe((source.Address + i), endian));

            return new MTable(ActionFlags, endian);
        }

        // Crawls the code box and applies changes to the linked command list.
        public void ParseCodeBox()
        {
            // Don't bother selectively processing events, just clear and repopulate the whole thing.
            string[] lines = CodeView.Lines.Where(x => !string.IsNullOrWhiteSpace(x) && !x.Contains("//")).ToArray();
            _curFile.Actions[_linked._flags].Commands.Clear();

            if (String.IsNullOrEmpty(CodeView.Text))
            {
                _curFile.Actions[_linked._flags]._empty = true;
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
                foreach (CommandDefinition e in Runtime.commandDictionary)
                    if (lines[i].StartsWith(e.Name))
                    {
                        if (unkC != null)
                        {
                            _curFile.Actions[_linked._flags].Commands.Add(unkC);
                            unkC = null;
                        }
                        string temp = lines[i].Substring(lines[i].IndexOf('(')).Trim(new char[] { '(', ')' });
                        List<string> Params = temp.Replace("0x", "").Split(',').ToList();
                        Command c = new Command(workingEndian, e);
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
                        _curFile.Actions[_linked._flags].Commands.Add(c);
                    }
            }

        }

        public bool OpenFile(string Filepath)
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
                return false;
            }
            ACMDFile file = new ACMDFile(source, workingEndian);
            if (file.Actions.Count == 0 && isRoot == false)
            {
                MessageBox.Show("There were no actions found");
                return false;
            }

            CharacterFiles.Add(file);

            return true;
            //}
            //catch (Exception x) { MessageBox.Show(x.Message); return false; }
        }
        public void OpenDirectory(string dirPath)
        {
            try
            {
                OpenFile(dirPath + "/game.bin");
                OpenFile(dirPath + "/effect.bin");
                OpenFile(dirPath + "/sound.bin");
                OpenFile(dirPath + "/expression.bin");
                MotionTable = ParseMTable(new DataSource(FileMap.FromFile(dirPath + "/motion.mtable")), workingEndian);
            }
            catch { return; }

            int counter = 0;
            foreach (uint u in MotionTable)
            {
                TreeNode n = new TreeNode(String.Format("{0:X} [{1:X8}]", counter, u));

                if (CharacterFiles[0].Actions.ContainsKey(u))
                    n.Nodes.Add("Main");
                if (CharacterFiles[1].Actions.ContainsKey(u))
                    n.Nodes.Add("GFX");
                if (CharacterFiles[2].Actions.ContainsKey(u))
                    n.Nodes.Add("Sound");
                if (CharacterFiles[3].Actions.ContainsKey(u))
                    n.Nodes.Add("Expression");

                treeView1.Nodes.Add(n);
                counter++;
            }
            isRoot = true;
            rootPath = dirPath;
            this.Text = String.Format("Main Form - {0}", dirPath);
        }


        #region Display related methods
        // Displays the list of commands as plain text in the code editor.
        public void DisplayScript(CommandList s)
        {
            StringBuilder sb = new StringBuilder();
            foreach (Command cmd in s.Commands)
                sb.Append(cmd.ToString() + "\n");

            CodeView.Text = sb.ToString();
            _linked = s;
        }
        #endregion

        #region Event Handler Methods
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ParseCodeBox();
            if (isRoot)
            {
                CharacterFiles[0].Export(rootPath + "/game.bin");
                CharacterFiles[1].Export(rootPath + "/effect.bin");
                CharacterFiles[2].Export(rootPath + "/sound.bin");
                CharacterFiles[3].Export(rootPath + "/expression.bin");
                MotionTable.Export(rootPath + "/Motion.mtable");
            }
            else
                _curFile.Export(FileName);
        }
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ParseCodeBox();
            if (isRoot)
            {
                FolderSelectDialog dlg = new FolderSelectDialog();
                DialogResult result = dlg.ShowDialog();
                if (result == DialogResult.OK)
                {
                    CharacterFiles[0].Export(dlg.SelectedPath + "/game.bin");
                    CharacterFiles[1].Export(dlg.SelectedPath + "/effect.bin");
                    CharacterFiles[2].Export(dlg.SelectedPath + "/sound.bin");
                    CharacterFiles[3].Export(dlg.SelectedPath + "/expression.bin");
                    MotionTable.Export(dlg.SelectedPath + "/Motion.mtable");
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

        private void openFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "ACMD Binary (*.bin)|*.bin| All Files (*.*)|*.*";
                DialogResult result = dlg.ShowDialog();
                if (result == DialogResult.OK)
                {
                    CodeView.Text = FileName =
                    rootPath = String.Empty;
                    _linked = null;
                    CharacterFiles.Clear(); MotionTable.Clear();
                    treeView1.Nodes.Clear();
                    isRoot = treeView1.ShowLines = treeView1.ShowRootLines = false;

                    if (OpenFile(dlg.FileName) && CharacterFiles[0] != null)
                    {
                        foreach (CommandList list in CharacterFiles[0].Actions.Values)
                            treeView1.Nodes.Add(String.Format("{0:X8}", list._flags));

                        FileName = dlg.FileName;
                        this.Text = String.Format("Main Form - {0}", FileName);
                    }
                }
            }
        }
        private void openDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderSelectDialog dlg = new FolderSelectDialog();
            DialogResult result = dlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                CodeView.Text = FileName =
                rootPath = String.Empty;
                _linked = null; _curFile = null;
                CharacterFiles.Clear(); MotionTable.Clear();
                treeView1.Nodes.Clear();
                isRoot = true;

                treeView1.ShowLines = treeView1.ShowRootLines = true;
                OpenDirectory(dlg.SelectedPath);
            }
            dlg.Dispose();
        }

        private void eventLibraryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EventLibrary dlg = new EventLibrary();
            dlg.Show();
        }
        private void treeView1_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            if (_linked != null)
                ParseCodeBox();

            if (treeView1.SelectedNode == null)
                return;

            if (!isRoot)
            {
                if (treeView1.SelectedNode.Level == 0)
                {
                    uint Ident = uint.Parse(treeView1.SelectedNode.Text, System.Globalization.NumberStyles.HexNumber);
                    if (_curFile.Actions[Ident].Dirty)
                        treeView1.Nodes[treeView1.Nodes.IndexOf(treeView1.SelectedNode)].BackColor = Color.PaleVioletRed;
                    else
                        treeView1.Nodes[treeView1.Nodes.IndexOf(treeView1.SelectedNode)].BackColor = SystemColors.Window;
                }
            }
            else if (isRoot)
                if (treeView1.SelectedNode.Level == 1)
                {
                    TreeNode n = treeView1.SelectedNode;
                    while (n.Level != 0)
                        n = n.Parent;
                    uint ident = MotionTable[treeView1.Nodes.IndexOf(n)];

                    if (_curFile.Actions[ident].Dirty)
                        treeView1.SelectedNode.BackColor = Color.PaleVioletRed;
                    else
                        treeView1.SelectedNode.BackColor = SystemColors.Window;
                }
        }
        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (!isRoot)
            {
                if (e.Node.Level == 0)
                {
                    uint Ident = uint.Parse(e.Node.Text, System.Globalization.NumberStyles.HexNumber);
                    DisplayScript(CharacterFiles[0].Actions[Ident]);
                    _curFile = CharacterFiles[0];
                }
            }
            else if (isRoot)
                if (e.Node.Level == 1)
                {
                    TreeNode n = e.Node;
                    while (n.Level != 0)
                        n = n.Parent;
                    uint ident = MotionTable[treeView1.Nodes.IndexOf(n)];

                    if (e.Node.Text == "Main")
                    {
                        DisplayScript(CharacterFiles[0].Actions[ident]);
                        _curFile = CharacterFiles[0];
                    }
                    else if (e.Node.Text == "GFX")
                    {

                        DisplayScript(CharacterFiles[1].Actions[ident]);
                        _curFile = CharacterFiles[1];
                    }
                    else if (e.Node.Text == "Sound")
                    {
                        DisplayScript(CharacterFiles[2].Actions[ident]);
                        _curFile = CharacterFiles[2];
                    }
                    else if (e.Node.Text == "Expression")
                    {
                        DisplayScript(CharacterFiles[3].Actions[ident]);
                        _curFile = CharacterFiles[3];
                    }
                }
        }
        private void btnHexView_Click(object sender, EventArgs e)
        {

            if (!isRoot)
            {
                if (treeView1.SelectedNode.Level == 0)
                {
                    uint ident = uint.Parse(treeView1.SelectedNode.Text, System.Globalization.NumberStyles.HexNumber);
                    byte[] data = CharacterFiles[0].Actions[ident].GetArray();
                    HexView f = new HexView(data);
                    f.Text = String.Format("HexView - {0} - ReadOnly", treeView1.SelectedNode.Text);
                    f.Show();

                }
            }
            else if (isRoot)
                if (treeView1.SelectedNode.Level == 1)
                {
                    TreeNode n = treeView1.SelectedNode;
                    while (n.Level != 0)
                        n = n.Parent;
                    uint ident = MotionTable[treeView1.Nodes.IndexOf(n)];

                    if (treeView1.SelectedNode.Text == "Main")
                    {
                        byte[] data = CharacterFiles[0].Actions[ident].GetArray();
                        HexView f = new HexView(data);
                        f.Text = String.Format("HexView - {0} - ReadOnly", treeView1.SelectedNode.Text);
                        f.Show();
                    }
                    else if (treeView1.SelectedNode.Text == "GFX")
                    {

                        byte[] data = CharacterFiles[1].Actions[ident].GetArray();
                        HexView f = new HexView(data);
                        f.Text = String.Format("HexView - {0} - ReadOnly", treeView1.SelectedNode.Text);
                        f.Show();
                    }
                    else if (treeView1.SelectedNode.Text == "Sound")
                    {
                        byte[] data = CharacterFiles[2].Actions[ident].GetArray();
                        HexView f = new HexView(data);
                        f.Text = String.Format("HexView - {0} - ReadOnly", treeView1.SelectedNode.Text);
                        f.Show();
                    }
                    else if (treeView1.SelectedNode.Text == "Expression")
                    {
                        byte[] data = CharacterFiles[3].Actions[ident].GetArray();
                        HexView f = new HexView(data);
                        f.Text = String.Format("HexView - {0} - ReadOnly", treeView1.SelectedNode.Text);
                        f.Show();
                    }
                }
        }
        #endregion

        private void dumpAsTextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!isRoot)
                return;

            StringBuilder sb = new StringBuilder();

            foreach (uint u in MotionTable)
            {
                sb.Append(String.Format("\n\n{0:X}: [{1:X8}]", MotionTable.IndexOf(u), u));
                CommandList c1 = null, c2 = null,
                            c3 = null, c4 = null;

                if (CharacterFiles[0].Actions.ContainsKey(u))
                    c1 = CharacterFiles[0].Actions[u];
                if (CharacterFiles[1].Actions.ContainsKey(u))
                    c2 = CharacterFiles[1].Actions[u];
                if (CharacterFiles[2].Actions.ContainsKey(u))
                    c3 = CharacterFiles[2].Actions[u];
                if (CharacterFiles[3].Actions.ContainsKey(u))
                    c4 = CharacterFiles[3].Actions[u];

                sb.Append("\n\tGame:{");
                if (c1 != null)
                    foreach (Command cmd in c1.Commands)
                        sb.Append(String.Format("\n\t\t{0}", cmd.ToString()));
                else
                    sb.Append("\n\t\tEmpty");
                sb.Append("\n\t}");

                sb.Append("\n\tGFX:{");
                if (c2 != null)
                    foreach (Command cmd in c2.Commands)
                        sb.Append(String.Format("\n\t\t{0}", cmd.ToString()));
                else
                    sb.Append("\n\t\tEmpty");
                sb.Append("\n\t}");

                sb.Append("\n\tSFX:{");
                if (c3 != null)
                    foreach (Command cmd in c3.Commands)
                        sb.Append(String.Format("\n\t\t{0}", cmd.ToString()));
                else
                    sb.Append("\n\t\tEmpty");
                sb.Append("\n\t}");

                sb.Append("\n\tExpression:{");
                if (c4 != null)
                    foreach (Command cmd in c4.Commands)
                        sb.Append(String.Format("\n\t\t{0}", cmd.ToString()));
                else
                    sb.Append("\n\t\tEmpty");
                sb.Append("\n\t}");
            }
            sb.Append("\n\t End. -Dumped via Sm4shCommand-");
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Plain Text (.txt) | *.txt";
            DialogResult result = dlg.ShowDialog();
            if (result == DialogResult.OK)
                using (StreamWriter writer = new StreamWriter(dlg.FileName, false, Encoding.UTF8))
                    writer.Write(sb.ToString());
        }

        private void ACMDMain_Load(object sender, EventArgs e)
        {
            if (File.Exists(Application.StartupPath + "/Events.cfg"))
            {
                Runtime.GetCommandInfo(Application.StartupPath + "/Events.cfg");

                TooltipDictionary dict = new TooltipDictionary();
                foreach (CommandDefinition cd in Runtime.commandDictionary)
                    if (!String.IsNullOrEmpty(cd.EventDescription))
                        dict.Add(cd.Name, cd.EventDescription);

                CodeView.CommandDictionary = Runtime.commandDictionary;
                CodeView.Tooltip.Dictionary = dict;
            }
            else
                MessageBox.Show("Could not load Events.cfg");
        }

        private void ACMDMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            Runtime.SaveCommandInfo(Application.StartupPath + "/Events.cfg");
        }
    }

    public unsafe class CommandDefinition
    {

        public uint Identifier;
        public string Name;
        public string EventDescription;


        public List<int> ParamSpecifiers = new List<int>();
        public List<string> ParamSyntax = new List<string>();

    }


    public unsafe class Command
    {
        public Command(Endianness _endian, CommandDefinition info)
        {
            endian = _endian;
            _commandInfo = info;
        }
        public Command() { }

        public CommandDefinition _commandInfo;
        public Endianness endian;


        public List<object> parameters = new List<object>();

        public virtual int CalcSize() { return 0x04 + (_commandInfo.ParamSpecifiers.Count * 4); }

        public override string ToString()
        {
            string Param = "";
            for (int i = 0; i < parameters.Count; i++)
            {

                if (_commandInfo.ParamSyntax.Count > 0)
                    Param += String.Format("{0}=", _commandInfo.ParamSyntax[i]);

                if (parameters[i] is int | parameters[i] is bint)
                    Param += String.Format("0x{0:X}{1}", parameters[i], i + 1 != parameters.Count ? ", " : "");
                if (parameters[i] is float | parameters[i] is bfloat)
                    Param += String.Format("{0}{1}", parameters[i], i + 1 != parameters.Count ? ", " : "");
                if (parameters[i] is decimal)
                    Param += String.Format("{0}{1}", parameters[i], i + 1 != parameters.Count ? ", " : "");

            }
            return String.Format("{0}({1})", _commandInfo.Name, Param);

        }
        public virtual byte[] ToArray()
        {
            byte[] tmp = new byte[CalcSize()];
            Util.SetWord(ref tmp, _commandInfo.Identifier, 0, endian);
            for (int i = 0; i < _commandInfo.ParamSpecifiers.Count; i++)
            {
                if (_commandInfo.ParamSpecifiers[i] == 0)
                    Util.SetWord(ref tmp, Convert.ToInt32(parameters[i]), (i + 1) * 4, endian);
                else if (_commandInfo.ParamSpecifiers[i] == 1)
                {
                    double HEX = Convert.ToDouble(parameters[i]);
                    float flt = (float)HEX;
                    byte[] bytes = BitConverter.GetBytes(flt);
                    int dec = BitConverter.ToInt32(bytes, 0);
                    string HexVal = dec.ToString("X");

                    Util.SetWord(ref tmp, Int32.Parse(HexVal, System.Globalization.NumberStyles.HexNumber), (i + 1) * 4, endian);
                }
            }
            return tmp;
        }
    }
    public unsafe class UnknownCommand : Command
    {
        public List<int> data = new List<int>();

        public override int CalcSize() { return data.Count * 4; }
        public override string ToString()
        {
            string s1 = "";
            for (int i = 0; i < data.Count; i++)
                s1 += String.Format("0x{0:X8}{1}", data[i], i + 1 != data.Count ? "\n" : "");
            return s1;
        }
        public override byte[] ToArray()
        {
            return data.SelectMany(i => BitConverter.GetBytes(i)).ToArray();
        }
    }

    public unsafe class ACMDFile
    {
        public VoidPtr Header { get { return _replSource != DataSource.Empty ? _replSource.Address : WorkingSource.Address; } }
        public DataSource WorkingSource, _replSource;

        public int CommandCount { get { return _commandCount; } set { _commandCount = value; } }
        private int _commandCount;

        public int ActionCount { get { return _actionCount; } set { _actionCount = value; } }
        private int _actionCount;

        public Endianness _endian;

        public SortedList<uint, CommandList> Actions = new SortedList<uint, CommandList>();
        public int Size
        {
            get
            {
                int size = 0x10 + (Actions.Count * 8);
                foreach (CommandList e in Actions.Values)
                    size += e.Size;
                return size;
            }
        }
        public bool Dirty
        {
            get
            {
                foreach (CommandList cl in Actions.Values)
                    if (cl.Dirty)
                        return true;
                return false;
            }
        }

        public ACMDFile(DataSource source, Endianness endian)
        {
            WorkingSource = source;
            _endian = endian;

            _actionCount = Util.GetWordUnsafe(source.Address + 0x08, endian);
            _commandCount = Util.GetWordUnsafe(source.Address + 0x0C, endian);

            Parse();
        }

        public void Parse()
        {
            for (int i = 0; i < _actionCount; i++)
            {
                uint _flags = 0;
                int _offset = 0;


                _flags = (uint)Util.GetWordUnsafe(WorkingSource.Address + 0x10 + (i * 8), _endian);
                _offset = Util.GetWordUnsafe((WorkingSource.Address + 0x10 + (i * 8)) + 0x04, _endian);


                Actions.Add(_flags, ParseEventList(_flags, _offset));
            }
        }
        public void Rebuild()
        {
            FileMap temp = FileMap.FromTempFile(Size);

            // Write changes to the new filemap.
            OnRebuild(temp.Address, temp.Length);

            // Close backing source.
            _replSource.Close();
            // set backing source to new source from temp map.
            _replSource = new DataSource(temp.Address, temp.Length);
            // Set backing source's map to the temp map.
            _replSource.Map = temp;
        }
        public void OnRebuild(VoidPtr address, int length)
        {
            //  Make sure empty event lists at least contain the ending specifier,
            //  otherwise the list will bleed over and read the next one.
            for (int i = 0; i < Actions.Count; i++)
                if (Actions.Values[i]._empty)
                    Actions.Values[i].Commands.Add(new Command() { _commandInfo = Runtime._endingCommand });

            VoidPtr addr = address; // Base address. (0x00)
            Util.SetWordUnsafe(address, 0x444D4341, Endianness.Little); // ACMD     

            //=========================================================================//   
            //                      Rebuilding Header and offsets                       //
            //==========================================================================//
            //
            Util.SetWordUnsafe(address + 0x04, 2, _endian); // Version (2)              //
            Util.SetWordUnsafe(address + 0x08, Actions.Count, _endian);                 //
                                                                                        //
            int count = 0;                                                              //
            foreach (CommandList e in Actions.Values)                                   //
                count += e.Commands.Count;                                              //
                                                                                        //
            Util.SetWordUnsafe(address + 0x0C, count, _endian);                         //
            addr += 0x10;                                                               //
                                                                                        //
                                                                                        //=======Write Event List offsets and flags=================//              //                                        //            //
            for (int i = 0, prev = 0; i < Actions.Count; i++)           //              //
            {                                                           //              //
                int dataOffset = 0x10 + (Actions.Count * 8) + prev;     //              //
                Util.SetWordUnsafe(addr, (int)Actions.Keys[i], _endian);//              //
                Util.SetWordUnsafe(addr + 4, dataOffset, _endian);      //              //
                prev += Actions.Values[i].Size;                         //              //
                addr += 8;                                              //              //
            }                                                           //              //
            //=========================================================//               //
            //                                                                         //
            //========================================================================//

            // Write event lists at final address.
            foreach (CommandList e in Actions.Values)
            {
                e.OnRebuild(addr, e.Size);
                addr += e.Size;
            }
        }

        private CommandList ParseEventList(uint _flags, int _offset)
        {
            CommandList _cur = new CommandList(_flags, _offset, _endian);

            Command c = null;
            UnknownCommand unkC = null;

            VoidPtr addr = (WorkingSource.Address + _offset);

            // Loop through Event List.
            while (Util.GetWordUnsafe(addr, _endian) != Runtime._endingCommand.Identifier)
            {
                // Try to get command definition
                uint ident = (uint)Util.GetWordUnsafe(addr, _endian);
                CommandDefinition info = null;
                foreach (CommandDefinition e in Runtime.commandDictionary)
                    if (e.Identifier == ident) { info = e; break; }

                // If a command definition exists, use that info to deserialize.
                if (info != null)
                {
                    // If previous commands were unknown, add them here.
                    if (unkC != null)
                    {
                        _cur.Commands.Add(unkC);
                        unkC = null;
                    }

                    // Get command parameters and add the command to the event list.
                    c = new Command(_endian, info);
                    for (int i = 0; i < info.ParamSpecifiers.Count; i++)
                    {
                        if (info.ParamSpecifiers[i] == 0)
                            c.parameters.Add(Util.GetWordUnsafe(0x04 + (addr + (i * 4)), _endian));
                        else if (info.ParamSpecifiers[i] == 1)
                            c.parameters.Add(Util.GetFloatUnsafe(0x04 + (addr + (i * 4)), _endian));
                        else if (info.ParamSpecifiers[i] == 2)
                            c.parameters.Add((decimal)Util.GetWordUnsafe(0x04 + (addr + (i * 4)), _endian));
                    }

                    _cur.Commands.Add(c);
                    addr += c.CalcSize();
                }
                // If there is no command definition, this is unknown data.
                // Add the current word to the unk command and continue adding
                // until we hit a known command
                else if (info == null)
                {
                    if (unkC == null)
                        unkC = new UnknownCommand();
                    unkC.data.Add(Util.GetWordUnsafe(addr, _endian));
                    addr += 0x04;
                }
            }

            // If we hit a script_end command, add it to the the Event List and terminate looping.
            if (Util.GetWordUnsafe(addr, _endian) == Runtime._endingCommand.Identifier)
            {
                CommandDefinition info = null;

                foreach (CommandDefinition e in Runtime.commandDictionary)
                    if (e.Identifier == Runtime._endingCommand.Identifier)
                    { info = e; break; }

                c = new Command(_endian, info);
                _cur.Commands.Add(c);
                addr += 4;
            }
            _cur.Initialize();
            return _cur;
        }

        public void Export(string path)
        {
            Rebuild();
            if (_replSource != DataSource.Empty)
            {
                WorkingSource.Close();
                WorkingSource = _replSource;
                _replSource = DataSource.Empty;


                DataSource src = WorkingSource;
                byte[] tmp = new byte[Size];
                for (int i = 0; i < tmp.Length; i++)
                    tmp[i] = *(byte*)(src.Address + i);
                File.WriteAllBytes(path, tmp);
            }
            else
                MessageBox.Show("No changes have been made.");
        }
        public byte[] GetArray()
        {
            DataSource src = WorkingSource;
            byte[] tmp = new byte[Size];
            for (int i = 0; i < tmp.Length; i++)
                tmp[i] = *(byte*)(src.Address + i);
            return tmp;
        }
    }
    public unsafe class MTable : IEnumerable
    {
        public Endianness _endian;
        private List<uint> _baseList = new List<uint>();
        public uint this[int i]
        {
            get { return _baseList[i]; }
            set { _baseList[i] = value; }
        }
        public MTable(List<uint> ActionFlags, Endianness endian)
        {
            _endian = endian;
            _baseList = ActionFlags;
        }
        public MTable() { }

        public void Export(string path)
        {
            byte[] mtable = new byte[_baseList.Count * 4];
            int p = 0;
            foreach (uint val in _baseList)
            {
                byte[] tmp = BitConverter.GetBytes(val);
                if (_endian == Endianness.Big)
                    Array.Reverse(tmp);

                for (int i = 0; i < 4; i++)
                    mtable[p + i] = tmp[i];
                p += 4;
            }

            File.WriteAllBytes(path, mtable);
        }
        public void Clear()
        {
            _baseList = new List<uint>();
        }
        public void Add(uint var)
        {
            _baseList.Add(var);
        }
        public void Remove(uint var)
        {
            _baseList.Remove(var);
        }
        public void Remove(int index)
        {
            _baseList.RemoveAt(index);
        }
        public int IndexOf(uint var)
        {
            return _baseList.IndexOf(var);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }
        public MTableEnumerator GetEnumerator()
        {
            return new MTableEnumerator(_baseList.ToArray());
        }
        public class MTableEnumerator : IEnumerator
        {
            public uint[] _data;
            int position = -1;
            public MTableEnumerator(uint[] data)
            {
                _data = data;
            }

            public bool MoveNext()
            {
                position++;
                return (position < _data.Length);
            }

            public void Reset()
            {
                position = -1;
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            public uint Current
            {
                get
                {
                    try
                    {
                        return _data[position];
                    }
                    catch (IndexOutOfRangeException)
                    {
                        throw new InvalidOperationException();
                    }
                }
            }
        }

    }

    public unsafe class CommandList
    {
        public Endianness _endian;
        private byte[] _data;

        public CommandList(uint flags, int offset, Endianness endian)
        {
            _flags = flags;
            _offset = offset;
            _endian = endian;
        }
        public CommandList() { }

        public int Size
        {
            get
            {
                int size = 0;
                foreach (Command e in Commands)
                    size += e.CalcSize();
                return size;
            }
        }

        public bool _empty;
        public bool Dirty
        {
            get
            {
                byte[] data = GetArray();
                if (data.Length != _data.Length)
                    return true;

                for (int i = 0; i < _data.Length; i++)
                    if (data[i] != _data[i])
                        return true;

                return false;
            }
        }


        public uint _flags;
        public int _offset;

        public void Initialize()
        {
            _data = GetArray();
        }
        public void OnRebuild(VoidPtr address, int size)
        {
            VoidPtr addr = address;
            for (int x = 0; x < Commands.Count; x++)
            {
                byte[] a = Commands[x].ToArray();
                byte* tmp = stackalloc byte[a.Length];
                for (int i = 0; i < a.Length; i++)
                    tmp[i] = a[i];

                Win32.MoveMemory(addr, tmp, (uint)a.Length);
                addr += Commands[x].CalcSize();
            }
        }
        public void Export(string path)
        {
            byte[] file = GetArray();
            File.WriteAllBytes(path, file);
        }
        public byte[] GetArray()
        {
            byte[] file = new byte[Size];

            int i = 0;
            foreach (Command c in Commands)
            {
                byte[] command = c.ToArray();
                for (int x = 0; x < command.Length; x++, i++)
                    file[i] = command[x];
            }

            return file;

        }

        public List<Command> Commands = new List<Command>();
    }

    public enum Endianness
    {
        Little = 0,
        Big = 1
    }
}
