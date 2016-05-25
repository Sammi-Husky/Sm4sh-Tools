using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SALT.Scripting
{
    public interface IScript : IEnumerable<ICommand>
    {
        byte[] GetBytes(System.IO.Endianness endian);
        int Size { get; }
        List<ICommand> Commands { get; set; }
        string Deserialize();
        void Serialize(string text);
        void Clear();
    }
}
