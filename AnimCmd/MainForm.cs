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
        MTable MotionTable = new MTable();

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
        Endianness workingEndian;

        #region Parsing
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

                CharacterFiles.Add(new ACMDFile(source, workingEndian));
                if (CharacterFiles[0].Actions.Count == 0)
                {
                    MessageBox.Show("There were no actions found");
                    return false;
                }
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

        private void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
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
        #endregion

        private void fileToolStripMenuItem1_Click(object sender, EventArgs e)
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
        }
        private void directoryToolStripMenuItem_Click_1(object sender, EventArgs e)
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
                isRoot = false;

                treeView1.ShowLines = treeView1.ShowRootLines = true;
                OpenDirectory(dlg.SelectedPath);
            }
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
            //  Remove empty event lists.
            for (int i = 0; i < Actions.Count; i++)
                if (Actions.Values[i]._empty)
                    Actions.Remove(Actions.Keys[i]);

            VoidPtr addr = address; // Base address. (0x00)
            *(uint*)address = 0x444D4341; // ACMD     

            //=========================================================================//   
            //                      Rebuilding Header and offsets                       //
            //==========================================================================//
            if (_endian == Endianness.Little)                                           //
            {                                                                           //
                *(int*)(address + 0x04) = 2; // Version (2)                             //
                *(int*)(address + 0x08) = Actions.Count; // Event list Count            //
                //
                int count = 0;                                                          //
                foreach (CommandList e in Actions.Values)                               //
                    count += e.Events.Count;                                            //
                //
                *(int*)(address + 0x0C) = count;// Unk field. (Command Count?)          //
                addr += 0x10;                                                           //
                //
                //=======Write Event List offsets and flags===============//            //
                int prev = 0;                                             //            //
                for (int i = 0; i < Actions.Count; i++)                   //            //
                {                                                         //            //
                    *(uint*)addr = Actions.Keys[i];                       //            //
                    *(int*)(addr + 4) = 0x10 + (Actions.Count * 8) + prev;//            //
                    prev += Actions.Values[i].Size;                       //            //
                    addr += 8;                                            //            //
                }                                                         //            //
                //========================================================//            //
            }                                                                           //
            //
            //
            //
            else if (_endian == Endianness.Big)                                         //
            {                                                                           //
                *(bint*)(address + 0x04) = 2;// Version (2)                             //
                *(bint*)(address + 0x08) = Actions.Count;// Event List Count            //
                //
                int count = 0;                                                          //
                foreach (CommandList e in Actions.Values)                               //
                    count += e.Events.Count;                                            //
                //
                *(bint*)(address + 0x0C) = count;// Unk field. (Command Count?)         //
                addr += 0x10;                                                           //
                //
                //=======Write Event List offsets and flags===============//            //
                int prev = 0;                                             //            //
                for (int i = 0; i < Actions.Count; i++)                   //            //
                {                                                         //            //
                    *(buint*)addr = Actions.Keys[i];                       //            //
                    *(bint*)(addr + 4) = 0x10 + (Actions.Count * 8) + prev;//            //
                    prev += Actions.Values[i].Size;                       //            //
                    addr += 8;                                            //            //
                }                                                         //            //
                //========================================================//            //                                                 //
            }                                                                           //
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
            VoidPtr addr = (WorkingSource.Address + _offset);


            int i = 0;
            while (*(uint*)addr != 0x5766F889 && *(uint*)addr != 0x89F86657)
            {
                uint ident = _endian == Endianness.Little ? *(uint*)addr : (uint)*(buint*)addr;
                CommandDefinition info = null;

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
                    unkC._commandInfo = new CommandDefinition() { Identifier = ident, Name = String.Format("0x{0:X}", ident) };
                    _cur.Events.Add(unkC);
                    addr += 0x04;
                }

                i++;
                temp++;
            }

            if (*(uint*)addr == 0x5766F889 || *(uint*)addr == 0x89F86657)
            {
                CommandDefinition info = null;
                foreach (CommandDefinition e in FormProvider.MainWindow.CommandDictionary)
                    if (e.Identifier == 0x5766F889 || e.Identifier == 0x89F86657)
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
    public unsafe class MTable : IEnumerable
    {
        public Endianness _endian;
        private List<uint> _baseList = new List<uint>();
        public uint this[int i]
        {
            get { return _baseList[i];}
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
