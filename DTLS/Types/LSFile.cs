using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        private SortedList<uint, LSEntry> Entries;

        public void Parse(string path)
        {
            _workingSource = new DataSource(FileMap.FromFile(path));

            short tag = *(short*)_workingSource.Address;
            if (tag != 0x666f)
                return;
            _version = *(short*)(_workingSource.Address + 0x02);
            _entryCount = *(int*)(_workingSource.Address + 0x04);
            Entries = new SortedList<uint, LSEntry>(_entryCount);

            for (int i = 0; i < _entryCount; i++)
            {
                LSEntry lsobj = new LSEntry();
                if (Version == 1)
                {
                    _s_LSEntry_v1 entry = *(_s_LSEntry_v1*)(_workingSource.Address + 0x08 + (i * 0x0C));
                    lsobj.FileNameCRC = entry._crc;
                    lsobj.DTOffset = entry._start;
                    lsobj.Size = entry._size;
                }
                else if (Version == 2)
                {
                    _s_LSEntry_v2 entry = *(_s_LSEntry_v2*)(_workingSource.Address + 0x08 + (i * 0x10));
                    lsobj.FileNameCRC = entry._crc;
                    lsobj.DTOffset = entry._start;
                    lsobj.Size = entry._size;
                    lsobj.DTIndex = entry._dtIndex;
                    lsobj.PaddingLength = entry._unk;
                }
                Entries.Add(lsobj.FileNameCRC, lsobj);
            }
        }

        public void UpdateEntries()
        {
            VoidPtr addr = _workingSource.Address;
            addr += 0x08;

            for (int i = 0; i < _entryCount; i++)
            {
                LSEntry lsobj = Entries.Values[i];
                if (Version == 1)
                {
                    _s_LSEntry_v1* entry = (_s_LSEntry_v1*)(addr + (i * 0x0C));
                    *entry = new _s_LSEntry_v1()
                    {
                        _crc = lsobj.FileNameCRC,
                        _start = lsobj.DTOffset,
                        _size = lsobj.Size
                    };
                    _s_LSEntry_v1* entry2 = (_s_LSEntry_v1*)(addr + (i * 0x0C));
                }
                else if (Version == 2)
                {
                    _s_LSEntry_v2* entry = (_s_LSEntry_v2*)(addr + (i * 0x10));
                    *entry = new _s_LSEntry_v2()
                    {
                        _crc = lsobj.FileNameCRC,
                        _start = lsobj.DTOffset,
                        _size = lsobj.Size,
                        _dtIndex = lsobj.DTIndex,
                        _unk = lsobj.PaddingLength
                    };
                }
            }
        }

        private uint calc_crc(string filename)
        {
            var b = Encoding.ASCII.GetBytes(filename);
            for (var i = 0; i < 4; i++)
                b[i] = (byte)(~filename[i] & 0xff);
            return ZLibNet.CrcCalculator.CaclulateCRC32(b) & 0xFFFFFFFF;
        }
        public LSEntry TryGetValue(string name)
        {
            uint CRC = calc_crc(name);
            LSEntry tmp = null;
            Entries.TryGetValue(CRC, out tmp);
            return tmp;
        }
        public void TrySetValue(string key, LSEntry data)
        {
            Entries[calc_crc(key)] = data;
        }
    }
    // Proxy class for LSEntries to deal with multiple versions
    public class LSEntry
    {
        public uint FileNameCRC { get { return _crc; } set { _crc = value; } }
        private uint _crc;
        public uint DTOffset { get { return _dtOffset; } set { _dtOffset = value; } }
        private uint _dtOffset;
        public int Size { get { return _dataSize; } set { _dataSize = value; } }
        private int _dataSize;
        public short DTIndex { get { return _dtIndex; } set { _dtIndex = value; } }
        private short _dtIndex = 0;
        public short PaddingLength { get { return _padLen; } set { _padLen = value; } }
        private short _padLen;

        public int FirstFile { get; set; }
    }
    public struct _s_LSEntry_v1
    {
        public uint _crc;
        public uint _start;
        public int _size;
    }
    public struct _s_LSEntry_v2
    {
        public uint _crc;
        public uint _start;
        public int _size;
        public short _dtIndex;
        public short _unk;
    }
}
