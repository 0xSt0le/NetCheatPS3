using System;
using System.Collections.Generic;
using System.Globalization;
using NetCheatPS3.Scanner;

namespace NetCheatPS3
{
    public partial class SearchControl
    {
        private bool TryRunFuzzyValueInitialSearch(ncSearcher searcher, ulong start, ulong stop, int typeIndex, string[] args)
        {
            if (!IsFuzzyValueChecked())
                return false;

            if (searcher.Name == null)
                return false;

            if (!String.Equals(searcher.Name, "Exact Value", StringComparison.OrdinalIgnoreCase) &&
                !String.Equals(searcher.Name, "Equal To", StringComparison.OrdinalIgnoreCase))
                return false;

            if (typeIndex < 0 || typeIndex >= SearchTypes.Count)
                return false;

            string typeName = SearchTypes[typeIndex].Name;
            bool isFloat = String.Equals(typeName, "Float", StringComparison.OrdinalIgnoreCase);
            bool isDouble = String.Equals(typeName, "Double", StringComparison.OrdinalIgnoreCase);

            if (!isFloat && !isDouble)
                return false;

            if (args == null || args.Length <= 0 || String.IsNullOrWhiteSpace(args[0]))
                return false;

            double target;
            if (!TryParseFuzzyDouble(args[0], out target))
                return false;

            int byteSize = SearchTypes[typeIndex].ByteSize;
            if (byteSize != 4 && byteSize != 8)
                return false;

            double tolerance = GetFuzzyTolerance(args[0]);
            double min = target - tolerance;
            double max = target + tolerance;

            int alignment = GetEffectiveSearchAlignment(typeIndex);
            if (alignment <= 0)
                alignment = byteSize;

            int blockSize = GetSelectedExactScanBlockSize();
            if (blockSize <= 0)
                blockSize = ExactScanner.DefaultBlockSize;

            MemoryReader reader = new MemoryReader();
            List<SearchListView.SearchListViewItem> pending = new List<SearchListView.SearchListViewItem>(1024);
            long matchCount = 0;

            ulong span = stop > start ? stop - start : 0;
            int blockCount = span == 0 ? 1 : (int)((span + (ulong)blockSize - 1UL) / (ulong)blockSize);
            if (blockCount <= 0)
                blockCount = 1;

            BeginScanProgress("Initial Scan", blockCount, "blocks");

            byte[] fullBlock = new byte[blockSize];
            EndianMode endian = activeScanLittleEndian ? EndianMode.Little : EndianMode.Big;

            int blockIndex = 0;
            for (ulong blockStart = start; blockStart < stop; blockStart += (ulong)blockSize, blockIndex++)
            {
                if (_shouldStopSearch)
                {
                    reader.PublishStats("Fuzzy Value scan stopped by user");
                    ResetScanProgress("Scan stopped");
                    return true;
                }

                ulong blockStop = blockStart + (ulong)blockSize;
                if (blockStop > stop)
                    blockStop = stop;

                int usable = (int)(blockStop - blockStart);
                if (usable <= 0)
                    break;

                byte[] readBlock = usable == blockSize ? fullBlock : new byte[usable];

                reader.ReadReadableSegments(
                    blockStart,
                    readBlock,
                    delegate(int segmentOffset, int segmentLength)
                    {
                        ScanFuzzySegment(typeIndex, byteSize, alignment, endian, min, max, blockStart, readBlock, segmentOffset, segmentLength, pending, ref matchCount);
                    });

                if (pending.Count >= 1024)
                {
                    AddResultRange(pending);
                    pending = new List<SearchListView.SearchListViewItem>(1024);
                }

                UpdateScanProgress(blockIndex + 1, matchCount);
            }

            if (pending.Count > 0)
                AddResultRange(pending);

            reader.PublishStats("Fuzzy Value scan completed");

            CompleteScanProgress(
                "Initial Scan complete | " + FormatResultCount(matchCount, matchCount) +
                " | Fuzzy range: " + min.ToString("0.###", CultureInfo.InvariantCulture) +
                "-" + max.ToString("0.###", CultureInfo.InvariantCulture) +
                " | OK " + reader.Stats.ReadSuccesses.ToString("N0") +
                "/" + reader.Stats.ReadAttempts.ToString("N0"));

            return true;
        }

        private void ScanFuzzySegment(
            int typeIndex,
            int byteSize,
            int alignment,
            EndianMode endian,
            double min,
            double max,
            ulong blockStart,
            byte[] block,
            int segmentOffset,
            int segmentLength,
            List<SearchListView.SearchListViewItem> pending,
            ref long matchCount)
        {
            if (segmentLength < byteSize)
                return;

            int firstOffset = segmentOffset;
            int rem = firstOffset % alignment;
            if (rem != 0)
                firstOffset += alignment - rem;

            int maxOffset = segmentOffset + segmentLength - byteSize;
            if (firstOffset > maxOffset)
                return;

            for (int off = firstOffset; off <= maxOffset; off += alignment)
            {
                if (_shouldStopSearch)
                    return;

                double value;
                if (!TryReadFuzzyFloatOrDouble(block, off, byteSize, endian, out value))
                    continue;

                if (Double.IsNaN(value) || Double.IsInfinity(value))
                    continue;

                if (value < min || value > max)
                    continue;

                byte[] raw = new byte[byteSize];
                Buffer.BlockCopy(block, off, raw, 0, byteSize);

                byte[] display = NormalizeRawMemoryForActiveScan(raw, typeIndex);

                if (!ShouldKeepFuzzyValue(typeIndex, display))
                    continue;

                pending.Add(SearchTypes[typeIndex].ToItem(blockStart + (ulong)off, display, display, typeIndex));
                matchCount++;
            }
        }

        private bool TryReadFuzzyFloatOrDouble(byte[] block, int offset, int byteSize, EndianMode endian, out double value)
        {
            value = 0.0;

            if (block == null || offset < 0 || offset + byteSize > block.Length)
                return false;

            byte[] tmp = new byte[byteSize];
            Buffer.BlockCopy(block, offset, tmp, 0, byteSize);

            bool rawLittle = endian == EndianMode.Little;
            if (BitConverter.IsLittleEndian != rawLittle)
                Array.Reverse(tmp);

            if (byteSize == 4)
            {
                value = BitConverter.ToSingle(tmp, 0);
                return true;
            }

            if (byteSize == 8)
            {
                value = BitConverter.ToDouble(tmp, 0);
                return true;
            }

            return false;
        }

        private bool ShouldKeepFuzzyValue(int typeIndex, byte[] display)
        {
            if (display == null)
                return false;

            if (noZeroCB != null && noZeroCB.Checked && IsFuzzyZeroBytes(display))
                return false;

            string typeName = SearchTypes[typeIndex].Name;

            if (String.Equals(typeName, "Float", StringComparison.OrdinalIgnoreCase))
            {
                float f = ReadFuzzyDisplayFloat(display);

                if (noNegativeCB != null && noNegativeCB.Checked && f < 0.0f)
                    return false;

                if (cleanFloatCB != null && cleanFloatCB.Checked && !IsFuzzySimpleValue(f))
                    return false;
            }
            else if (String.Equals(typeName, "Double", StringComparison.OrdinalIgnoreCase))
            {
                double d = ReadFuzzyDisplayDouble(display);

                if (noNegativeCB != null && noNegativeCB.Checked && d < 0.0)
                    return false;

                if (cleanFloatCB != null && cleanFloatCB.Checked && !IsFuzzySimpleValue(d))
                    return false;
            }

            return true;
        }

        private bool IsFuzzyZeroBytes(byte[] value)
        {
            if (value == null)
                return true;

            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] != 0)
                    return false;
            }

            return true;
        }

        private float ReadFuzzyDisplayFloat(byte[] value)
        {
            if (value == null || value.Length < 4)
                return 0.0f;

            byte[] tmp = new byte[4];
            Buffer.BlockCopy(value, 0, tmp, 0, 4);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(tmp);

            return BitConverter.ToSingle(tmp, 0);
        }

        private double ReadFuzzyDisplayDouble(byte[] value)
        {
            if (value == null || value.Length < 8)
                return 0.0;

            byte[] tmp = new byte[8];
            Buffer.BlockCopy(value, 0, tmp, 0, 8);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(tmp);

            return BitConverter.ToDouble(tmp, 0);
        }

        private bool IsFuzzySimpleValue(double value)
        {
            if (Double.IsNaN(value) || Double.IsInfinity(value))
                return false;

            double abs = Math.Abs(value);

            if (abs > 0.0 && abs < CleanFloatMinAbs)
                return false;

            if (abs > CleanFloatMaxAbs)
                return false;

            return true;
        }

        private bool IsFuzzyValueChecked()
        {
            return fuzzyValueCB != null && fuzzyValueCB.Visible && fuzzyValueCB.Checked;
        }

        private bool TryParseFuzzyDouble(string text, out double value)
        {
            value = 0.0;

            if (String.IsNullOrWhiteSpace(text))
                return false;

            text = text.Trim();

            if (Double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return true;

            if (Double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
                return true;

            return false;
        }

        private double GetFuzzyTolerance(string text)
        {
            int decimals = CountFuzzyDecimalPlaces(text);

            if (decimals <= 0)
                return 0.1;

            // Requested rule:
            // 70   -> fuzzy, +/- 0.1
            // 70.0 -> exact 70.0
            // 70.00 and more decimal places keep fuzzy precision behavior.
            if (decimals == 1)
                return 0.0;

            double tolerance = Math.Pow(10.0, -decimals);

            if (tolerance < 0.000001)
                tolerance = 0.000001;

            return tolerance;
        }

        private int CountFuzzyDecimalPlaces(string text)
        {
            if (String.IsNullOrWhiteSpace(text))
                return 0;

            text = text.Trim();

            int e = text.IndexOfAny(new char[] { 'e', 'E' });
            if (e >= 0)
                text = text.Substring(0, e);

            int dot = text.LastIndexOf('.');
            int comma = text.LastIndexOf(',');
            int sep = Math.Max(dot, comma);

            if (sep < 0 || sep >= text.Length - 1)
                return 0;

            int count = 0;
            for (int i = sep + 1; i < text.Length; i++)
            {
                if (Char.IsDigit(text[i]))
                    count++;
            }

            return count;
        }

        private bool FuzzyDoubleEquals(double currentValue, double target, string text)
        {
            double tolerance = GetFuzzyTolerance(text);
            return currentValue >= target - tolerance && currentValue <= target + tolerance;
        }
    }
}
