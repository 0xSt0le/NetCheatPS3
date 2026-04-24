namespace NetCheatPS3.Scanner
{
    internal sealed class MemoryReadStats
    {
        public long ReadAttempts;
        public long ReadSuccesses;
        public long ReadFailures;

        public long BytesRequested;
        public long BytesRead;

        public long FullBlockSuccesses;
        public long FullBlockFailures;

        public long FallbackSplits;
        public long FallbackSegmentSuccesses;
        public long FallbackSegmentFailures;
        public long PartialBlocksRecovered;

        public void Add(MemoryReadStats other)
        {
            if (other == null)
                return;

            ReadAttempts += other.ReadAttempts;
            ReadSuccesses += other.ReadSuccesses;
            ReadFailures += other.ReadFailures;
            BytesRequested += other.BytesRequested;
            BytesRead += other.BytesRead;
            FullBlockSuccesses += other.FullBlockSuccesses;
            FullBlockFailures += other.FullBlockFailures;
            FallbackSplits += other.FallbackSplits;
            FallbackSegmentSuccesses += other.FallbackSegmentSuccesses;
            FallbackSegmentFailures += other.FallbackSegmentFailures;
            PartialBlocksRecovered += other.PartialBlocksRecovered;
        }

        public MemoryReadStats Clone()
        {
            MemoryReadStats clone = new MemoryReadStats();
            clone.Add(this);
            return clone;
        }

        public override string ToString()
        {
            return "Attempts: " + ReadAttempts.ToString("N0") +
                   " | OK: " + ReadSuccesses.ToString("N0") +
                   " | Failed: " + ReadFailures.ToString("N0") +
                   " | Bytes OK: " + BytesRead.ToString("N0") +
                   " | Fallback splits: " + FallbackSplits.ToString("N0") +
                   " | Partial recovered: " + PartialBlocksRecovered.ToString("N0");
        }
    }
}