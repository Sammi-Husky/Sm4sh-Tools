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

        public uint Identifier;
        public string Name;
        public string EventDescription;


        public List<int> ParamSpecifiers = new List<int>();
        public List<string> ParamSyntax = new List<string>();

    }
}
