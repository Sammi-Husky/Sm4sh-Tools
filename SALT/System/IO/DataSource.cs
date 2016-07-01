//=========================================================================\\
// Taken from BrawlLib's source code, credit goes to devs who worked on it.\\
//          (Kryal, Bero, BlackJax96, LibertyErnie, Sammi Husky)           \\
//              My deepest apologies to anyone who i've missed             \\
//=========================================================================\\
namespace System.IO
{
    public unsafe struct DataSource
    {
        public static readonly DataSource Empty = new DataSource();

        public VoidPtr Address;
        public int Length;
        public FileMap Map;

        public DataSource(VoidPtr addr, int len)
        {
            this.Address = addr;
            this.Length = len;
            this.Map = null;
        }

        public DataSource(FileMap map)
        {
            this.Address = map.Address;
            this.Length = map.Length;
            this.Map = map;
        }
        public DataSource(byte[] data)
        {
            fixed(byte* psrc = data)
            {
                this.Address = psrc;
                this.Length = data.Length;
                this.Map = null;
            }
        }

        public void Close()
        {
            if (this.Map != null) { this.Map.Dispose(); this.Map = null; }
            this.Address = null;
            this.Length = 0;
        }

        public byte[] ToArray()
        {
            var bytes = new byte[this.Length];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = *(byte*)(Address + i);
            return bytes;
        }
        public byte[] Slice(int start, int len)
        {
            if (start > this.Length | start < 0)
                throw new IndexOutOfRangeException();

            var bytes = new byte[len];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = *(byte*)(Address + start + i);
            return bytes;
        }

        public static bool operator ==(DataSource src1, DataSource src2) { return (src1.Address == src2.Address) && (src1.Length == src2.Length) && (src1.Map == src2.Map); }
        public static bool operator !=(DataSource src1, DataSource src2) { return (src1.Address != src2.Address) || (src1.Length != src2.Length) || (src1.Map != src2.Map); }
        public override bool Equals(object obj)
        {
            if (obj is DataSource)
                return this == (DataSource)obj;
            return base.Equals(obj);
        }

        public override int GetHashCode() { return base.GetHashCode(); }
    }
}
