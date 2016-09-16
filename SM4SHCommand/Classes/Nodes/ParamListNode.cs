using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SALT.PARAMS;

namespace Sm4shCommand.Nodes
{
    public class ParamListNode : TreeNode
    {
        public ParamListNode(int group, int entry)
        {
            Group = group;
            Entry = entry;
            labels = new List<string>();
            Parameters = new List<ParamEntry>();
        }
        public virtual byte[] GetBytes()
        {
            var output = new byte[0];
            foreach (ParamEntry param in Parameters)
            {
                output = output.Concat(param.GetBytes()).ToArray();
            }
            return output;
        }
        public List<ParamEntry> Parameters { get; set; }
        public List<string> labels { get; set; }
        public int Group { get; set; }
        public int Entry { get; set; }
    }
}
