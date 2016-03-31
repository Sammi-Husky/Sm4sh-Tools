using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DTLS.Types
{
    public class PACKFile
    {

    }
    public struct FileData
    {
        public FileData(string name, uint offset, int len)
        {
            Name = name;
            Offset = offset;
            Size = len;
        }
        public int Size { get; set; }
        public uint Offset { get; set; }
        public string Name { get; set; }
    }
}
