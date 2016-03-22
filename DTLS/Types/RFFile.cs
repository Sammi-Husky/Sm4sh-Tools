using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.IO;
using DTLS.IO;

namespace DTLS
{
    public unsafe class RFFile
    {
        public DataSource WorkingSource { get; set; }
        public RfHeader Header { get; set; }
        public RFFile(byte[] data)
        {
            fixed (byte* src = data)
            {
                DataSource cmpSource = new DataSource(src, data.Length);
                byte[] header = cmpSource.Slice(0, 0x80);
                byte[] filedata = Util.DeCompress(cmpSource.Slice(0x80, cmpSource.Length - 0x80));
                Parse(header.Concat(filedata).ToArray());
                cmpSource.Close();
            }
        }

        public int RegionCode { get { return Header.RegionEtc & 3; } }
        private string[] Extensions;
        public string Locale { get; set; }

        public List<ResourceEntry> ResourceEntries { get { return _entries; } set { _entries = value; } }
        private List<ResourceEntry> _entries = new List<ResourceEntry>();
        public ResourceEntry this[int i]
        {
            get { return ResourceEntries[i]; }
            set { ResourceEntries[i] = value; }
        }

        public void Parse(byte[] data)
        {
            fixed (byte* src = data)
                WorkingSource = new DataSource(src, data.Length);

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

                    var referenceLen = (reference & 0x1f) + 4;
                    var refReloff = (reference & 0xe0) >> 6 << 8 | (reference >> 8);
                    name = GetStr((int)rsobj.NameOffset - refReloff).Remove(referenceLen) + GetStr((int)rsobj.NameOffset + 2);
                }
                else
                    name = GetStr((int)rsobj.NameOffset);

                rsobj.EntryString = name + Extensions[rsobj.extIndex];
                ResourceEntries.Add(rsobj);
            }
            //ResourceEntries = LinkEntries(entries);
        }
        private List<ResourceEntry> LinkEntries(List<ResourceEntry> entries)
        {
            entries.Clear();
            ResourceEntry tmpNode = null;

            foreach (ResourceEntry entry in ResourceEntries)
            {
                if (entry == null)
                    continue;

                if (entry.FolderDepth == 1)
                {
                    if (tmpNode?.Root != null)
                        entries.Add(tmpNode.Root);
                    tmpNode = entry;
                    continue;
                }
                else if (entry.Directory)
                {

                    var tmp = entry;
                    if (entry.FolderDepth > tmpNode.FolderDepth)
                        tmp.Parent = tmpNode;
                    else if (entry.FolderDepth < tmpNode.FolderDepth)
                        tmp.Parent = tmpNode.Parent.Parent;
                    else
                        tmp.Parent = tmpNode.Parent;
                    tmpNode.InsertChild(tmp.FolderDepth - 1, tmp);
                    tmpNode = tmp;
                }
                else
                {
                    var tmp = entry;
                    tmp.Parent = tmpNode;
                    tmpNode.InsertChild(tmp.FolderDepth - 1, tmp);
                }
            }
            entries.Add(tmpNode.Root);
            return entries;
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
        public int FolderDepth { get { return (int)flags & 0xff; } }
        public bool Localized { get { return (flags & 0x800) > 0; } }
        public bool Packed { get { return (flags & 0x400) > 0; } }
        public bool Directory { get { return (flags & 0x200) > 0; } }

        [Browsable(false)]
        public uint Flags { get { return flags; } set { flags = value; } }
        private uint flags;

        [Browsable(false)]
        public ResourceEntry Parent { get; set; }

        [Browsable(false)]
        public List<ResourceEntry> Children { get { return _children; } set { _children = value; } }
        private List<ResourceEntry> _children = new List<ResourceEntry>();

        [Browsable(false)]
        public ResourceEntry Root
        {
            get
            {
                if (Parent == null)
                    return this;
                ResourceEntry root = Parent;
                while (root.Parent != null)
                    root = root.Parent;
                return root;
            }
        }
        public string Path
        {
            get
            {
                string str = EntryString;
                ResourceEntry _entry = this;
                while ((_entry = _entry.Parent) != null)
                    str = str.Insert(0, _entry.EntryString);
                return str;
            }
        }
        public string PackFile
        {
            get
            {
                string str = Path;
                var _entry = this;
                for (; _entry != null && !_entry.Packed; _entry = _entry.Parent)
                    str = str.Remove(str.IndexOf(_entry.EntryString));
                return str + "packed";
            }
        }

        public void InsertChild(int level, ResourceEntry child)
        {
            if (level < FolderDepth)
                Parent.InsertChild(level, child);
            else if (level == FolderDepth)
                Children.Add(child);
        }

        public override string ToString()
        {
            return EntryString;
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