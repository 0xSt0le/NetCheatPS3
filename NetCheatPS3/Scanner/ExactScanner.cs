using System;

namespace NetCheatPS3.Scanner
{
    internal static class ExactScanner
    {
                public static int DefaultBlockSize
        {
            get
            {
                try
                {
                    if (NetCheatPS3.SearchControl.Instance != null)
                        return NetCheatPS3.SearchControl.Instance.GetSelectedExactScanBlockSize();
                }
                catch
                {
                }

                return 0x40000;
            }
        }

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
                {
                    reader.PublishStats("Exact scan stopped by user");
                    return;
                }

                int usable = (int)Math.Min((ulong)request.BlockSize, request.Stop - addr);
                if (usable <= 0)
                    break;

                byte[] readBlock = usable == request.BlockSize ? fullBlock : new byte[usable];

                reader.ReadReadableSegments(
                    addr,
                    readBlock,
                    delegate(int segmentOffset, int segmentLength)
                    {
                        ScanReadableSegment(
                            request,
                            compare,
                            readBlock,
                            addr,
                            segmentOffset,
                            segmentLength,
                            shouldStop,
                            onMatch);
                    });

                if (onBlockScanned != null)
                    onBlockScanned(blockIndex);
            }

            reader.PublishStats("Exact scan completed");
        }

        private static void ScanReadableSegment(
            ExactScanRequest request,
            byte[] compare,
            byte[] block,
            ulong blockAddress,
            int segmentOffset,
            int segmentLength,
            Func<bool> shouldStop,
            Action<ulong, byte[]> onMatch)
        {
            if (segmentLength < request.ByteSize)
                return;

            int firstOffset = segmentOffset;
            int alignmentRemainder = firstOffset % request.Alignment;
            if (alignmentRemainder != 0)
                firstOffset += request.Alignment - alignmentRemainder;

            int maxOffset = segmentOffset + segmentLength - request.ByteSize;
            if (firstOffset > maxOffset)
                return;

            for (int off = firstOffset; off <= maxOffset; off += request.Alignment)
            {
                if (shouldStop != null && shouldStop())
                    return;

                if (IsMatch(block, off, compare))
                {
                    byte[] hit = new byte[request.ByteSize];
                    Buffer.BlockCopy(block, off, hit, 0, request.ByteSize);
                    NormalizeDisplayBytes(hit, request.EndianMode, request.ByteSize, request.KeepRawBytes);

                    if (onMatch != null)
                        onMatch(blockAddress + (ulong)off, hit);
                }
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