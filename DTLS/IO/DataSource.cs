//=========================================================================\\
// Taken from BrawlLib's source code, credit goes to devs who worked on it.\\
//          (Kryal, Bero, BlackJax96, LibertyErnie, Sammi Husky)           \\
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
            Address = addr;
            Length = len;
            Map = null;
        }
        public DataSource(FileMap map)
        {
            Address = map.Address;
            Length = map.Length;
            Map = map;
        }

        public void Close()
        {
            if (Map != null) { Map.Dispose(); Map = null; }
            Address = null;
            Length = 0;
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

        public byte[] ToArray()
        {
            byte[] b = new byte[Length];
            for (int i = 0; i < b.Length; i++)
                b[i] = *(byte*)(Address + i);
            return b;
        }

        public byte[] Slice(int start, int len)
        {
            if (len > Length | start > Length)
                return null;

            byte[] b = new byte[len];
            for (int i = 0; i < b.Length; i++)
                b[i] = *(byte*)(Address + start + i);

            return b;
        }
    }

}
