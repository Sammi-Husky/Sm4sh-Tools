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

        public SortedList<uint, LSEntryObject> Entries;

        public void Parse(string path)
        {
            _workingSource = new DataSource(FileMap.FromFile(path));

            short tag = *(short*)_workingSource.Address;
            if (tag != 0x666f)
                return;
            _version = *(short*)(_workingSource.Address + 0x02);
            _entryCount = *(int*)(_workingSource.Address + 0x04);
            Entries = new SortedList<uint, LSEntryObject>(_entryCount);

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

        public void UpdateEntries()
        {
            VoidPtr addr = _workingSource.Address;
            addr += 0x08;

            for (int i = 0; i < _entryCount; i++)
            {
                LSEntryObject lsobj = Entries.Values[i];
                if (Version == 1)
                {
                    LSEntry_v1* entry = (LSEntry_v1*)(addr + (i * 0x0C));
                    *entry = new LSEntry_v1()
                    {
                        _crc = lsobj.FileNameCRC,
                        _start = lsobj.DTOffset,
                        _size = lsobj.Size
                    };
                    LSEntry_v1* entry2 = (LSEntry_v1*)(addr + (i * 0x0C));
                }
                else if (Version == 2)
                {
                    LSEntry_v2* entry = (LSEntry_v2*)(addr + (i * 0x10));
                    *entry = new LSEntry_v2()
                    {
                        _crc = lsobj.FileNameCRC,
                        _start = lsobj.DTOffset,
                        _size = lsobj.Size,
                        _dtIndex = lsobj.DTIndex,
                        _padlen = lsobj.PaddingLength
                    };
                }
            }
        }

        public void ConvertToV2()
        {
            int size = 0x08 + Entries.Count * 0x10;
            string path = _workingSource.Map.FilePath;
            _workingSource.Close();
            _workingSource = new DataSource(FileMap.FromTempFile(size));
            VoidPtr addr = _workingSource.Address;
            *(uint*)addr = 0x0002666f;
            *(int*)(addr + 4) = Entries.Count;
            addr += 0x08;
            for (int i = 0; i < Entries.Count; i++)
            {
                LSEntryObject lsobj = Entries.Values[i];
                LSEntry_v2* entry = (LSEntry_v2*)(addr + (i * 0x10));
                *entry = new LSEntry_v2()
                {
                    _crc = lsobj.FileNameCRC,
                    _start = lsobj.DTOffset,
                    _size = lsobj.Size,
                    _dtIndex = lsobj.DTIndex,
                    _padlen = lsobj.PaddingLength
                };
            }
            _workingSource.Export(path);
        }
    }
    // Proxy class for LSEntries to deal with multiple versions
    public class LSEntryObject
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
    }
    public struct LSEntry_v1
    {
        public uint _crc;
        public uint _start;
        public int _size;
    }
    public struct LSEntry_v2
    {
        public uint _crc;
        public uint _start;
        public int _size;
        public short _dtIndex;
        public short _padlen;
    }
}
