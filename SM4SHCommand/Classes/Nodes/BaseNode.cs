using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Sm4shCommand.Classes;
using System.Security.Cryptography;

namespace Sm4shCommand.Nodes
{
    public class BaseNode : TreeNode
    {
        private static ContextMenuStrip _menu = new ContextMenuStrip();
        protected static T GetInstance<T>() where T : BaseNode { return Runtime.Instance.FileTree.SelectedNode as T; }
    }
}
