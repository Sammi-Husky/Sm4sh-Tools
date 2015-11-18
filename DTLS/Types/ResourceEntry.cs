using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTLS.Types
{
    public struct ResourceEntry
    {
        public uint offInPack;
        public uint nameOffsetEtc;
        public uint cmpSize;
        public uint decSize;
        public uint timestamp;
        public uint flags;
    }
}
