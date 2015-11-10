using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTLS.Types
{
    public unsafe struct LSEntry_v1
    {
        public uint _crc;
        public uint _start;
        public uint _size;

        private VoidPtr Address { get { fixed (void* ptr = &this) return ptr; } }
    }

    public unsafe struct LSEntry_v2
    {
        public uint _crc;
        public uint _start;
        public uint _size;
        public short _dtIndex;
        public short _padlen;

        private VoidPtr Address { get { fixed (void* ptr = &this) return ptr; } }
    }
}
