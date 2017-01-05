using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.IO;

namespace DTLS
{
    public unsafe class RFFile
    {
        public DataSource WorkingSource { get; set; }
        public DataSource CompressedSource { get; set; }
        public RfHeader Header { get; set; }

        public RFFile() { }
        public RFFile(string filepath)
        {

            CompressedSource = new DataSource(FileMap.FromFile(filepath));
            byte[] header = CompressedSource.Slice(0, 0x80);
            byte[] filedata = Util.DeCompress(CompressedSource.Slice(0x80, CompressedSource.Length - 0x80));
            File.WriteAllBytes(filepath + ".dec", header.Concat(filedata).ToArray());
            WorkingSource = new DataSource(FileMap.FromFile(filepath + ".dec"));
            Parse();
        }

        public int RegionCode { get { return (Header.RegionEtc & 3) - 1; } }
        private string[] Extensions;
        public string Locale { get; set; }

        public List<ResourceEntry> ResourceEntries { get { return _entries; } set { _entries = value; } }
        private List<ResourceEntry> _entries = new List<ResourceEntry>();
        public ResourceEntry this[int i]
        {
            get { return ResourceEntries[i]; }
            set { ResourceEntries[i] = value; }
        }

        public void Parse()
        {
            _s_RFHeader rfheader = *(_s_RFHeader*)WorkingSource.Address;
            Header = new RfHeader
            {
                Tag = rfheader._rf,
                RegionEtc = rfheader._regionEtc,
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

            VoidPtr addr = WorkingSource.Address + Header.StrsChunkOffset;
            addr += *(uint*)addr * 0x2000;
            addr += 4;
            Extensions = new string[*(uint*)addr];
            addr += 4;

            for (int i = 0; i < Extensions.Length; i++)
                Extensions[i] = GetStr(*(int*)(addr + i * 4));


            addr = WorkingSource.Address + Header.EntriesChunkOffset;
            uint size1 = *(uint*)addr * 8 + 4;
            addr += size1;
            uint size2 = *(uint*)addr + 4;
            addr += size2;

            var entries = new List<ResourceEntry>();

            for (int i = 0; i < Header.EntryCount; i++, addr += 0x18)
            {
                _s_ResourceEntry entry = *(_s_ResourceEntry*)addr;
                ResourceEntry rsobj = new ResourceEntry()
                {
                    OffInPack = entry.offInPack,
                    NameOffsetEtc = entry.nameOffsetEtc,
                    CmpSize = entry.cmpSize,
                    DecSize = entry.decSize,
                    Timestamp = DateTimeExtension.FromUnixBytes(entry.timestamp),
                    Flags = entry.flags,
                };

                if (rsobj.OffInPack == 0xBBBBBBBB)
                {
                    ResourceEntries.Add(null);
                    continue;
                }

                var name = "";
                if ((entry.nameOffsetEtc & 0x00800000) > 0)
                {
                    var strbytes = GetStrRef((int)rsobj.NameOffset);
                    var reference = BitConverter.ToUInt16(strbytes, 0);

                    var reflen = (reference & 0x1f) + 4;
                    var refoff = (reference & 0xe0) >> 6 << 8 | (reference >> 8);
                    name = GetStr((int)rsobj.NameOffset - refoff).Remove(reflen) + GetStr((int)rsobj.NameOffset + 2);
                }
                else
                    name = GetStr((int)rsobj.NameOffset);

                rsobj.EntryString = name + Extensions[rsobj.extIndex];
                ResourceEntries.Add(rsobj);
            }
            //ResourceEntries = LinkEntries(entries);
        }
        public void UpdateEntries()
        {

            VoidPtr addr = WorkingSource.Address + Header.EntriesChunkOffset;
            uint size1 = *(uint*)addr * 8 + 4;
            addr += size1;
            uint size2 = *(uint*)addr + 4;
            addr += size2;

            for (int i = 0; i < Header.EntryCount; i++)
            {
                if (ResourceEntries[i] == null)
                {
                    //ResourceEntry* entry = (ResourceEntry*)addr;
                    //*entry = new ResourceEntry
                    //{
                    //    offInPack = 0xBBBBBBBB,
                    //    nameOffsetEtc = 0xBBBBBBBB,
                    //    cmpSize = 0xBBBBBBBB,
                    //    decSize = 0xBBBBBBBB,
                    //    timestamp = 0xBBBBBBBB,
                    //    flags = 0xBBBBBBBB
                    //};
                    addr += 0x18;
                }
                else
                {
                    _s_ResourceEntry* entry = (_s_ResourceEntry*)addr;
                    *entry = new _s_ResourceEntry
                    {
                        offInPack = ResourceEntries[i].OffInPack,
                        nameOffsetEtc = ResourceEntries[i].NameOffsetEtc,
                        cmpSize = ResourceEntries[i].CmpSize,
                        decSize = ResourceEntries[i].DecSize,
                        timestamp = ResourceEntries[i].Timestamp.ToUnixBytes(),
                        flags = ResourceEntries[i].Flags
                    };
                    addr += 0x18;
                }
            }
        }

        public byte[] GetStrRef(int start)
        {
            var segOff = start & 0x1fff;
            return BitConverter.GetBytes(*(short*)(WorkingSource.Address +
                Header.StrsChunkOffset + 4 + start));
        }
        public string GetStr(int start)
        {
            var segOff = start & 0x1fff;
            return new String((sbyte*)(WorkingSource.Address +
                Header.StrsChunkOffset + 4 + start));
        }

        public byte[] GetBytes()
        {
            byte[] data = WorkingSource.Slice((int)Header.HeaderLen1,
                WorkingSource.Length - (int)Header.HeaderLen1);
            Header.DecompressedLen = (uint)data.Length;
            data = Util.Compress(data);
            Header.CompressedLen = (uint)data.Length;
            byte[] header = Header.ToArray();
            byte[] full = header.Concat(data).ToArray();

            return full;
        }
    }

    public class RfHeader
    {
        public short Tag { get { return tag; } set { tag = value; } }
        private short tag;
        public short RegionEtc { get { return _regionEtc; } set { _regionEtc = value; } }
        private short _regionEtc;
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

        public byte[] ToArray()
        {
            byte[] b = new byte[0x80];
            Util.SetWord(ref b, tag, 0);
            Util.SetHalf(ref b, RegionEtc, 2);
            Util.SetWord(ref b, headerLen1, 4);
            Util.SetWord(ref b, pad, 8);
            Util.SetWord(ref b, EntriesChunkOffset, 0x0C);
            Util.SetWord(ref b, entriesChunkLen, 0x10);
            Util.SetWord(ref b, unixTimestamp, 0x14);
            Util.SetWord(ref b, compressedLen, 0x18);
            Util.SetWord(ref b, decompressedLen, 0x1c);
            Util.SetWord(ref b, strsChunkOffset, 0x20);
            Util.SetWord(ref b, strsChunkLen, 0x24);
            Util.SetWord(ref b, entryCount, 0x28);

            for (int i = 0; i < 0x15; i++)
                Util.SetWord(ref b, 0xAAAAAAAA, 0x2C + i * 4);

            return b;
        }
    }
    public unsafe struct _s_RFHeader
    {
        public short _rf;
        public short _regionEtc;
        public uint _headerLen1;
        public uint _pad0;
        public uint _headerLen2;
        public uint _0x18EntriesLen;
        public uint _unixTimestamp;
        public uint _compressedLen;
        public uint _decompressedLen;
        public uint _strsPlus;
        public uint _strsLen;
        public uint _resourceEntries;

        private VoidPtr Address { get { fixed (void* ptr = &this) return ptr; } }
    }

    public class ResourceEntry
    {
        public string EntryString { get { return _string; } set { _string = value; } }
        private string _string;

        [Browsable(false)]
        public uint OffInPack { get { return offInPack; } set { offInPack = value; } }
        private uint offInPack;
        [Browsable(false)]
        public uint NameOffsetEtc { get { return nameOffsetEtc; } set { nameOffsetEtc = value; } }
        private uint nameOffsetEtc;
        [Browsable(false)]
        public int CmpSize { get { return cmpSize; } set { cmpSize = value; } }
        private int cmpSize;
        [Browsable(false)]
        public int DecSize { get { return decSize; } set { decSize = value; } }
        private int decSize;

        public DateTime Timestamp
        {
            get
            {
                DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                dtDateTime = dtDateTime.AddSeconds(timestamp).ToLocalTime();
                return dtDateTime;
            }
            set
            {
                timestamp = value.ToUnixBytes();
            }
        }
        private uint timestamp;

        public uint extIndex { get { return nameOffsetEtc >> 24; } }
        public uint NameOffset { get { return nameOffsetEtc & 0xfffff; } }

        public int FolderDepth { get { return (int)_flags & 0xff; } }
        public bool Localized { get { return (_flags & 0x800) > 0; } }
        public bool Packed { get { return (_flags & 0x400) > 0; } }
        public bool IsDirectory { get { return (_flags & 0x200) > 0; } }
        public bool OverridePackedFile { get { return (_flags & 0x4000) > 0; } }

        [Browsable(false)]
        public uint Flags { get { return _flags; } set { _flags = value; } }
        private uint _flags;

        public override string ToString()
        {
            return EntryString;
        }

        // Code taken from Dei's SM4SHExplorer.
        private uint CalculateFlags()
        {
            uint flag = 0x00000000;

            flag |= (uint)FolderDepth;

            if (this.Packed)
                flag |= 0x400;

            if (!this.Localized) //Everything in the main resource has the flag
                flag |= 0x800;

            if (OverridePackedFile)
                flag |= 0x4000;

            if (this.IsDirectory)
            {
                flag |= 0x200;

                if (this.Packed || this.OffInPack != 0)
                    flag |= 0x1000;
            }

            return flag;
        }
    }
    public struct _s_ResourceEntry
    {
        public uint offInPack;
        public uint nameOffsetEtc;
        public int cmpSize;
        public int decSize;
        public uint timestamp;
        public uint flags;
    }
}