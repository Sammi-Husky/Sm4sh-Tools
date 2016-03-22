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
        }
        public GroupWrapper(int index) : base($"Group[{index}]")
        {
            ContextMenuStrip = _menu;
        }

        public int EntryCount { get; set; }
        public override void Wrap()
        {
            var groups = Parameters.Chunk(EntryCount);
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
            var output = new byte[1] { 0x20 }.Concat(BitConverter.GetBytes(EntryCount).Reverse()).ToArray();

            foreach (ValuesWrapper node in Nodes)
            {
                foreach (ParamEntry val in node.Parameters)
                {
                    output = output.Concat(val.GetBytes()).ToArray();
                }
            }
            return output;
        }
        private static void ApplyLablesAction(object sender, EventArgs e)
        {
            GetInstance<GroupWrapper>().ApplyLabels();
        }
        public void ApplyLabels()
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    using (StreamReader reader = new StreamReader(dlg.FileName))
                    {
                        string[] lines = reader.ReadLine().Split('\n');
                        foreach (var node in Nodes)
                            ((ValuesWrapper)node).labels = lines.ToList();
                    }
                }
        }
    }
}
