using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parameters
{
    class GroupWrapper : ValuesWrapper
    {
        public GroupWrapper(int index) : base($"Group[{index}]") { }

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
    }
}
