namespace Be.Windows.Forms {
    /// <summary>
    ///   Represents a position in the HexBox control
    /// </summary>
    internal struct BytePositionInfo {
        public BytePositionInfo(long index, int characterPosition) : this() {
            Index = index;
            CharacterPosition = characterPosition;
        }

        public int CharacterPosition { get; private set; }
        public long Index { get; private set; }
    }
}