using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using SALT.PARAMS;

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
        }
        public GroupWrapper(int index) : base($"Group[{index}]")
        {
            ContextMenuStrip = _menu;
        }
        public ParamGroup Group { get; set; }

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
            using (var dlg = new popupTextbox(((ValuesWrapper)Nodes[0]).Params.Values.Count))
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    var type = ((ValuesWrapper)Nodes[0]).Params.Values[dlg.ParamIndex].Type;
                    object value = null;
                    switch (type)
                    {
                        case ParamType.u8:
                        case ParamType.s8:
                            value = (byte)int.Parse(dlg.TextVal);
                            break;
                        case ParamType.s16:
                            value = short.Parse(dlg.TextVal);
                            break;
                        case ParamType.u16:
                            value = ushort.Parse(dlg.TextVal);
                            break;
                        case ParamType.u32:
                            value = uint.Parse(dlg.TextVal);
                            break;
                        case ParamType.s32:
                            value = int.Parse(dlg.TextVal);
                            break;
                        case ParamType.f32:
                            value = float.Parse(dlg.TextVal);
                            break;
                        case ParamType.str:
                            value = dlg.TextVal;
                            break;
                    }

                    for (int i = 0; i < Nodes.Count; i++)
                    {
                        ((ValuesWrapper)Nodes[i]).Params.Values[dlg.ParamIndex].Value = value;
                    }
                }
        }
    }
}
