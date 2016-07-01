using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace Parameters
{
    class GroupWrapper : ValuesWrapper
    {
        private static ContextMenuStrip _menu;
        static GroupWrapper()
        {
            _menu = new ContextMenuStrip();
            _menu.Items.Add(new ToolStripMenuItem("Apply Labels..", null, ApplyLablesAction));
            _menu.Items.Add(new ToolStripMenuItem("Global Change..", null, GlobalChangeAction));
            _menu.Items.Add(new ToolStripMenuItem("Add New Struct", null, AddStructAction));
        }
        public GroupWrapper(int index) : base($"Group[{index}]")
        {
            ContextMenuStrip = _menu;
        }

        public int EntryCount { get; set; }
        public List<ParameterType> types = new List<ParameterType>();

        public override void Wrap()
        {
            var groups = Parameters.Chunk(EntryCount);

            foreach (ParamEntry ent in groups.ElementAt(0))
                types.Add(ent.Type);

            Parameters.Clear();
            int i = 0;
            foreach (ParamEntry[] thing in groups)
            {
                Nodes.Add(new ValuesWrapper($"Entry[{i}]") { Parameters = thing.ToList() });
                i++;
            }
        }
        public override byte[] GetBytes()
        {
            List<byte> output = new List<byte>() { 0x20 };
            output.AddRange(BitConverter.GetBytes(EntryCount).Reverse());

            foreach (ValuesWrapper node in Nodes)
            {
                foreach (ParamEntry val in node.Parameters)
                {
                    output.AddRange(val.GetBytes());
                }
            }
            return output.ToArray();
        }
        private static void AddStructAction(object sender, EventArgs e)
        {
            GetInstance<GroupWrapper>().AddStruct();
        }
        private static void ApplyLablesAction(object sender, EventArgs e)
        {
            GetInstance<GroupWrapper>().ApplyLabels();
        }
        private static void GlobalChangeAction(object sender, EventArgs e)
        {
            GetInstance<GroupWrapper>().ApplyGlobalChange();
        }
        public override void ApplyLabels()
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                if (Directory.Exists(Path.Combine(Application.StartupPath, "templates")))
                    dlg.InitialDirectory = Path.Combine(Application.StartupPath, "templates");

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    using (StreamReader reader = new StreamReader(dlg.FileName))
                    {
                        var lines = reader.ReadToEnd().Split('\n').ToList();
                        foreach (var node in Nodes)
                            ((ValuesWrapper)node).labels = lines;
                    }
                }
            }
        }
        public void ApplyGlobalChange()
        {
            using (var dlg = new popupTextbox(((ValuesWrapper)Nodes[0]).Parameters.Count))
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    var type = ((ValuesWrapper)Nodes[0]).Parameters[dlg.ParamIndex].Type;
                    object value = null;
                    switch (type)
                    {
                        case ParameterType.u8:
                        case ParameterType.s8:
                            value = (byte)int.Parse(dlg.TextVal);
                            break;
                        case ParameterType.s16:
                            value = short.Parse(dlg.TextVal);
                            break;
                        case ParameterType.u16:
                            value = ushort.Parse(dlg.TextVal);
                            break;
                        case ParameterType.u32:
                            value = uint.Parse(dlg.TextVal);
                            break;
                        case ParameterType.s32:
                            value = int.Parse(dlg.TextVal);
                            break;
                        case ParameterType.f32:
                            value = float.Parse(dlg.TextVal);
                            break;
                        case ParameterType.str:
                            value = dlg.TextVal;
                            break;
                    }

                    for (int i = 0; i < Nodes.Count; i++)
                    {
                        ((ValuesWrapper)Nodes[i]).Parameters[dlg.ParamIndex].Value = value;
                    }
                }
        }
        public void AddStruct()
        {
            var entries = new List<ParamEntry>(types.Count);
            foreach (var t in types)
                switch (t)
                {
                    case ParameterType.u8:
                        entries.Add(new ParamEntry((byte)0, t));
                        break;
                    case ParameterType.s8:
                        entries.Add(new ParamEntry((byte)0, t));
                        break;
                    case ParameterType.u16:
                        entries.Add(new ParamEntry((ushort)0, t));
                        break;
                    case ParameterType.s16:
                        entries.Add(new ParamEntry((short)0, t));
                        break;
                    case ParameterType.u32:
                        entries.Add(new ParamEntry((uint)0, t));
                        break;
                    case ParameterType.s32:
                        entries.Add(new ParamEntry(0, t));
                        break;
                    case ParameterType.f32:
                        entries.Add(new ParamEntry((float)0, t));
                        break;
                    case ParameterType.str:
                        entries.Add(new ParamEntry("NewEntry", t));
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            EntryCount++;
            Nodes.Add(new ValuesWrapper($"Entry[{EntryCount}]") { Parameters = entries });
        }
    }
}
