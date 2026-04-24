namespace NetCheatPS3.Scanner
{
    internal sealed class SnapshotHeader
    {
        public int Version;
        public int TypeIndex;
        public int ByteSize;
        public bool LittleEndian;
        public long Count;
    }
}