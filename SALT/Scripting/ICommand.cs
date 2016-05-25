using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SALT.Scripting
{
    public interface ICommand
    {
        List<object> Parameters { get; set; }
        uint Ident { get; set; }
        int Size { get; }

        string ToString();
        byte[] GetBytes(System.IO.Endianness endian);

    }
}
