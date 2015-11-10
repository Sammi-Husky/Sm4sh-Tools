using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DTLS.Types;
using System.IO;
using System.Threading.Tasks;

namespace DTLS
{
    public unsafe class LSFile
    {
        public LSFile(string filepath)
        {
            Parse(filepath);
        }
        public LSFile() { }

        public DataSource WorkingSource => _workingSource;
        private DataSource _workingSource;

        public int Version { get { return _version; } set { _version = value; } }
        private int _version;

        public int EntryCount { get { return _entryCount; } set { _entryCount = value; } }
        private int _entryCount;

        public Dictionary<uint, LSEntryObject> Entries;

        public void Parse(string path)
        {
            _workingSource = new DataSource(FileMap.FromFile(path));

            short tag = *(short*)_workingSource.Address;
            if (tag != 0x666f)
                return;
            _version = *(short*)(_workingSource.Address + 0x02);
            _entryCount = *(int*)(_workingSource.Address + 0x04);
            Entries = new Dictionary<uint, LSEntryObject>(_entryCount);

            for (int i = 0; i < _entryCount; i++)
            {
                LSEntryObject lsobj = new LSEntryObject();
                if (Version == 1)
                {
                    LSEntry_v1 entry = *(LSEntry_v1*)(_workingSource.Address + 0x08 + (i * 0x0C));
                    lsobj.FileNameCRC = entry._crc;
                    lsobj.DTOffset = entry._start;
                    lsobj.Size = entry._size;
                }
                else if (Version == 2)
                {
                    LSEntry_v2 entry = *(LSEntry_v2*)(_workingSource.Address + 0x08 + (i * 0x10));
                    lsobj.FileNameCRC = entry._crc;
                    lsobj.DTOffset = entry._start;
                    lsobj.Size = entry._size;
                    lsobj.DTIndex = entry._dtIndex;
                    lsobj.PaddingLength = entry._padlen;
                }
                Entries.Add(lsobj.FileNameCRC, lsobj);
            }
        }
    }

    // Proxy class for LSEntries to deal with multiple versions
    public class LSEntryObject
    {
        public uint FileNameCRC { get { return _crc; } set { _crc = value; } }
        private uint _crc;
        public uint DTOffset { get { return _dtOffset; } set { _dtOffset = value; } }
        private uint _dtOffset;
        public uint Size { get { return _dataSize; } set { _dataSize = value; } }
        private uint _dataSize;
        public short DTIndex { get { return _dtIndex; } set { _dtIndex = value; } }
        private short _dtIndex = 0;
        public short PaddingLength { get { return _padLen; } set { _padLen = value; } }
        private short _padLen;
    }
}
