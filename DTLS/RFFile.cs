using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using DTLS.IO;
using DTLS.Types;

namespace DTLS
{
    public unsafe class RFFile
    {
        public RFFile(string filename)
        {
            DataSource cmpSource = new DataSource(FileMap.FromFile(filename));
            byte[] header = cmpSource.Slice(0, 0x80);
            byte[] filedata = Util.DeCompress(cmpSource.Slice(0x80, cmpSource.Length - 0x80));
            File.WriteAllBytes($"{filename}.dec", header.Concat(filedata).ToArray());
            cmpSource.Close();

            Parse($"{filename}.dec");
        }
        public RFFile() { }

        public RfHeaderObject Header;
        public static byte[][] strChunks;
        public uint[] ExtensionOffsets;
        public string[] Extensions;
        public ResourceEntryObject[] ResourceEntries;

        public void Parse(string fileDecomp)
        {
            DataSource _workingSource = new DataSource(FileMap.FromFile(fileDecomp));
            RFHeader rfheader = *(RFHeader*)_workingSource.Address;
            Header = new RfHeaderObject
            {
                Tag = rfheader._rf,
                HeaderLen1 = rfheader._headerLen1,
                Pad = rfheader._pad0,
                EntriesChunkOffset = rfheader._headerLen2,
                EntriesChunkLen = rfheader._0x18EntriesLen,
                UnixTimestamp = rfheader._unixTimestamp,
                CompressedLen = rfheader._compressedLen,
                DecompressedLen = rfheader._decompressedLen,
                StrsChunkOffset = rfheader._strsPlus,
                StrsChunkLen = rfheader._strsLen,
                EntryCount = rfheader._resourceEntries
            };

            VoidPtr addr = _workingSource.Address + Header.StrsChunkOffset;
            strChunks = new byte[*(uint*)addr][];
            addr += 4;

            for (int i = 0; i < strChunks.Length; i++)
                strChunks[i] = _workingSource.Slice((int)(Header.StrsChunkOffset + 4) + i * 0x2000,
                    0x2000);

            addr += strChunks.Length * 0x2000;
            ExtensionOffsets = new uint[*(uint*)addr];
            Extensions = new string[ExtensionOffsets.Length];
            addr += 4;

            for (int i = 0; i < ExtensionOffsets.Length; i++)
            {
                ExtensionOffsets[i] = *(uint*)(addr + i * 4);
                var ext = Encoding.ASCII.GetString(str_from_offset((int)ExtensionOffsets[i], 64));
                Extensions[i] = ext.Remove(ext.IndexOf('\0'));
            }

            addr = _workingSource.Address + Header.EntriesChunkOffset;
            addr += *(uint*)addr * 8 + 4;
            addr += *(uint*)addr + 4;
            ResourceEntries = new ResourceEntryObject[Header.EntryCount];

            for (int i = 0; i < Header.EntryCount; i++, addr += 0x18)
            {
                ResourceEntry entry = *(ResourceEntry*)addr;
                ResourceEntryObject rsobj = new ResourceEntryObject()
                {
                    OffInChunk = entry.offInChunk,
                    NameOffsetEtc = entry.nameOffsetEtc,
                    CmpSize = entry.cmpSize,
                    DecSize = entry.decSize,
                    Timestamp = entry.timestamp,
                    Flags = entry.flags,
                };

                if (rsobj.OffInChunk == 0xBBBBBBBB)
                {
                    ResourceEntries[i] = null;
                    continue;
                }

                var strbytes = str_from_offset((int)rsobj.NameOffset, 128);
                var name = Encoding.ASCII.GetString(str_from_offset((int)rsobj.NameOffset, 128));
                if ((entry.nameOffsetEtc & 0x00800000) > 0)
                {
                    var reference = BitConverter.ToUInt16(strbytes, 0);

                    var referenceLen = (reference & 0x1f) + 4;
                    var refReloff = (reference & 0xe0) >> 6 << 8 | (reference >> 8);
                    name = Encoding.ASCII.GetString(str_from_offset((int)rsobj.NameOffset - refReloff, referenceLen)) +
                           name.Substring(2);
                }
                if (name.Contains('\0'))
                    name = name.Substring(0, name.IndexOf('\0'));

                rsobj.EntryString = name + Extensions[rsobj.extIndex];
                ResourceEntries[i] = rsobj;
            }
            _workingSource.Close();
        }
        public static byte[] str_from_offset(int start, int len)
        {
            var segOff = start & 0x1fff;
            var str = strChunks[start / 0x2000].Skip(segOff).Take(len).ToArray();
            return str;
        }
    }

    public class RfHeaderObject
    {
        public uint Tag { get { return tag; } set { tag = value; } }
        private uint tag;
        public uint HeaderLen1 { get { return headerLen1; } set { headerLen1 = value; } }
        private uint headerLen1;
        public uint Pad { get { return pad; } set { pad = value; } }
        private uint pad;
        public uint EntriesChunkOffset { get { return entriesChunkOffset; } set { entriesChunkOffset = value; } }
        private uint entriesChunkOffset;
        public uint EntriesChunkLen { get { return entriesChunkLen; } set { entriesChunkLen = value; } }
        private uint entriesChunkLen;
        public uint UnixTimestamp { get { return unixTimestamp; } set { unixTimestamp = value; } }
        private uint unixTimestamp;
        public uint CompressedLen { get { return compressedLen; } set { compressedLen = value; } }
        private uint compressedLen;
        public uint DecompressedLen { get { return decompressedLen; } set { decompressedLen = value; } }
        private uint decompressedLen;
        public uint StrsChunkOffset { get { return strsChunkOffset; } set { strsChunkOffset = value; } }
        private uint strsChunkOffset;
        public uint StrsChunkLen { get { return strsChunkLen; } set { strsChunkLen = value; } }
        private uint strsChunkLen;
        public uint EntryCount { get { return entryCount; } set { entryCount = value; } }
        private uint entryCount;
    }

    public class ResourceEntryObject
    {
        public string EntryString { get { return _string; } set { _string = value; } }
        private string _string;

        public uint OffInChunk { get { return offInChunk; } set { offInChunk = value; } }
        private uint offInChunk;
        public uint NameOffsetEtc { get { return nameOffsetEtc; } set { nameOffsetEtc = value; } }
        private uint nameOffsetEtc;
        public int CmpSize { get { return cmpSize; } set { cmpSize = value; } }
        private int cmpSize;
        public int DecSize { get { return decSize; } set { decSize = value; } }
        private int decSize;
        public uint Timestamp { get { return timestamp; } set { timestamp = value; } }
        private uint timestamp;
        public uint Flags { get { return flags; } set { flags = value; } }
        private uint flags;

        public uint extIndex { get { return nameOffsetEtc >> 24; } }
        public uint NameOffset { get { return nameOffsetEtc & 0xfffff; } }
        public int FolderDepth { get { return (int)flags & 0xff; } }
        public bool Localized { get { return (flags & 0x800) > 0; } }
        public bool HasFiles { get { return (flags & 0x400) > 0; } }
        public bool Compressed { get { return (flags & 0x200) > 0; } }
        public bool inPatch { get { return (flags & 0xFFF) <0xc00; } }
    }
}
