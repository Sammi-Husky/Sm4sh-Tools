namespace Be.Windows.Forms {
    internal abstract class DataBlock {
        public abstract long Length { get; }
        public DataMap Map { get; internal set; }
        public DataBlock NextBlock { get; internal set; }
        public DataBlock PreviousBlock { get; internal set; }
        public abstract void RemoveBytes(long position, long count);
    }
}