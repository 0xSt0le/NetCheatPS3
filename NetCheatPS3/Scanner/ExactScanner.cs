using System;

namespace NetCheatPS3.Scanner
{
    internal static class ExactScanner
    {
        public const int DefaultBlockSize = 0x10000;

        public static void Scan(
    ExactScanRequest request,
    MemoryReader reader,
    Func<bool> shouldStop,
    Action<int> onBlockScanned,
    Action<ulong, byte[]> onMatch)
        {
            if (request == null)
                throw new ArgumentNullException("request");
            if (reader == null)
                throw new ArgumentNullException("reader");
            if (request.CompareBytes == null)
                throw new ArgumentNullException("request.CompareBytes");
            if (request.ByteSize <= 0)
                throw new ArgumentOutOfRangeException("request.ByteSize");
            if (request.Alignment <= 0)
                throw new ArgumentOutOfRangeException("request.Alignment");
            if (request.BlockSize <= 0)
                throw new ArgumentOutOfRangeException("request.BlockSize");

            byte[] compare = PrepareCompareBytes(
                request.CompareBytes,
                request.EndianMode,
                request.ByteSize,
                request.KeepRawBytes);

            byte[] fullBlock = new byte[request.BlockSize];
            int blockIndex = 0;

            for (ulong addr = request.Start; addr < request.Stop; addr += (ulong)request.BlockSize, blockIndex++)
            {
                if (shouldStop != null && shouldStop())
                    return;

                int usable = (int)Math.Min((ulong)request.BlockSize, request.Stop - addr);
                if (usable <= 0)
                    break;

                byte[] readBlock = usable == request.BlockSize ? fullBlock : new byte[usable];

                if (reader.TryReadBlock(addr, readBlock))
                {
                    int maxOffset = usable - request.ByteSize;
                    if (maxOffset >= 0)
                    {
                        for (int off = 0; off <= maxOffset; off += request.Alignment)
                        {
                            if (shouldStop != null && shouldStop())
                                return;

                            if (IsMatch(readBlock, off, compare))
                            {
                                byte[] hit = new byte[request.ByteSize];
                                Buffer.BlockCopy(readBlock, off, hit, 0, request.ByteSize);
                                NormalizeDisplayBytes(hit, request.EndianMode, request.ByteSize, request.KeepRawBytes);

                                if (onMatch != null)
                                    onMatch(addr + (ulong)off, hit);
                            }
                        }
                    }
                }

                if (onBlockScanned != null)
                    onBlockScanned(blockIndex);
            }
        }

        private static byte[] PrepareCompareBytes(byte[] input, EndianMode endianMode, int byteSize, bool keepRawBytes)
        {
            byte[] output = new byte[byteSize];
            Buffer.BlockCopy(input, 0, output, 0, byteSize);

            if (!keepRawBytes && byteSize > 1 && endianMode == EndianMode.Little)
                Array.Reverse(output);

            return output;
        }

        private static void NormalizeDisplayBytes(byte[] bytes, EndianMode endianMode, int byteSize, bool keepRawBytes)
        {
            if (!keepRawBytes && byteSize > 1 && endianMode == EndianMode.Little)
                Array.Reverse(bytes);
        }

        private static bool IsMatch(byte[] block, int offset, byte[] compare)
        {
            for (int i = 0; i < compare.Length; i++)
            {
                if (block[offset + i] != compare[i])
                    return false;
            }

            return true;
        }
    }
}