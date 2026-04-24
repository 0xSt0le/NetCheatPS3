using System;

namespace NetCheatPS3.Scanner
{
    internal sealed class MemoryReader
    {
        private const int RetryCount = 2;
        private const int FallbackRetryCount = 1;
        private const int DefaultMinFallbackSegmentSize = 0x1000;

        public MemoryReadStats Stats { get; private set; }

        public bool EnableSegmentFallback { get; set; }
        public int MinFallbackSegmentSize { get; set; }

        public MemoryReader()
        {
            Stats = new MemoryReadStats();
            EnableSegmentFallback = true;
            MinFallbackSegmentSize = DefaultMinFallbackSegmentSize;
        }

        public bool TryReadBlock(ulong addr, byte[] buffer)
        {
            if (buffer == null || buffer.Length == 0)
                return false;

            Stats.FullBlockFailures++;

            if (TryReadRaw(addr, buffer, 0, buffer.Length, RetryCount))
            {
                Stats.FullBlockFailures--;
                Stats.FullBlockSuccesses++;
                return true;
            }

            return false;
        }

        public int ReadReadableSegments(ulong addr, byte[] buffer, Action<int, int> onSegment)
        {
            if (buffer == null || buffer.Length == 0)
                return 0;

            if (onSegment == null)
                throw new ArgumentNullException("onSegment");

            if (TryReadBlock(addr, buffer))
            {
                onSegment(0, buffer.Length);
                return 1;
            }

            if (!EnableSegmentFallback)
                return 0;

            int minSegment = MinFallbackSegmentSize;
            if (minSegment <= 0)
                minSegment = DefaultMinFallbackSegmentSize;

            int recoveredSegments = ReadSegmentsBySplitting(addr, buffer, 0, buffer.Length, minSegment, onSegment);
            if (recoveredSegments > 0)
                Stats.PartialBlocksRecovered++;

            return recoveredSegments;
        }

        private int ReadSegmentsBySplitting(ulong baseAddr, byte[] buffer, int offset, int count, int minSegment, Action<int, int> onSegment)
        {
            if (count <= 0)
                return 0;

            if (count <= minSegment)
            {
                if (TryReadRaw(baseAddr + (ulong)offset, buffer, offset, count, FallbackRetryCount))
                {
                    Stats.FallbackSegmentSuccesses++;
                    onSegment(offset, count);
                    return 1;
                }

                Stats.FallbackSegmentFailures++;
                return 0;
            }

            // Try the current smaller segment once before splitting again.
            // The original full block was already tried by TryReadBlock(), so this mainly
            // catches cases where a 64 KB read fails but a 32 KB/16 KB read succeeds.
            if (count < buffer.Length && TryReadRaw(baseAddr + (ulong)offset, buffer, offset, count, FallbackRetryCount))
            {
                Stats.FallbackSegmentSuccesses++;
                onSegment(offset, count);
                return 1;
            }

            Stats.FallbackSplits++;

            int left = count / 2;
            int right = count - left;

            int recovered = 0;
            recovered += ReadSegmentsBySplitting(baseAddr, buffer, offset, left, minSegment, onSegment);
            recovered += ReadSegmentsBySplitting(baseAddr, buffer, offset + left, right, minSegment, onSegment);

            return recovered;
        }

        private bool TryReadRaw(ulong addr, byte[] destination, int destinationOffset, int count, int attempts)
        {
            if (destination == null || count <= 0 || destinationOffset < 0 || (destinationOffset + count) > destination.Length)
                return false;

            if (attempts <= 0)
                attempts = 1;

            Stats.BytesRequested += count;

            for (int attempt = 0; attempt < attempts; attempt++)
            {
                Stats.ReadAttempts++;

                byte[] readBuffer;

                if (destinationOffset == 0 && count == destination.Length)
                    readBuffer = destination;
                else
                    readBuffer = new byte[count];

                bool ok = false;

                try
                {
                    ok = Form1.apiGetMem(addr, ref readBuffer);
                }
                catch
                {
                    ok = false;
                }

                if (ok && readBuffer != null && readBuffer.Length >= count)
                {
                    if (!object.ReferenceEquals(readBuffer, destination) || destinationOffset != 0 || count != destination.Length)
                        Buffer.BlockCopy(readBuffer, 0, destination, destinationOffset, count);

                    Stats.ReadSuccesses++;
                    Stats.BytesRead += count;
                    return true;
                }

                Stats.ReadFailures++;
            }

            return false;
        }
    }
}