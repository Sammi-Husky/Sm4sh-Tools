using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using SALT.PARAMS;

namespace Parameters
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            items = new List<object[]>();
            tbl = new DataTable();
            tbl.Columns.Add(new DataColumn("Name") { ReadOnly = true });
            tbl.Columns.Add("Value");
            dataGridView1.DataSource = tbl;
        }

        private ParamFile OpenedFile { get; set; }
        private string fileLoaded = string.Empty;
        List<object[]> items;
        DataTable tbl;
        public void LoadFile(string file)
        {
            if (File.Exists(file))
            {
                fileLoaded = file;
                treeView1.Nodes.Clear();
                ParseParams(file);
                tbl.Rows.Clear();
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                LoadFile(dlg.FileName);
            }
        }
        private void ParseParams(string filepath)
        {
            OpenedFile = new ParamFile(filepath);
            for (int i = 0; i < OpenedFile.Groups.Count; i++)
            {
                TreeNode node = null;
                if (OpenedFile.Groups[i] is ParamGroup)
                {
                    var g = (ParamGroup)OpenedFile.Groups[i];
                    node = new GroupWrapper(i) { Group = g };
                    for (int x = 0; x < g.Chunks.Length; x++)
                    {
                        node.Nodes.Add(new ValuesWrapper($"Entry[{x}]") { Params = g.Chunks[x] });
                    }
                }
                else
                {
                    var g = ((ParamList)OpenedFile.Groups[i]);
                    node = new ValuesWrapper($"Values[{i}]") { Params = g };
                }
                treeView1.Nodes.Add(node);
            }
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag is ParamGroup)
                return;

            tbl.Rows.Clear();

            var vals = ((ValuesWrapper)e.Node).Params.Values;
            for (int i = 0; i < vals.Count; i++)
            {
                tbl.Rows.Add(i);

                var entry = vals[i];
                switch (entry.Type)
                {
                    case ParamType.u8:
                        tbl.Rows[i][1] = (byte)entry.Value;
                        break;
                    case ParamType.s8:
                        tbl.Rows[i][1] = (byte)entry.Value;
                        break;
                    case ParamType.u16:
                        tbl.Rows[i][1] = (ushort)entry.Value;
                        break;
                    case ParamType.s16:
                        tbl.Rows[i][1] = (short)entry.Value;
                        break;
                    case ParamType.u32:
                        tbl.Rows[i][1] = (uint)entry.Value;
                        break;
                    case ParamType.s32:
                        tbl.Rows[i][1] = (int)entry.Value;
                        break;
                    case ParamType.f32:
                        tbl.Rows[i][1] = (float)entry.Value;
                        break;
                    case ParamType.str:
                        tbl.Rows[i][1] = (string)entry.Value;
                        break;
                    default:
                        tbl.Rows[i][1] = entry.Value.ToString();
                        break;
                }
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                if (!string.IsNullOrEmpty(fileLoaded))
                {
                    dlg.InitialDirectory = Path.GetDirectoryName(fileLoaded);
                    dlg.FileName = Path.GetFileName(fileLoaded);
                }

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    OpenedFile.Export(dlg.FileName);
                }
            }
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (treeView1.SelectedNode == null | treeView1.SelectedNode.Tag is ParamGroup)
                return;

            var vals = treeView1.SelectedNode.Tag as ParamEntry[];
            int row = e.RowIndex;

            var t = vals[row].Type;
            object val = null;
            switch (t)
            {
                case ParamType.u8:
                case ParamType.s8:
                    val = Convert.ToByte(tbl.Rows[row][1]);
                    break;
                case ParamType.u16:
                    val = Convert.ToUInt16(tbl.Rows[row][1]);
                    break;
                case ParamType.s16:
                    val = Convert.ToInt16(tbl.Rows[row][1]);
                    break;
                case ParamType.u32:
                    val = Convert.ToUInt32(tbl.Rows[row][1]);
                    break;
                case ParamType.s32:
                    val = Convert.ToInt32(tbl.Rows[row][1]);
                    break;
                case ParamType.f32:
                    val = Convert.ToSingle(tbl.Rows[row][1]);
                    break;
                case ParamType.str:
                    val = tbl.Rows[row][1];
                    break;
            }
            ((ParamEntry[])treeView1.SelectedNode.Tag)[row].Value = val;
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            treeView1.SelectedNode = e.Node;
        }
    }
}
