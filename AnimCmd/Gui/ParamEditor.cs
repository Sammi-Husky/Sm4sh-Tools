using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SALT.PARAMS;
using Sm4shCommand.Nodes;

namespace Sm4shCommand.GUI
{
    public partial class ParamEditor : TabPage
    {
        public ParamEditor(ParamListNode node)
        {
            InitializeComponent();
            Node = node;
            tbl = new DataTable();
            tbl.Columns.Add(new DataColumn("Name") { ReadOnly = true });
            tbl.Columns.Add("Value");
            dataGridView1.DataSource = tbl;
            
            for (int i = 0; i < node.Parameters.Count; i++)
            {
                tbl.Rows.Add(i);

                // Set second column to value
                var entry = node.Parameters[i];
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
        private DataTable tbl;
        public ParamListNode Node { get; set; }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            for (int i = 0; i < Node.Parameters.Count; i++)
            {
                var t = Node.Parameters[i].Type;
                object val = null;
                switch (t)
                {
                    case ParamType.u8:
                    case ParamType.s8:
                        val = Convert.ToByte(tbl.Rows[i][1]);
                        break;
                    case ParamType.u16:
                        val = Convert.ToUInt16(tbl.Rows[i][1]);
                        break;
                    case ParamType.s16:
                        val = Convert.ToInt16(tbl.Rows[i][1]);
                        break;
                    case ParamType.u32:
                        val = Convert.ToUInt32(tbl.Rows[i][1]);
                        break;
                    case ParamType.s32:
                        val = Convert.ToInt32(tbl.Rows[i][1]);
                        break;
                    case ParamType.f32:
                        val = Convert.ToSingle(tbl.Rows[i][1]);
                        break;
                    case ParamType.str:
                        val = tbl.Rows[i][1];
                        break;
                }
                Node.Parameters[i].Value = val;
            }
        }
    }
}
