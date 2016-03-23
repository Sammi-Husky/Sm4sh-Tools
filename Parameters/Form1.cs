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

        List<object[]> items;
        DataTable tbl;
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                treeView1.Nodes.Clear();
                ParseParams(dlg.FileName);
                tbl.Rows.Clear();
            }
        }
        private void ParseParams(string filepath)
        {
            using (FileStream stream = new FileStream(filepath, FileMode.Open, FileAccess.ReadWrite))
            {
                using (var reader = new BinaryReader(stream))
                {
                    stream.Seek(0x08, SeekOrigin.Begin);
                    var wrp = new ValuesWrapper("Group[0]");
                    int group = 0;

                    while (stream.Position != stream.Length)
                    {
                        ParameterType type = (ParameterType)stream.ReadByte();
                        switch (type)
                        {
                            case ParameterType.u8:
                                wrp.Parameters.Add(new ParamEntry(reader.ReadByte(), type));
                                break;
                            case ParameterType.s8:
                                wrp.Parameters.Add(new ParamEntry(reader.ReadByte(), type));
                                break;
                            case ParameterType.u16:
                                wrp.Parameters.Add(new ParamEntry(reader.ReadUInt16().Reverse(), type));
                                break;
                            case ParameterType.s16:
                                wrp.Parameters.Add(new ParamEntry(reader.ReadInt16().Reverse(), type));
                                break;
                            case ParameterType.u32:
                                wrp.Parameters.Add(new ParamEntry(reader.ReadUInt32().Reverse(), type));
                                break;
                            case ParameterType.s32:
                                wrp.Parameters.Add(new ParamEntry(reader.ReadInt32().Reverse(), type));
                                break;
                            case ParameterType.f32:
                                wrp.Parameters.Add(new ParamEntry(reader.ReadSingle().Reverse(), type));
                                break;
                            case ParameterType.str:
                                int tmp = reader.ReadInt32().Reverse();
                                wrp.Parameters.Add(new ParamEntry(new string(reader.ReadChars(tmp)), type));
                                break;
                            case ParameterType.group:
                                if (wrp.Parameters.Count > 0)
                                {
                                    wrp.Wrap();
                                    treeView1.Nodes.Add(wrp);
                                }
                                wrp = new GroupWrapper(++group);
                                ((GroupWrapper)wrp).EntryCount = reader.ReadInt32().Reverse();
                                break;
                            default:
                                throw new NotImplementedException($"unk typecode: {type} at offset: {stream.Position:X}");
                        }
                    }
                    wrp.Wrap();
                    treeView1.Nodes.Add(wrp);
                }
            }
        }

        private void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            var node = e.Node as ValuesWrapper;
            if (node is GroupWrapper)
                return;

            tbl.Rows.Clear();
            for (int i = 0; i < node.Parameters.Count; i++)
            {
                // Add row with first column set as index or label
                if (i < node.labels.Count && !string.IsNullOrWhiteSpace(node.labels[i]))
                    tbl.Rows.Add(node.labels[i]);
                else
                    tbl.Rows.Add(i);

                // Set second column to value
                var entry = node.Parameters[i];
                switch (entry.Type)
                {
                    case ParameterType.u8:
                        tbl.Rows[i][1] = (byte)entry.Value;
                        break;
                    case ParameterType.s8:
                        tbl.Rows[i][1] = (byte)entry.Value;
                        break;
                    case ParameterType.u16:
                        tbl.Rows[i][1] = (ushort)entry.Value;
                        break;
                    case ParameterType.s16:
                        tbl.Rows[i][1] = (short)entry.Value;
                        break;
                    case ParameterType.u32:
                        tbl.Rows[i][1] = (uint)entry.Value;
                        break;
                    case ParameterType.s32:
                        tbl.Rows[i][1] = (int)entry.Value;
                        break;
                    case ParameterType.f32:
                        tbl.Rows[i][1] = (float)entry.Value;
                        break;
                    case ParameterType.str:
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
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    using (FileStream stream = new FileStream(dlg.FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    {
                        using (BinaryWriter writer = new BinaryWriter(stream))
                        {
                            writer.Write(0x0000FFFF);
                            writer.Write(0);
                            foreach (ValuesWrapper node in treeView1.Nodes)
                            {

                                byte[] data = null;
                                if (node is ValuesWrapper)
                                    data = ((ValuesWrapper)node).GetBytes();
                                else if (node is GroupWrapper)
                                    data = ((GroupWrapper)node).GetBytes();

                                if (data != null)
                                    writer.Write(data, 0, data.Length);
                            }
                        }
                    }
                }
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (treeView1.SelectedNode == null | treeView1.SelectedNode is GroupWrapper)
                return;

            ValuesWrapper wrp = treeView1.SelectedNode as ValuesWrapper;

            for (int i = 0; i < wrp.Parameters.Count; i++)
            {
                var t = wrp.Parameters[i].Type;
                object val = null;
                switch (t)
                {
                    case ParameterType.u8:
                    case ParameterType.s8:
                        val = Convert.ToByte(tbl.Rows[i][1]);
                        break;
                    case ParameterType.u16:
                        val = Convert.ToUInt16(tbl.Rows[i][1]);
                        break;
                    case ParameterType.s16:
                        val = Convert.ToInt16(tbl.Rows[i][1]);
                        break;
                    case ParameterType.u32:
                        val = Convert.ToUInt32(tbl.Rows[i][1]);
                        break;
                    case ParameterType.s32:
                        val = Convert.ToInt32(tbl.Rows[i][1]);
                        break;
                    case ParameterType.f32:
                        val = Convert.ToSingle(tbl.Rows[i][1]);
                        break;
                    case ParameterType.str:
                        val = tbl.Rows[i][1];
                        break;
                }
                ((ValuesWrapper)treeView1.SelectedNode).Parameters[i].Value = val;
            }
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            treeView1.SelectedNode = e.Node;
        }
    }

    public enum ParameterType : byte
    {
        u8 = 1,
        s8 = 2,
        u16 = 3,
        s16 = 4,
        u32 = 5,
        s32 = 6,
        f32 = 7,
        str = 8,
        group = 0x20
    }
}
