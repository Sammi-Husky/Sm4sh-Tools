using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTLS.Types
{
    public unsafe struct LSEntry
    {
        public uint _crc;
        public uint _start;
        public uint _size;

        private VoidPtr Address { get { fixed (void* ptr = &this) return ptr; } }
    }
}
