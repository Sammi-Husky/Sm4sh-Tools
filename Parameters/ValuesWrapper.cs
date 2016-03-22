using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Parameters
{
    class ValuesWrapper : TreeNode
    {
        public ValuesWrapper(string text)
        {
            Text = text;
            Parameters = new List<ParamEntry>();
            labels = new List<string>();
        }
        public virtual void Wrap() { }
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


        protected static T GetInstance<T>() where T : ValuesWrapper { return FormProvider.Instance.treeView1.SelectedNode as T; }
    }
}
