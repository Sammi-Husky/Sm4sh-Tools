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
using Sm4shCommand.Structs;
using Sm4shCommand.Classes;

namespace Sm4shCommand
{
    public unsafe partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            if (File.Exists(Application.StartupPath + "/Events.txt"))
                CommandDictionary = Runtime.GetCommandDictionary(Application.StartupPath + "/Events.txt");

            eDictionary dict = new eDictionary();
            foreach (CommandDefinition cd in CommandDictionary)
                if (!String.IsNullOrEmpty(cd.EventDescription))
                    dict.Add(cd.Name, cd.EventDescription);
            CodeView.CommandDictionary = CommandDictionary;
            CodeView.Tooltip.Dictionary = dict;
        }

        //================================================================================\\
        // List of action event lists in single file mode. Null if in full directory mode.\\
        //================================================================================\\
        ACMDFile _curFile;

        //==================================\\
        // List of motion table identifiers \\
        //==================================\\
        List<uint> Mtable = new List<uint>();

        //===========================================================================\\
        // Main, gfx, sound, and expression event lists. Null if in single file mode.\\
        //===========================================================================\\
        List<ACMDFile> CharacterFiles = new List<ACMDFile>();

        // Misc runtime variables.
        bool isRoot = false;
        CommandList _linked;
        public List<CommandDefinition> CommandDictionary = new List<CommandDefinition>();
        string FileName;
        string rootPath;

        #region Parsing
        // Parses an MTable file. Basically copies all data into a list of uints.
        public List<uint> ParseMTable(DataSource source, Endianness endian)
        {
            VoidPtr addr = source.Address;
            List<uint> ActionFlags = new List<uint>();
            int i = 0;
            while (i * 4 != source.Length)
            {
                if(endian == Endianness.Little)
                    ActionFlags.Add(*(uint*)(addr + (i * 4)));
                else if(endian == Endianness.Big)
                    ActionFlags.Add(*(buint*)(addr + (i * 4)));
                i++;
            }
            return ActionFlags;
        }

        // Crawls the code box and applies changes to the linked command list.
        public void ParseCodeBox()
        {
            // Don't bother selectively processing events, just clear and repopulate the whole thing.
            string[] lines = CodeView.Lines.Where(x => !string.IsNullOrWhiteSpace(x) && !x.Contains("//")).ToArray();
            _curFile.Actions[_linked._flags].Events.Clear();
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("0x"))
                {
                    UnknownCommand unkC = new UnknownCommand();
                    unkC._commandInfo = new CommandDefinition() { Identifier = UInt32.Parse(lines[i].Substring(2, 8), System.Globalization.NumberStyles.HexNumber), Name = lines[i].Substring(2) };
                    unkC._owner = _curFile.Actions[_linked._flags];
                    unkC._index = i;
                    unkC.ident = UInt32.Parse(lines[i].Substring(2, 8), System.Globalization.NumberStyles.HexNumber);
                    unkC._offset = UInt32.Parse(lines[i].Substring(lines[i].IndexOf('@') + 3), System.Globalization.NumberStyles.HexNumber);
                    _curFile.Actions[_linked._flags].Events.Add(unkC);
                    continue;
                }
                foreach (CommandDefinition e in CommandDictionary)
                    if (lines[i].StartsWith(e.Name))
                    {
                        string temp = lines[i].Substring(lines[i].IndexOf('(')).Trim(new char[] { '(', ')' });
                        List<string> Params = temp.Replace("0x", "").Split(',').ToList();
                        Command c = new Command();
                        c._owner = _curFile.Actions[_linked._flags];
                        c._commandInfo = e;
                        for (int counter = 0; counter < e.ParamSpecifiers.Count; counter++)
                        {
                            if (e.ParamSpecifiers[counter] == 1)
                                c.parameters.Add(float.Parse(Params[counter]));
                            else if (e.ParamSpecifiers[counter] == 0)
                                c.parameters.Add(Int32.Parse(Params[counter], System.Globalization.NumberStyles.HexNumber));
                        }
                        _curFile.Actions[_linked._flags].Events.Add(c);
                    }
            }
        }

        public void OpenFile(Endianness endian)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "ACMD Binary (*.bin)|*.bin| All Files (*.*)|*.*";
            DialogResult result = dlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                CodeView.Text = FileName =
                rootPath = String.Empty;
                _linked = null; _curFile = null;
                CharacterFiles.Clear(); Mtable.Clear();
                treeView1.Nodes.Clear();
                isRoot = false;
                treeView1.ShowLines = treeView1.ShowRootLines = false;

                try
                {
                    _curFile = new ACMDFile(new DataSource(FileMap.FromFile(dlg.FileName)), endian);

                    foreach (CommandList list in _curFile.Actions.Values)
                        treeView1.Nodes.Add(String.Format("{0:X8}", list._flags));

                    if (_curFile.Actions.Count == 0)
                        MessageBox.Show("There were no actions found");
                    else
                        isRoot = false;
                    FileName = dlg.FileName;
                    this.Text = String.Format("Main Form - {0}", FileName);
                }
                catch (Exception x) { MessageBox.Show(x.Message); }
            }
        }
        public void OpenDirectory(Endianness endian)
        {
            FolderSelectDialog dlg = new FolderSelectDialog();
            DialogResult result = dlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                CodeView.Text = FileName =
                rootPath = String.Empty;
                _linked = null; _curFile = null;
                CharacterFiles.Clear(); Mtable.Clear();
                treeView1.Nodes.Clear();
                isRoot = false;

                treeView1.ShowLines = treeView1.ShowRootLines = true;

                Mtable = ParseMTable(new DataSource(FileMap.FromFile(dlg.SelectedPath + "/motion.mtable")), endian);
                CharacterFiles.Add(new ACMDFile(new DataSource(FileMap.FromFile(dlg.SelectedPath + "/game.bin")), endian));
                CharacterFiles.Add(new ACMDFile(new DataSource(FileMap.FromFile(dlg.SelectedPath + "/effect.bin")), endian));
                CharacterFiles.Add(new ACMDFile(new DataSource(FileMap.FromFile(dlg.SelectedPath + "/sound.bin")), endian));
                CharacterFiles.Add(new ACMDFile(new DataSource(FileMap.FromFile(dlg.SelectedPath + "/expression.bin")), endian));

                int counter = 0;
                foreach (uint u in Mtable)
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
                rootPath = dlg.SelectedPath;
                this.Text = String.Format("Main Form - {0}", dlg.SelectedPath);
            }
        }
        #endregion

        #region Display related methods
        // Displays the list of commands as plain text in the code editor.
        public void DisplayScript(CommandList s)
        {
            if (_linked != null)
                ParseCodeBox();

            StringBuilder sb = new StringBuilder();
            sb.Append(String.Format("//=======================================\\\\\n" +
                                    "//\t\t0x{0:X8}\t\t           \\\\\n" +
                                    "//=======================================\\\\\n",
                                                                            s._flags));
            foreach (Command cmd in s.Events)
                sb.Append(cmd.GetFormated() + "\n");

            CodeView.Text = sb.ToString();
            CodeView.ProcessAllLines();
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
                }
            }
            else
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.Filter = "ACMD Binary (*.bin)|*.bin|All Files (*.*)|*.*";
                DialogResult result = dlg.ShowDialog();
                if (result == DialogResult.OK)
                    _curFile.Export(dlg.FileName);
            }
        }
        private void directoryToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
        private void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (!isRoot)
            {
                if (e.Node.Level == 0)
                {
                    uint Ident = uint.Parse(e.Node.Text, System.Globalization.NumberStyles.HexNumber);
                    DisplayScript(_curFile.Actions[Ident]);
                }
            }
            else if (isRoot)
                if (e.Node.Level == 1)
                {
                    TreeNode n = e.Node;
                    while (n.Level != 0)
                        n = n.Parent;
                    uint ident = Mtable[treeView1.Nodes.IndexOf(n)];

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
        #endregion

        private void fileToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            OpenFile(Endianness.Little);
        }
        private void directoryToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            OpenDirectory(Endianness.Little);
        }

        private void fileToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            OpenFile(Endianness.Big);
        }
        private void directoryToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            OpenDirectory(Endianness.Big);
        }
    }

    public unsafe class CommandDefinition
    {
        public uint Identifier;
        public string Name;
        public string EventDescription;


        public List<int> ParamSpecifiers = new List<int>();
    }

    public unsafe class Command
    {
        public DataSource WorkingSource { get { return _workingSource; } set { _workingSource = value; } }
        private DataSource _workingSource;

        public Command(CommandList Owner, int index, DataSource source)
        {
            _owner = Owner;
            _index = index;
            _workingSource = source;
        }
        public Command() { }

        public CommandDefinition _commandInfo;
        public CommandList _owner;
        public int _index;

        public List<object> parameters = new List<object>();

        public virtual int CalcSize() { return 0x04 + (_commandInfo.ParamSpecifiers.Count * 4); }
        public void getparams()
        {
            for (int i = 0; i < _commandInfo.ParamSpecifiers.Count; i++)
            {
                if (_owner._endian == Endianness.Little)
                {
                    if (_commandInfo.ParamSpecifiers[i] == 0)
                        parameters.Add(*(int*)(0x04 + (WorkingSource.Address + (i * 4))));
                    else if (_commandInfo.ParamSpecifiers[i] == 1)
                        parameters.Add(*(float*)(0x04 + (WorkingSource.Address + (i * 4))));
                }
                else if (_owner._endian == Endianness.Big)
                {
                    if (_commandInfo.ParamSpecifiers[i] == 0)
                        parameters.Add((int)*(bint*)(0x04 + (WorkingSource.Address + (i * 4))));
                    else if (_commandInfo.ParamSpecifiers[i] == 1)
                        parameters.Add((float)*(bfloat*)(0x04 + (WorkingSource.Address + (i * 4))));
                }
            }
        }
        public virtual string GetFormated()
        {
            string Param = "";
            for (int i = 0; i < parameters.Count; i++)
            {
                if (parameters[i] is int | parameters[i] is bint)
                    Param += String.Format("0x{0:X}{1}", parameters[i], i + 1 != parameters.Count ? ", " : "");
                if (parameters[i] is float | parameters[i] is bfloat)
                    Param += String.Format("{0}{1}", parameters[i], i + 1 != parameters.Count ? ", " : "");
            }
            string s = String.Format("{0}({1})", _commandInfo.Name, Param);
            //string s = (*(buint*)WorkingSource.Address)._data.ToString("x");
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
            return String.Format("0x{0:X8} @0x{1:X}", ident, _offset);
        }
    }
    public unsafe class ACMDFile
    {
        public VoidPtr Header { get { return _replSource != DataSource.Empty ? _replSource.Address : WorkingSource.Address; } }
        public DataSource WorkingSource, _replSource;
        int temp = 0;

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

        public ACMDFile(DataSource source, Endianness endian)
        {
            WorkingSource = source;
            _endian = endian;

            if (endian == Endianness.Little)
            {
                _actionCount = *(int*)(source.Address + 0x08);
                _commandCount = *(int*)(source.Address + 0x0C);
            }
            else if (endian == Endianness.Big)
            {
                _actionCount = *(bint*)(source.Address + 0x08);
                _commandCount = *(bint*)(source.Address + 0x0C);
            }

            Parse();
        }

        public void Parse()
        {
            for (int i = 0; i < _actionCount; i++)
            {
                uint _flags = 0;
                int _offset = 0;

                if (_endian == Endianness.Little)
                {
                    _flags = *(uint*)(WorkingSource.Address + 0x10 + (i * 8));
                    _offset = *(int*)((WorkingSource.Address + 0x10 + (i * 8)) + 0x04);
                }
                else if (_endian == Endianness.Big)
                {
                    _flags = *(buint*)(WorkingSource.Address + 0x10 + (i * 8));
                    _offset = *(bint*)((WorkingSource.Address + 0x10 + (i * 8)) + 0x04);
                }

                Actions.Add(_flags, ParseEventList(_flags, _offset));
            }
            MessageBox.Show(temp.ToString("X"));
        }
        public void Rebuild()
        {
            FileMap temp = FileMap.FromTempFile(Size);

            OnRebuild(temp.Address, temp.Length);

            _replSource.Close();
            _replSource = new DataSource(temp.Address, temp.Length);
            _replSource.Map = temp;
        }
        public void OnRebuild(VoidPtr address, int length)
        {
            for (int i = 0; i < Actions.Count; i++)
                if (Actions.Values[i]._empty)
                    Actions.Remove(Actions.Keys[i]);

            // Rebuild ACMD header.
            VoidPtr addr = address;
            *(uint*)address = 0x444D4341;

            if (_endian == Endianness.Little)
            {
                *(int*)(address + 0x04) = 2;
                *(int*)(address + 0x08) = Actions.Count;

                int count = 0;
                foreach (CommandList e in Actions.Values)
                    count += e.Events.Count;

                *(int*)(address + 0x0C) = count;
                addr += 0x10;

                int prev = 0;
                for (int i = 0; i < Actions.Count; i++)
                {
                    *(uint*)addr = Actions.Keys[i];
                    *(int*)(addr + 4) = 0x10 + (Actions.Count * 8) + prev;
                    prev += Actions.Values[i].Size;
                    addr += 8;
                }
            }
            else if (_endian == Endianness.Big)
            {
                *(bint*)(address + 0x04) = 2;
                *(bint*)(address + 0x08) = Actions.Count;

                int count = 0;
                foreach (CommandList e in Actions.Values)
                    count += e.Events.Count;

                *(bint*)(address + 0x0C) = count;
                addr += 0x10;

                int prev = 0;
                for (bint i = 0; i < Actions.Count; i++)
                {
                    *(buint*)addr = Actions.Keys[i];
                    *(bint*)(addr + 4) = 0x10 + (Actions.Count * 8) + prev;
                    prev += Actions.Values[i].Size;
                    addr += 8;
                }
            }

            // Write event lists.
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
            VoidPtr addr = (WorkingSource.Address + _offset);
            uint endingCommand = _endian == Endianness.Little ? 0x5766F889 : 0x89F86657;

            int i = 0;
            while (*(uint*)addr != endingCommand)
            {
                uint ident = 0;
                CommandDefinition info = null;
                if (_endian == Endianness.Little)
                    ident = *(uint*)addr;
                else if (_endian == Endianness.Big)
                    ident = *(buint*)addr;

                foreach (CommandDefinition e in FormProvider.MainWindow.CommandDictionary)
                    if (e.Identifier == ident)
                        info = e;

                if (info != null)
                {
                    DataSource src = new DataSource(addr, 0x04 + (info.ParamSpecifiers.Count * 4));
                    c = new Command(_cur, i, src) { _commandInfo = info };
                    _cur.Events.Add(c);
                    addr += c.CalcSize();
                    c.getparams();
                }
                else if (info == null)
                {
                    DataSource src = new DataSource(addr, 0x04);
                    UnknownCommand unkC = new UnknownCommand() { _owner = _cur, _offset = (uint)addr - (uint)WorkingSource.Address, ident = ident, _index = i, WorkingSource = src };
                    unkC._commandInfo = new CommandDefinition() { Identifier = ident, Name = String.Format("0x{0}", ident.ToString("X")) };
                    _cur.Events.Add(unkC);
                    addr += 0x04;
                }

                i++;
                temp++;
            }

            if (*(uint*)addr == endingCommand)
            {
                CommandDefinition info = null;
                foreach (CommandDefinition e in FormProvider.MainWindow.CommandDictionary)
                    if (e.Identifier ==  0x5766F889 | e.Identifier == 0x89F86657)
                        info = e;

                DataSource src = new DataSource(addr, 0x04);
                c = new Command(_cur, i + 1, src) { _commandInfo = info };
                _cur.Events.Add(c);
                addr += 4;
                temp++;
            }

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
    }
    public enum Endianness
    {
        Little = 0,
        Big = 1
    }
}
