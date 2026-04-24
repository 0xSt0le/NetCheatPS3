using System;

namespace NetCheatPS3.Scanner
{
    internal sealed class MemoryReader
    {
        private const int RetryCount = 2;

        public MemoryReadStats Stats { get; private set; }

        public MemoryReader()
        {
            Stats = new MemoryReadStats();
        }

        public bool TryReadBlock(ulong addr, byte[] buffer)
        {
            if (buffer == null || buffer.Length == 0)
                return false;

            for (int attempt = 0; attempt < RetryCount; attempt++)
            {
                Stats.ReadAttempts++;

                byte[] readBuffer = buffer;
                bool ok = false;

                try
                {
                    ok = Form1.apiGetMem(addr, ref readBuffer);
                }
                catch
                {
                    ok = false;
                }

                if (ok)
                {
                    if (!object.ReferenceEquals(readBuffer, buffer))
                    {
                        int count = Math.Min(buffer.Length, readBuffer.Length);
                        Buffer.BlockCopy(readBuffer, 0, buffer, 0, count);
                    }

                    Stats.ReadSuccesses++;
                    return true;
                }

                Stats.ReadFailures++;
            }

            return false;
        }
    }
}