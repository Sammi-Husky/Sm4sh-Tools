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
using Sm4shCommand.Classes;
using System.Collections;
using Be.Windows.Forms;

namespace Sm4shCommand
{
    public unsafe partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            if (File.Exists(Application.StartupPath + "/Events.cfg") &&
                File.Exists(Application.StartupPath + "/EventSyntax.cfg"))
            {
                Runtime.GetCommandDictionary(Application.StartupPath + "/Events.cfg");
                Runtime.GetCommandSyntax(Application.StartupPath + "/EventSyntax.cfg");

                TooltipDictionary dict = new TooltipDictionary();
                foreach (CommandDefinition cd in Runtime.commandDictionary)
                    if (!String.IsNullOrEmpty(cd.EventDescription))
                        dict.Add(cd.Name, cd.EventDescription);

                CodeView.CommandDictionary = Runtime.commandDictionary;
                CodeView.Tooltip.Dictionary = dict;
            }
            else
                MessageBox.Show("Could not load one or more resources! (Events.cfg, Syntax.cfg)");
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
            VoidPtr addr = source.Address;
            List<uint> ActionFlags = new List<uint>();

            int i = 0;
            while (i * 4 != source.Length)
            {
                if (endian == Endianness.Little)
                    ActionFlags.Add(*(uint*)(addr + (i * 4)));
                else if (endian == Endianness.Big)
                    ActionFlags.Add(*(buint*)(addr + (i * 4)));
                i++;
            }
            MTable m = new MTable(ActionFlags, endian);

            return m;
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

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("0x"))
                {
                    UnknownCommand unkC = new UnknownCommand();
                    unkC._commandInfo = new CommandDefinition() { Identifier = UInt32.Parse(lines[i].Substring(2, 8), System.Globalization.NumberStyles.HexNumber), Name = lines[i].Substring(2) };
                    unkC._owner = _curFile.Actions[_linked._flags];
                    unkC.ident = UInt32.Parse(lines[i].Substring(2, 8), System.Globalization.NumberStyles.HexNumber);
                    unkC._offset = UInt32.Parse(lines[i].Substring(lines[i].IndexOf('@') + 3), System.Globalization.NumberStyles.HexNumber);
                    _curFile.Actions[_linked._flags].Commands.Add(unkC);
                    continue;
                }
                foreach (CommandDefinition e in Runtime.commandDictionary)
                    if (lines[i].StartsWith(e.Name))
                    {
                        string temp = lines[i].Substring(lines[i].IndexOf('(')).Trim(new char[] { '(', ')' });
                        List<string> Params = temp.Replace("0x", "").Split(',').ToList();
                        Command c = new Command();
                        c._owner = _curFile.Actions[_linked._flags];
                        c._commandInfo = e;
                        for (int counter = 0; counter < e.ParamSpecifiers.Count; counter++)
                        {
                            // FIX TO ACCOMIDATE SYNTAX INFO!!!!!
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
            try
            {
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
                file.Dispose();
                source.Close();

                return true;
            }
            catch (Exception x) { MessageBox.Show(x.Message); return false; }
        }
        public void OpenDirectory(string dirPath)
        {
            OpenFile(dirPath + "/game.bin");
            OpenFile(dirPath + "/effect.bin");
            OpenFile(dirPath + "/sound.bin");
            OpenFile(dirPath + "/expression.bin");
            MotionTable = ParseMTable(new DataSource(FileMap.FromFile(dirPath + "/motion.mtable")), workingEndian);

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
                sb.Append(cmd.GetFormated() + "\n");

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
            OpenFileDialog dlg = new OpenFileDialog();
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
            dlg.Dispose();
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
                    //byte[] data = _curFile.GetArray();
                    byte[] data = CharacterFiles[0].Actions[uint.Parse(treeView1.SelectedNode.Text, System.Globalization.NumberStyles.HexNumber)].GetArray();
                    HexView f = new HexView(data);
                    f.Text = "HexView - " + treeView1.SelectedNode.Text;
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
                        f.Text = "HexView - " + treeView1.SelectedNode.Text;
                        f.Show();
                    }
                    else if (treeView1.SelectedNode.Text == "GFX")
                    {

                        byte[] data = CharacterFiles[1].Actions[ident].GetArray();
                        HexView f = new HexView(data);
                        f.Text = "HexView - " + treeView1.SelectedNode.Text;
                        f.Show();
                    }
                    else if (treeView1.SelectedNode.Text == "Sound")
                    {
                        byte[] data = CharacterFiles[2].Actions[ident].GetArray();
                        HexView f = new HexView(data);
                        f.Text = "HexView - " + treeView1.SelectedNode.Text;
                        f.Show();
                    }
                    else if (treeView1.SelectedNode.Text == "Expression")
                    {
                        byte[] data = CharacterFiles[3].Actions[ident].GetArray();
                        HexView f = new HexView(data);
                        f.Text = "HexView - " + treeView1.SelectedNode.Text;
                        f.Show();
                    }
                }
        }
        #endregion

    }

    public unsafe class CommandDefinition
    {
        public uint Identifier;
        public string Name;
        public string EventDescription;


        public List<int> ParamSpecifiers = new List<int>();
    }
    public unsafe class CommandSyntax
    {
        public uint Identifier;
        public List<string> ParamSyntax = new List<string>();
    }

    public unsafe class Command
    {
        public DataSource WorkingSource { get { return _workingSource; } set { _workingSource = value; } }
        private DataSource _workingSource;

        public Command(CommandList Owner, DataSource source)
        {
            _owner = Owner;
            _workingSource = source;
        }
        public Command() { }

        public CommandDefinition _commandInfo;
        public CommandSyntax _syntax;
        public CommandList _owner;


        public List<object> parameters = new List<object>();

        public virtual int CalcSize() { return 0x04 + (_commandInfo.ParamSpecifiers.Count * 4); }
        public void getparams()
        {
            for (int i = 0; i < _commandInfo.ParamSpecifiers.Count; i++)
            {
                if (_commandInfo.ParamSpecifiers[i] == 0)
                    parameters.Add(Util.GetWordUnsafe(0x04 + (WorkingSource.Address + (i * 4)), _owner._endian));
                else if (_commandInfo.ParamSpecifiers[i] == 1)
                    parameters.Add(Util.GetFloatUnsafe(0x04 + (WorkingSource.Address + (i * 4)), _owner._endian));
                else if (_commandInfo.ParamSpecifiers[i] == 2)
                    parameters.Add((decimal)Util.GetWordUnsafe(0x04 + (WorkingSource.Address + (i * 4)), _owner._endian));
            }
        }
        public virtual string GetFormated()
        {
            string Param = "";
            for (int i = 0; i < parameters.Count; i++)
            {
                if(_syntax != null)
                    if (_syntax.ParamSyntax.Count > 0)
                        Param += String.Format("{0}=", _syntax.ParamSyntax[i]);

                if (parameters[i] is int | parameters[i] is bint)
                    Param += String.Format("0x{0:X}{1}", parameters[i], i + 1 != parameters.Count ? ", " : "");
                if (parameters[i] is float | parameters[i] is bfloat)
                    Param += String.Format("{0}{1}", parameters[i], i + 1 != parameters.Count ? ", " : "");
                if (parameters[i] is decimal)
                    Param += String.Format("{0}{1}", parameters[i], i + 1 != parameters.Count ? ", " : "");

            }
            string s = String.Format("{0}({1})", _commandInfo.Name, Param);
            return s;

        }
        public virtual byte[] ToArray()
        {
            byte[] tmp = new byte[CalcSize()];
            Util.SetWord(ref tmp, _commandInfo.Identifier, 0, _owner._endian);
            for (int i = 0; i < _commandInfo.ParamSpecifiers.Count; i++)
            {
                if (_commandInfo.ParamSpecifiers[i] == 0)
                    Util.SetWord(ref tmp, Convert.ToInt32(parameters[i]), (i + 1) * 4, _owner._endian);
                else if (_commandInfo.ParamSpecifiers[i] == 1)
                {
                    double HEX = Convert.ToDouble(parameters[i]);
                    float flt = (float)HEX;
                    byte[] bytes = BitConverter.GetBytes(flt);
                    int dec = BitConverter.ToInt32(bytes, 0);
                    string HexVal = dec.ToString("X");

                    Util.SetWord(ref tmp, Int32.Parse(HexVal, System.Globalization.NumberStyles.HexNumber), (i + 1) * 4, _owner._endian);
                }
            }
            return tmp;
        }
    }
    public unsafe class UnknownCommand : Command
    {
        public uint _offset;
        public uint ident;

        public override int CalcSize() { return 0x04; }
        public override string GetFormated()
        {
            return String.Format("0x{0:X8}", ident);
        }
    }
    public unsafe class ACMDFile : IDisposable
    {

        ~ACMDFile() { Dispose(); }
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
                    Actions.Values[i].Commands.Add(new Command() { _commandInfo = Runtime._endingCommand, _owner = Actions.Values[i] });

            VoidPtr addr = address; // Base address. (0x00)
            Util.SetWordUnsafe(address, 0x444D4341, Endianness.Little); // ACMD     

            //=========================================================================//   
            //                      Rebuilding Header and offsets                       //
            //==========================================================================//
            //
            Util.SetWordUnsafe(address + 0x04, 2, _endian); // Version (2)          //
            Util.SetWordUnsafe(address + 0x08, Actions.Count, _endian);             //
            //
            int count = 0;                                                          //
            foreach (CommandList e in Actions.Values)                               //
                count += e.Commands.Count;                                            //
            //
            Util.SetWordUnsafe(address + 0x0C, count, _endian);                     //
            addr += 0x10;                                                           //
            //
            //=======Write Event List offsets and flags=================//          //                                        //            //
            for (int i = 0, prev = 0; i < Actions.Count; i++)           //          //
            {                                                           //          //
                int dataOffset = 0x10 + (Actions.Count * 8) + prev;     //          //
                Util.SetWordUnsafe(addr, (int)Actions.Keys[i], _endian);//          //
                Util.SetWordUnsafe(addr + 4, dataOffset, _endian);      //          //
                prev += Actions.Values[i].Size;                         //          //
                addr += 8;                                              //          //
            }                                                           //          //
            //=========================================================//           //
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
            using (CommandList _cur = new CommandList(_flags, _offset, _endian))
            {
                Command c = null;
                VoidPtr addr = (WorkingSource.Address + _offset);


                while (Util.GetWordUnsafe(addr, _endian) != Runtime._endingCommand.Identifier)
                {
                    uint ident = (uint)Util.GetWordUnsafe(addr, _endian);
                    CommandDefinition info = null;
                    CommandSyntax syntaxInfo = null;

                    foreach (CommandDefinition e in Runtime.commandDictionary)
                        if (e.Identifier == ident)
                            info = e;

                    foreach (CommandSyntax sy in Runtime.syntaxDictionary)
                        if (sy.Identifier == ident)
                            syntaxInfo = sy;

                    if (info != null)
                    {
                        DataSource src = new DataSource(addr, 0x04 + (info.ParamSpecifiers.Count * 4));
                        c = new Command(_cur, src) { _commandInfo = info, _syntax = syntaxInfo };
                        _cur.Commands.Add(c);
                        addr += c.CalcSize();
                        c.getparams();
                    }
                    else if (info == null)
                    {
                        DataSource src = new DataSource(addr, 0x04);
                        UnknownCommand unkC = new UnknownCommand() { _owner = _cur, _offset = (uint)addr - (uint)WorkingSource.Address, ident = ident, WorkingSource = src };
                        unkC._commandInfo = new CommandDefinition() { Identifier = ident, Name = String.Format("0x{0:X}", ident) };
                        _cur.Commands.Add(unkC);
                        addr += 0x04;
                    }

                }

                if (Util.GetWordUnsafe(addr, _endian) == Runtime._endingCommand.Identifier)
                {
                    CommandDefinition info = null;
                    CommandSyntax syntaxInfo = null;

                    foreach (CommandDefinition e in Runtime.commandDictionary)
                        if (e.Identifier == Runtime._endingCommand.Identifier)
                            info = e;
                    foreach (CommandSyntax sy in Runtime.syntaxDictionary)
                        if (sy.Identifier == Runtime._endingCommand.Identifier)
                            syntaxInfo = sy;

                    DataSource src = new DataSource(addr, 0x04);
                    c = new Command(_cur, src) { _commandInfo = info };
                    _cur.Commands.Add(c);
                    addr += 4;
                }
                _cur.Initialize();
                return _cur;
            }
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

        public void Dispose()
        {
            if (WorkingSource.Map != null)
                WorkingSource.Close();
            if (_replSource.Map != null)
                _replSource.Close();

            GC.SuppressFinalize(this);
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

    public enum Endianness
    {
        Little = 0,
        Big = 1
    }
}
