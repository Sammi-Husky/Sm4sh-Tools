using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTLS.Types
{
    public unsafe struct RFHeader
    {
        public uint _rf;
        public uint _headerLen1;
        public uint _pad0;
        public uint _headerLen2;
        public uint _0x18EntriesLen;
        public uint _unixTimestamp;
        public uint _compressedLen;
        public uint _decompressedLen;
        public uint _strsPlus;
        public uint _strsLen;

        private VoidPtr Address { get { fixed (void* ptr = &this) return ptr; } }
    }
}
