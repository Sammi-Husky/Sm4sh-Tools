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
using AnimCmd.Structs;
using AnimCmd.Classes;

namespace AnimCmd
{
    public unsafe partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            if (File.Exists(Application.StartupPath + "/Events.txt"))
            {
                StreamReader stream = new StreamReader(Application.StartupPath + "/Events.txt");
                List<string> a = stream.ReadToEnd().Split('\n').Select(x => x.Trim('\r')).ToList();
                a.RemoveAll(x => String.IsNullOrEmpty(x) || x.Contains("//"));

                for (int i = 0; i < a.Count; i += 3)
                {
                    EventInfo h = new EventInfo();
                    h.Identifier = uint.Parse(a[i], System.Globalization.NumberStyles.HexNumber);
                    h.Name = a[i + 1];
                    string[] tmp = a[i + 2].Split(',').Where(x => x != "NONE").ToArray();
                    foreach (string s in tmp)
                        h.ParamSpecifiers.Add(Int32.Parse(s));
                    EventDictionary.Add(h);
                    ListViewItem lvi = new ListViewItem(h.Identifier.ToString("X"));
                    lvi.SubItems.Add(h.Name);
                    listView1.Items.Add(lvi);
                }
            }
            CodeView.Dictionary = EventDictionary;
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
        EventList _linked;
        public List<EventInfo> EventDictionary = new List<EventInfo>();

        #region Parsing
        // Parses an MTable file. Basically copies all data into a list of uints.
        public List<uint> ParseMTable(DataSource source)
        {
            VoidPtr addr = source.Address;
            List<uint> ActionFlags = new List<uint>();
            int i = 0;
            while (i * 4 != source.Length)
            {
                ActionFlags.Add(*(uint*)(addr + (i * 4)));
                i++;
            }
            return ActionFlags;
        }

        public void ParseCodeBox()
        {
            _curFile.Actions[_linked._flags].Events.Clear();
            string[] lines = CodeView.Lines.Where(x => !string.IsNullOrWhiteSpace(x) && !x.Contains("//")).ToArray();
            if (lines.Length == 0)
            {
                _curFile.Actions.Remove(_linked._flags);
                _curFile.ActionCount--;
                *(int*)(_curFile.WorkingSource.Address + 0x08) = _curFile.ActionCount;
                return;
            }

            for (int i = 0; i < lines.Length; i++)
            {
                foreach (EventInfo e in EventDictionary)
                    if (lines[i].Contains(e.Name))
                    {
                        string[] temp = lines.Select(x => x.Substring(x.IndexOf('(')).Trim(new char[] { '(', ')' })).ToArray();
                        List<string[]> Params = temp.Select(x => x.Split(',')).ToList();
                        Command c = new Command();
                        c._commandInfo = e;
                        for (int counter = 0; counter < e.ParamSpecifiers.Count; counter++)
                        {
                            if (e.ParamSpecifiers[counter] == 0)
                                c.parameters.Add(float.Parse(Params[i][counter]));
                            else if (e.ParamSpecifiers[counter] == 1)
                                c.parameters.Add(Int32.Parse(Params[i][counter]));
                        }
                        _curFile.Actions[_linked._flags].Events.Add(c);
                    }
            }

            _curFile.Rebuild();
        }
        #endregion

        #region Display related methods
        // Displays the list of scripts as plain text in the code editor.
        public void DisplayScript(EventList s)
        {
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
        private void fileToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            CodeView.Text = String.Empty;
            treeView1.Nodes.Clear();
            treeView1.ShowLines = treeView1.ShowRootLines = false;

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = ".bin | *.bin";
            DialogResult result = dlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                _curFile = new ACMDFile(new DataSource(FileMap.FromFile(dlg.FileName)));

                foreach (EventList list in _curFile.Actions.Values)
                    treeView1.Nodes.Add(String.Format("{0:X8}", list._flags));

                if (_curFile.Actions.Count == 0)
                    MessageBox.Show("There were no actions found");
                else
                    isRoot = false;
            }
        }
        private void directoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CodeView.Text = String.Empty;
            treeView1.Nodes.Clear();
            treeView1.ShowLines = treeView1.ShowRootLines = true;

            FolderSelectDialog dlg = new FolderSelectDialog();
            DialogResult result = dlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                Mtable = ParseMTable(new DataSource(FileMap.FromFile(dlg.SelectedPath + "/motion.mtable")));
                CharacterFiles.Add(new ACMDFile(new DataSource(FileMap.FromFile(dlg.SelectedPath + "/game.bin"))));
                CharacterFiles.Add(new ACMDFile(new DataSource(FileMap.FromFile(dlg.SelectedPath + "/effect.bin"))));
                CharacterFiles.Add(new ACMDFile(new DataSource(FileMap.FromFile(dlg.SelectedPath + "/sound.bin"))));
                CharacterFiles.Add(new ACMDFile(new DataSource(FileMap.FromFile(dlg.SelectedPath + "/expression.bin"))));

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
            }
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
                        _curFile = CharacterFiles[0];
                        DisplayScript(CharacterFiles[0].Actions[ident]);
                    }
                    else if (e.Node.Text == "GFX")
                    {
                        _curFile = CharacterFiles[1];
                        DisplayScript(CharacterFiles[1].Actions[ident]);
                    }
                    else if (e.Node.Text == "Sound")
                    {
                        _curFile = CharacterFiles[2];
                        DisplayScript(CharacterFiles[2].Actions[ident]);
                    }
                    else if (e.Node.Text == "Expression")
                    {
                        _curFile = CharacterFiles[3];
                        DisplayScript(CharacterFiles[3].Actions[ident]);
                    }
                }
        }
        #endregion


        private void testToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //SaveFileDialog dlg = new SaveFileDialog();
            //DialogResult result = dlg.ShowDialog();
            //if (result == DialogResult.OK)
            //    Export();
            ParseCodeBox();
            if (isRoot)
                foreach (ACMDFile f in CharacterFiles)
                    f.Export(String.Format("C:/TEST/{0}.bin", CharacterFiles.IndexOf(f)));
            else
                _curFile.Export(String.Format("C:/TEST/{0}.bin", "game"));
        }
    }

    public unsafe class EventInfo
    {
        public uint Identifier;
        public string Name;
        public List<int> ParamSpecifiers = new List<int>();
    }

    public unsafe class Command
    {
        public DataSource WorkingSource { get { return _workingSource; } set { _workingSource = value; } }
        private DataSource _workingSource;

        public Command(EventList Owner, int index, DataSource source)
        {
            _owner = Owner;
            _index = index;
            _workingSource = source;
        }
        public Command() { }

        public EventInfo _commandInfo;
        public EventList _owner;
        public int _index;

        public List<object> parameters = new List<object>();

        public virtual int CalcSize() { return 0x04 + (_commandInfo.ParamSpecifiers.Count * 4); }
        public void getparams()
        {
            for (int i = 0; i < _commandInfo.ParamSpecifiers.Count; i++)
            {
                if (_commandInfo.ParamSpecifiers[i] == 0)
                    parameters.Add(*(int*)(0x04 + (WorkingSource.Address + (i * 4))));
                else if (_commandInfo.ParamSpecifiers[i] == 1)
                    parameters.Add(*(float*)(0x04 + (WorkingSource.Address + (i * 4))));
            }
        }
        public virtual string GetFormated()
        {
            string Param = "";
            for (int i = 0; i < parameters.Count; i++)
            {
                if (parameters[i] is int)
                    Param += String.Format("0x{0:X}{1}", parameters[i], i + 1 != parameters.Count ? ", " : "");
                if (parameters[i] is float)
                    Param += String.Format("{0}{1}", parameters[i], i + 1 != parameters.Count ? ", " : "");
            }
            string s = String.Format("{0}({1})", _commandInfo.Name, Param);
            return s;
        }
        public virtual byte[] ToArray()
        {
            byte[] tmp = new byte[CalcSize()];
            Util.SetWord(ref tmp, _commandInfo.Identifier, 0);
            for (int i = 0; i < _commandInfo.ParamSpecifiers.Count; i++)
            {
                if (_commandInfo.ParamSpecifiers[i] == 0)
                    Util.SetWord(ref tmp, Convert.ToInt32(parameters[i]), (i + 1) * 4);
                else if (_commandInfo.ParamSpecifiers[i] == 1)
                {
                    double HEX = Convert.ToDouble(parameters[i]);
                    float flt = (float)HEX;
                    byte[] bytes = BitConverter.GetBytes(flt);
                    int dec = BitConverter.ToInt32(bytes, 0);
                    string HexVal = dec.ToString("X");

                    Util.SetWord(ref tmp, Int32.Parse(HexVal, System.Globalization.NumberStyles.HexNumber), (i + 1) * 4);
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
        public AnimCmdHeader* Header { get { return _replSource != DataSource.Empty ? (AnimCmdHeader*)_replSource.Address : (AnimCmdHeader*)WorkingSource.Address; } }
        public DataSource WorkingSource, _replSource;

        public int CommandCount { get { return _commandCount; } set { _commandCount = value; } }
        private int _commandCount;

        public int ActionCount { get { return _actionCount; } set { _actionCount = value; } }
        private int _actionCount;

        public SortedList<uint, EventList> Actions = new SortedList<uint, EventList>();
        public int Size
        {
            get
            {
                int size = 0x10 + (_actionCount * 8);
                foreach (EventList e in Actions.Values)
                    size += e.Size;
                return size;
            }
        }

        public ACMDFile(DataSource source)
        {
            WorkingSource = source;
            _commandCount = *(int*)(source.Address + 0x0C);
            _actionCount = *(int*)(source.Address + 0x08);
            Parse();
        }

        public void Parse()
        {
            for (int i = 0; i < Header->_subactionCount; i++)
            {
                TableEntry entry = *(TableEntry*)(WorkingSource.Address + 0x10 + (i * 8));
                Actions.Add(entry._flags, ParseEventList(entry));
            }
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
            // Rebuild ACMD header.
            VoidPtr addr = address;
            AnimCmdHeader* header = (AnimCmdHeader*)address;
            header->_magic = 0x444D4341;
            header->_version = 2;
            header->_subactionCount = Actions.Count;
            int count = 0;
            foreach (EventList e in Actions.Values)
                count += e.Events.Count;
            header->_commandCount = count;
            addr += 0x10;

            // Rebuild action offset table.
            int prev = 0;
            for (int i = 0; i < Actions.Count; i++)
            {
                *(uint*)addr = Actions.Keys[i];
                *(int*)(addr + 4) = 0x10 +(Actions.Count * 8) + prev;            
                prev += Actions.Values[i].Size;
                addr += 8;
            }


            // Write event lists.
            foreach (EventList e in Actions.Values)
            {
                e.OnRebuild(addr, e.Size);
                addr += e.Size;
            }
        }

        private EventList ParseEventList(TableEntry t)
        {
            EventList _cur = new EventList(t);
            Command c = null;
            VoidPtr addr = (WorkingSource.Address + t._offset);

            int i = 0;
            while (*(uint*)addr != 0x5766F889)
            {
                EventInfo info = null;
                uint ident = *(uint*)addr;
                foreach (EventInfo e in FormProvider.MainWindow.EventDictionary)
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
                    c = new UnknownCommand() { _owner = _cur, _offset = (uint)addr - (uint)WorkingSource.Address, ident = ident, _index = i, WorkingSource = src };
                    _cur.Events.Add(c);
                    addr += 0x04;
                }

                i++;
            }

            if (*(uint*)addr == 0x5766F889)
            {
                EventInfo info = null;
                foreach (EventInfo e in FormProvider.MainWindow.EventDictionary)
                    if (e.Identifier == 0x5766F889)
                        info = e;

                DataSource src = new DataSource(addr, 0x04);
                c = new Command(_cur, i + 1, src) { _commandInfo = info };
                _cur.Events.Add(c);
                addr += 4;
            }

            return _cur;
        }
        public void Export(string path)
        {
            DataSource src = _replSource != DataSource.Empty ? _replSource : WorkingSource;
            byte[] tmp = new byte[Size];
            for (int i = 0; i < tmp.Length; i++)
                tmp[i] = *(byte*)(src.Address + i);
            File.WriteAllBytes(path, tmp);
        }

    }
}
