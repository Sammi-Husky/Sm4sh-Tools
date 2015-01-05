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
                a.RemoveAll(x => String.IsNullOrEmpty(x));

                for (int i = 0; i < a.Count; i += 3)
                {
                    EventInfo h = new EventInfo();
                    h.Identifier = uint.Parse(a[i], System.Globalization.NumberStyles.HexNumber);
                    h.Name = a[i + 1];
                    string[] tmp = a[i + 2].Split(',').Where(x => x != "NONE").ToArray();
                    foreach (string s in tmp)
                        h.ParamSpecifiers.Add(Int32.Parse(s));
                    EventDictionary.Add(h);
                }
            }
            CodeView.Dictionary = EventDictionary;
        }

        //================================================================================\\
        // List of action event lists in single file mode. Null if in full directory mode.\\
        //================================================================================\\
        SortedList<uint, EventList> FileEvents = new SortedList<uint, EventList>();

        //==================================\\
        // List of motion table identifiers \\
        //==================================\\
        List<uint> Mtable = new List<uint>();

        //===========================================================================\\
        // Main, gfx, sound, and expression event lists. Null if in single file mode.\\
        //===========================================================================\\
        SortedList<uint, EventList> EventsMain = new SortedList<uint, EventList>();
        SortedList<uint, EventList> EventsGFX = new SortedList<uint, EventList>();
        SortedList<uint, EventList> EventsSound = new SortedList<uint, EventList>();
        SortedList<uint, EventList> EventsExpression = new SortedList<uint, EventList>();

        // True if in multi file mode.
        bool isRoot = false;
        public List<EventInfo> EventDictionary = new List<EventInfo>();

        #region Parsing
        // Parses an ACMD file, returning a list of EventLists sorted by flags (used as idents)
        public SortedList<uint, EventList> ParseACMD(DataSource source)
        {
            AnimCmdHeader* Header = (AnimCmdHeader*)source.Address;
            SortedList<uint, EventList> temp = new SortedList<uint, EventList>();
            for (int i = 0; i < Header->_subactionCount; i++)
            {
                TableEntry entry = *(TableEntry*)(source.Address + 0x10 + (i * 8));
                temp.Add(entry._flags, ParseEventList(entry, source));
            }
            return temp;
        }

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

        // Parses an event list at a specific address specified by the passed in TableEntry, in the passed in file.
        public EventList ParseEventList(TableEntry t, DataSource FileSource)
        {
            EventList _cur = new EventList(t);
            Command c = null;
            EventInfo info = null;
            VoidPtr addr = (FileSource.Address + t._offset);

            int i = 0;
            while (*(uint*)addr != 0x5766F889)
            {
                uint ident = *(uint*)addr;
                foreach (EventInfo e in EventDictionary)
                    if (e.Identifier == ident)
                        info = e;

                if (info != null)
                {
                    DataSource src = new DataSource(addr, 0x04 + (info.ParamSpecifiers.Count * 4));
                    c = new Command(_cur, i, src) { _commandInfo = info };
                    _cur.Events.Add(c);
                    addr += c.CalcSize();
                }

                else if (info == null)
                {
                    DataSource src = new DataSource(addr, 0x04);
                    c = new UnknownCommand() { _owner = _cur, _offset = (uint)addr - (uint)FileSource.Address, ident = ident, _index = i, WorkingSource = src };
                    _cur.Events.Add(c);
                    addr += 0x04;
                    i++;
                    continue;
                }
                c.getparams();
                i++;
            }

            if (*(uint*)addr == 0x5766F889)
            {
                foreach (EventInfo e in EventDictionary)
                    if (e.Identifier == 0x5766F889)
                        info = e;
                DataSource src = new DataSource(addr, 0x04);
                c = new Command(_cur, ++i, src) { _commandInfo = info };
                _cur.Events.Add(c);
                addr += 4;
            }
            return _cur;
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
        }
        #endregion

        #region Event Handler Methods
        private void fileToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            CodeView.Text = String.Empty;
            FileEvents.Clear(); treeView1.Nodes.Clear();
            treeView1.ShowLines = treeView1.ShowRootLines = false;

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = ".bin | *.bin";
            DialogResult result = dlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                FileEvents = ParseACMD(new DataSource(FileMap.FromFile(dlg.FileName)));

                foreach (EventList list in FileEvents.Values)
                    treeView1.Nodes.Add(String.Format("{0:X8}", list._flags));

                if (FileEvents.Count == 0)
                    MessageBox.Show("There were no actions found");
                else
                    isRoot = false;
            }
        }
        private void directoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CodeView.Text = String.Empty;
            FileEvents.Clear(); treeView1.Nodes.Clear();
            treeView1.ShowLines = treeView1.ShowRootLines = true;

            FolderSelectDialog dlg = new FolderSelectDialog();
            DialogResult result = dlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                Mtable = ParseMTable(new DataSource(FileMap.FromFile(dlg.SelectedPath + "/motion.mtable")));
                EventsMain = ParseACMD(new DataSource(FileMap.FromFile(dlg.SelectedPath + "/game.bin")));
                EventsGFX = ParseACMD(new DataSource(FileMap.FromFile(dlg.SelectedPath + "/effect.bin")));
                EventsSound = ParseACMD(new DataSource(FileMap.FromFile(dlg.SelectedPath + "/sound.bin")));
                EventsExpression = ParseACMD(new DataSource(FileMap.FromFile(dlg.SelectedPath + "/expression.bin")));

                int counter = 0;
                foreach (uint u in Mtable)
                {
                    TreeNode n = new TreeNode(String.Format("{0:X} [{1:X8}]", counter, u));

                    if (EventsMain.ContainsKey(u))
                        n.Nodes.Add("Main");
                    if (EventsGFX.ContainsKey(u))
                        n.Nodes.Add("GFX");
                    if (EventsSound.ContainsKey(u))
                        n.Nodes.Add("Sound");
                    if (EventsExpression.ContainsKey(u))
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
                    DisplayScript(FileEvents[Ident]);
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
                        DisplayScript(EventsMain[ident]);
                    else if (e.Node.Text == "GFX")
                        DisplayScript(EventsGFX[ident]);
                    else if (e.Node.Text == "Sound")
                        DisplayScript(EventsSound[ident]);
                    else if (e.Node.Text == "Expression")
                        DisplayScript(EventsExpression[ident]);
                }
        }
        #endregion
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
                    parameters.Add(*(int*)(0x04 + WorkingSource.Address + (i * 4)));
                else if (_commandInfo.ParamSpecifiers[i] == 1)
                    parameters.Add(*(float*)(0x04 + WorkingSource.Address + (i * 4)));
            }
        }
        public virtual string GetFormated()
        {
            string Param = "";
            for (int i = 0; i < parameters.Count; i++)
            {
                if (parameters[i] is int)
                    Param += String.Format("{0}, ", parameters[i]);
                if (parameters[i] is float)
                    Param += String.Format("{0}, ", parameters[i]);
            }
            string s = String.Format("{0}({1})", _commandInfo.Name, Param);
            return s;
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
}
