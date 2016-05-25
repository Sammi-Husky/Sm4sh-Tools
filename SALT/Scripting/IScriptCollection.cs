using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SALT.Scripting
{
    public interface IScriptCollection
    {
        SortedList<uint, IScript> Scripts { get; set; }
        int Size { get; }

        byte[] GetBytes(System.IO.Endianness endian);
        void Export(string path);
    }
}
