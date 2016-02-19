using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sm4shCommand.Classes
{
    /// <summary>
    /// Contains command information
    /// </summary>
    public unsafe class CommandInfo
    {

        public uint Identifier = 0;
        public string Name { get { return _name; } set { _name = value; } }
        private string _name;
        public string EventDescription = "NONE";

        public List<int> ParamSpecifiers = new List<int>();
        public List<string> ParamSyntax = new List<string>();

    }
}
