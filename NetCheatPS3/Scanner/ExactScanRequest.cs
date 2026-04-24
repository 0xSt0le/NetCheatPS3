namespace NetCheatPS3.Scanner
{
    internal sealed class ExactScanRequest
    {
        public ulong Start;
        public ulong Stop;
        public int BlockSize;
        public int ByteSize;
        public int Alignment;
        public byte[] CompareBytes;
        public EndianMode EndianMode;
        public bool KeepRawBytes;
    }
}