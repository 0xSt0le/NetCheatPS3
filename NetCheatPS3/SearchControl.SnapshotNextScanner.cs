using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NetCheatPS3.Scanner;

namespace NetCheatPS3
{
    public partial class SearchControl
    {
        private enum SnapshotNextModeKind
        {
            Unsupported = 0,
            Changed,
            Unchanged,
            Increased,
            Decreased,
            IncreasedBy,
            DecreasedBy,
            Equal,
            NotEqual,
            LessThan,
            GreaterThan,
            Between
        }

        private struct SnapshotNextModeSpec
        {
            public SnapshotNextModeKind Kind;
            public bool Signed;
            public bool NeedsOneArg;
            public bool NeedsTwoArgs;
            public string DisplayName;
        }

        private bool TryRunSnapshotNextSearch(ncSearcher searcher, string[] args)
        {
            if (!HasActiveSnapshot())
                return false;

            if (searcher.Name == null)
                return false;

            if (activeSnapshotTypeIndex < 0 || activeSnapshotTypeIndex >= SearchTypes.Count)
                return false;

            if (activeSnapshotByteSize <= 0)
                return false;

            SnapshotNextModeSpec spec;
            if (!TryGetSnapshotNextModeSpec(searcher.Name, out spec))
                return false;

            if (spec.NeedsOneArg && (args == null || args.Length < 1))
                return false;

            if (spec.NeedsTwoArgs && (args == null || args.Length < 2))
                return false;

            string oldSnapshotPath = activeSnapshotPath;
            if (String.IsNullOrEmpty(oldSnapshotPath) || !File.Exists(oldSnapshotPath))
                return false;

            bool compareToFirst = IsCompareToFirstScanChecked() && HasFirstSnapshot();
            string firstPath = compareToFirst ? GetFirstSnapshotPath() : null;
            if (compareToFirst && (String.IsNullOrEmpty(firstPath) || !File.Exists(firstPath)))
                compareToFirst = false;

            ulong start;
            ulong stop;

            try
            {
                start = Convert.ToUInt64(startAddrTB.Text, 16);
                stop = Convert.ToUInt64(stopAddrTB.Text, 16);
            }
            catch
            {
                start = 0;
                stop = UInt32.MaxValue;
            }

            if (stop <= start)
                return false;

            int typeIndex = activeSnapshotTypeIndex;
            int byteSize = activeSnapshotByteSize;
            int blockSize = GetSelectedExactScanBlockSize();
            if (blockSize <= 0)
                blockSize = ExactScanner.DefaultBlockSize;

            string newSnapshotPath = CreateSnapshotTempPath();

            long matchCount = 0;
            List<SearchListView.SearchListViewItem> visible = new List<SearchListView.SearchListViewItem>(1024);

            MemoryReader reader = new MemoryReader();

            IEnumerator<SnapshotRecord> firstRecords = null;
            bool hasFirstRecord = false;

            try
            {
                if (compareToFirst)
                {
                    firstRecords = SnapshotStore.ReadRecords(firstPath).GetEnumerator();
                    hasFirstRecord = firstRecords.MoveNext();
                }

                SetProgBar(0);

                ulong span = stop - start;
                int blockCount = (int)((span + (ulong)blockSize - 1UL) / (ulong)blockSize);
                if (blockCount <= 0)
                    blockCount = 1;

                SetProgBarMax(blockCount);

                using (SnapshotStore output = SnapshotStore.Create(newSnapshotPath, typeIndex, byteSize, activeScanLittleEndian))
                using (IEnumerator<SnapshotRecord> records = SnapshotStore.ReadRecords(oldSnapshotPath).GetEnumerator())
                {
                    bool hasRecord = records.MoveNext();

                    byte[] block = new byte[blockSize];

                    for (ulong blockStart = start; blockStart < stop; blockStart += (ulong)blockSize)
                    {
                        if (_shouldStopSearch)
                        {
                            reader.PublishStats("Snapshot next scan stopped by user");
                            return true;
                        }

                        ulong blockStop = blockStart + (ulong)blockSize;
                        if (blockStop > stop)
                            blockStop = stop;

                        int usable = (int)(blockStop - blockStart);
                        byte[] readBlock = usable == blockSize ? block : new byte[usable];

                        List<SnapshotRecord> blockRecords = new List<SnapshotRecord>();

                        while (hasRecord && records.Current.Address < blockStart)
                            hasRecord = records.MoveNext();

                        while (hasRecord && records.Current.Address < blockStop)
                        {
                            blockRecords.Add(records.Current);
                            hasRecord = records.MoveNext();
                        }

                        Dictionary<ulong, byte[]> firstValues = null;
                        if (compareToFirst && blockRecords.Count > 0)
                            firstValues = CollectFirstSnapshotValuesForBlock(blockRecords, firstRecords, ref hasFirstRecord, byteSize);

                        if (blockRecords.Count > 0)
                        {
                            reader.ReadReadableSegments(
                                blockStart,
                                readBlock,
                                delegate(int segmentOffset, int segmentLength)
                                {
                                    ProcessSnapshotNextSegment(
                                        spec,
                                        args,
                                        typeIndex,
                                        byteSize,
                                        blockStart,
                                        readBlock,
                                        segmentOffset,
                                        segmentLength,
                                        blockRecords,
                                        firstValues,
                                        compareToFirst,
                                        output,
                                        visible,
                                        ref matchCount);
                                });
                        }

                        IncProgBar(1);
                    }

                    output.Complete();
                }

                reader.PublishStats(compareToFirst
                    ? "Snapshot next scan completed vs first scan: " + spec.DisplayName
                    : "Snapshot next scan completed: " + spec.DisplayName);

                activeSnapshotPath = newSnapshotPath;
                activeSnapshotTypeIndex = typeIndex;
                activeSnapshotByteSize = byteSize;
                activeSnapshotResultCount = matchCount;
                activeScanUsesNewEngine = true;

                try
                {
                    if (!String.Equals(oldSnapshotPath, newSnapshotPath, StringComparison.OrdinalIgnoreCase) &&
                        !String.Equals(oldSnapshotPath, firstSnapshotPath, StringComparison.OrdinalIgnoreCase) &&
                        File.Exists(oldSnapshotPath))
                        File.Delete(oldSnapshotPath);
                }
                catch
                {
                }

                if (visible.Count > 0)
                    AddResultRange(visible);

                double readMb = reader.Stats.BytesRead / (1024.0 * 1024.0);
                SetProgBarText(
                    "Next: " + matchCount.ToString("N0") +
                    " | Vis: " + searchListView1.TotalCount.ToString("N0") +
                    " | OK: " + reader.Stats.ReadSuccesses.ToString("N0") +
                    " | Fail: " + reader.Stats.ReadFailures.ToString("N0") +
                    " | MB: " + readMb.ToString("0.0"));

                return true;
            }
            catch (Exception ex)
            {
                CrashLogger.Log("SearchControl.TryRunSnapshotNextSearch", ex);

                try
                {
                    if (File.Exists(newSnapshotPath))
                        File.Delete(newSnapshotPath);
                }
                catch
                {
                }

                throw;
            }
            finally
            {
                if (firstRecords != null)
                    firstRecords.Dispose();
            }
        }

        private Dictionary<ulong, byte[]> CollectFirstSnapshotValuesForBlock(
            List<SnapshotRecord> blockRecords,
            IEnumerator<SnapshotRecord> firstRecords,
            ref bool hasFirstRecord,
            int byteSize)
        {
            Dictionary<ulong, byte[]> values = new Dictionary<ulong, byte[]>();

            if (blockRecords == null || firstRecords == null || !hasFirstRecord)
                return values;

            for (int i = 0; i < blockRecords.Count; i++)
            {
                ulong address = blockRecords[i].Address;

                while (hasFirstRecord && firstRecords.Current.Address < address)
                    hasFirstRecord = firstRecords.MoveNext();

                if (!hasFirstRecord)
                    break;

                if (firstRecords.Current.Address == address &&
                    firstRecords.Current.Value != null &&
                    firstRecords.Current.Value.Length == byteSize)
                {
                    values[address] = firstRecords.Current.Value;
                }
            }

            return values;
        }

        private bool IsSnapshotNextModeSupported(string mode)
        {
            SnapshotNextModeSpec spec;
            return TryGetSnapshotNextModeSpec(mode, out spec);
        }

        private bool TryGetSnapshotNextModeSpec(string mode, out SnapshotNextModeSpec spec)
        {
            spec = new SnapshotNextModeSpec();
            spec.Kind = SnapshotNextModeKind.Unsupported;
            spec.Signed = true;
            spec.NeedsOneArg = false;
            spec.NeedsTwoArgs = false;
            spec.DisplayName = "";

            if (String.IsNullOrEmpty(mode))
                return false;

            string normalized = NormalizeSnapshotModeName(mode);
            if (normalized.Length == 0)
                return false;

            if (String.Equals(normalized, "unknown value", StringComparison.OrdinalIgnoreCase))
                return false;

            if (normalized.IndexOf("pointer", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            if (normalized.IndexOf("joker", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            if (normalized.IndexOf("text", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            spec.DisplayName = mode.Trim();

            if (StartsWithMode(normalized, "changed"))
            {
                spec.Kind = SnapshotNextModeKind.Changed;
                return true;
            }

            if (StartsWithMode(normalized, "unchanged"))
            {
                spec.Kind = SnapshotNextModeKind.Unchanged;
                return true;
            }

            if (StartsWithMode(normalized, "increased by") || normalized.IndexOf("increased by", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                spec.Kind = SnapshotNextModeKind.IncreasedBy;
                spec.NeedsOneArg = true;
                return true;
            }

            if (StartsWithMode(normalized, "decreased by") || normalized.IndexOf("decreased by", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                spec.Kind = SnapshotNextModeKind.DecreasedBy;
                spec.NeedsOneArg = true;
                return true;
            }

            if (StartsWithMode(normalized, "increased"))
            {
                spec.Kind = SnapshotNextModeKind.Increased;
                return true;
            }

            if (StartsWithMode(normalized, "decreased"))
            {
                spec.Kind = SnapshotNextModeKind.Decreased;
                return true;
            }

            if (StartsWithMode(normalized, "not equal"))
            {
                spec.Kind = SnapshotNextModeKind.NotEqual;
                spec.NeedsOneArg = true;
                return true;
            }

            if (StartsWithMode(normalized, "exact value") || StartsWithMode(normalized, "equal"))
            {
                spec.Kind = SnapshotNextModeKind.Equal;
                spec.NeedsOneArg = true;
                return true;
            }

            if (StartsWithMode(normalized, "smaller than") || StartsWithMode(normalized, "less than"))
            {
                spec.Kind = SnapshotNextModeKind.LessThan;
                spec.NeedsOneArg = true;
                return true;
            }

            if (StartsWithMode(normalized, "bigger than") || StartsWithMode(normalized, "greater than"))
            {
                spec.Kind = SnapshotNextModeKind.GreaterThan;
                spec.NeedsOneArg = true;
                return true;
            }

            if (StartsWithMode(normalized, "value between"))
            {
                spec.Kind = SnapshotNextModeKind.Between;
                spec.NeedsTwoArgs = true;
                return true;
            }

            return false;
        }

        private string NormalizeSnapshotModeName(string mode)
        {
            if (mode == null)
                return "";

            string trimmed = mode.Trim().ToLowerInvariant();
            while (trimmed.IndexOf("  ", StringComparison.Ordinal) >= 0)
                trimmed = trimmed.Replace("  ", " ");

            return trimmed;
        }

        private bool StartsWithMode(string normalizedMode, string prefix)
        {
            if (normalizedMode == null || prefix == null)
                return false;

            if (!normalizedMode.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;

            if (normalizedMode.Length == prefix.Length)
                return true;

            char next = normalizedMode[prefix.Length];
            return next == ' ' || next == '(' || next == '-' || next == ':';
        }

        private string CreateSnapshotTempPath()
        {
            string dir = Path.Combine(Path.GetTempPath(), "NetCheatPS3");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "snapshot_" + Guid.NewGuid().ToString("N") + ".ncs");
        }

        private void ProcessSnapshotNextSegment(
            SnapshotNextModeSpec spec,
            string[] args,
            int typeIndex,
            int byteSize,
            ulong blockStart,
            byte[] readBlock,
            int segmentOffset,
            int segmentLength,
            List<SnapshotRecord> blockRecords,
            Dictionary<ulong, byte[]> firstValues,
            bool compareToFirst,
            SnapshotStore output,
            List<SearchListView.SearchListViewItem> visible,
            ref long matchCount)
        {
            ulong segmentStart = blockStart + (ulong)segmentOffset;
            ulong segmentStop = segmentStart + (ulong)segmentLength;

            for (int i = 0; i < blockRecords.Count; i++)
            {
                SnapshotRecord record = blockRecords[i];

                if (record.Address < segmentStart || record.Address + (ulong)byteSize > segmentStop)
                    continue;

                int offset = (int)(record.Address - blockStart);
                if (offset < 0 || offset + byteSize > readBlock.Length)
                    continue;

                byte[] currentValue = new byte[byteSize];
                Buffer.BlockCopy(readBlock, offset, currentValue, 0, byteSize);
                currentValue = NormalizeRawMemoryForActiveScan(currentValue, typeIndex);

                byte[] baselineValue = record.Value;

                if (compareToFirst)
                {
                    if (firstValues == null || !firstValues.TryGetValue(record.Address, out baselineValue))
                        continue;
                }

                if (baselineValue == null || baselineValue.Length != byteSize)
                    continue;

                if (!SnapshotNextMatches(spec, args, typeIndex, baselineValue, currentValue))
                    continue;

                if (!ShouldKeepSnapshotResult(typeIndex, currentValue))
                    continue;

                output.WriteRecord(record.Address, currentValue);
                matchCount++;

                if (visible.Count < MaxVisibleSnapshotResults)
                    visible.Add(SearchTypes[typeIndex].ToItem(record.Address, currentValue, baselineValue, typeIndex));
            }
        }

        private bool ShouldKeepSnapshotResult(int typeIndex, byte[] value)
        {
            if (value == null)
                return false;

            string typeName = SearchTypes[typeIndex].Name;

            if (activeFilterNoZero && IsZeroValue(value))
                return false;

            if (String.Equals(typeName, "Float", StringComparison.OrdinalIgnoreCase))
            {
                float f = ReadFloatDisplay(value);
                if (activeFilterNoNegative && f < 0.0f)
                    return false;

                if (activeFilterCleanFloat && !IsCleanFloatValue(f))
                    return false;
            }
            else if (String.Equals(typeName, "Double", StringComparison.OrdinalIgnoreCase))
            {
                double d = ReadDoubleDisplay(value);
                if (activeFilterNoNegative && d < 0.0)
                    return false;

                if (activeFilterCleanFloat && !IsCleanDoubleValue(d))
                    return false;
            }
            else if (activeFilterNoNegative && IsSignedNegative(value))
            {
                return false;
            }

            return true;
        }

        private bool SnapshotNextMatches(SnapshotNextModeSpec spec, string[] args, int typeIndex, byte[] oldValue, byte[] currentValue)
        {
            switch (spec.Kind)
            {
                case SnapshotNextModeKind.Changed:
                    return !BytesEqual(oldValue, currentValue);

                case SnapshotNextModeKind.Unchanged:
                    return BytesEqual(oldValue, currentValue);
            }

            string typeName = SearchTypes[typeIndex].Name;

            if (String.Equals(typeName, "Float", StringComparison.OrdinalIgnoreCase))
                return SnapshotNextMatchesDouble(spec, args, ReadFloatDisplay(oldValue), ReadFloatDisplay(currentValue));

            if (String.Equals(typeName, "Double", StringComparison.OrdinalIgnoreCase))
                return SnapshotNextMatchesDouble(spec, args, ReadDoubleDisplay(oldValue), ReadDoubleDisplay(currentValue));

            long oldSigned = ReadSignedDisplay(oldValue);
            long currentSigned = ReadSignedDisplay(currentValue);

            switch (spec.Kind)
            {
                case SnapshotNextModeKind.Increased:
                    return currentSigned > oldSigned;

                case SnapshotNextModeKind.Decreased:
                    return currentSigned < oldSigned;

                case SnapshotNextModeKind.IncreasedBy:
                    return currentSigned == oldSigned + ParseArgSigned(args, typeIndex, 0);

                case SnapshotNextModeKind.DecreasedBy:
                    return currentSigned == oldSigned - ParseArgSigned(args, typeIndex, 0);

                case SnapshotNextModeKind.Equal:
                    return currentSigned == ParseArgSigned(args, typeIndex, 0);

                case SnapshotNextModeKind.NotEqual:
                    return currentSigned != ParseArgSigned(args, typeIndex, 0);

                case SnapshotNextModeKind.LessThan:
                    return currentSigned < ParseArgSigned(args, typeIndex, 0);

                case SnapshotNextModeKind.GreaterThan:
                    return currentSigned > ParseArgSigned(args, typeIndex, 0);

                case SnapshotNextModeKind.Between:
                    long minSigned = ParseArgSigned(args, typeIndex, 0);
                    long maxSigned = ParseArgSigned(args, typeIndex, 1);
                    return currentSigned >= minSigned && currentSigned <= maxSigned;
            }

            return false;
        }

        private bool SnapshotNextMatchesDouble(SnapshotNextModeSpec spec, string[] args, double oldValue, double currentValue)
        {
            switch (spec.Kind)
            {
                case SnapshotNextModeKind.Increased:
                    return currentValue > oldValue;

                case SnapshotNextModeKind.Decreased:
                    return currentValue < oldValue;

                case SnapshotNextModeKind.IncreasedBy:
                    double increasedExpected = oldValue + ParseArgDouble(args, 0);
                    return IsFuzzyValueChecked() ? FuzzyDoubleEquals(currentValue, increasedExpected, args == null || args.Length == 0 ? null : args[0]) : currentValue == increasedExpected;

                case SnapshotNextModeKind.DecreasedBy:
                    double decreasedExpected = oldValue - ParseArgDouble(args, 0);
                    return IsFuzzyValueChecked() ? FuzzyDoubleEquals(currentValue, decreasedExpected, args == null || args.Length == 0 ? null : args[0]) : currentValue == decreasedExpected;

                case SnapshotNextModeKind.Equal:
                    double exactValue = ParseArgDouble(args, 0);
                    return IsFuzzyValueChecked() ? FuzzyDoubleEquals(currentValue, exactValue, args == null || args.Length == 0 ? null : args[0]) : currentValue == exactValue;

                case SnapshotNextModeKind.NotEqual:
                    double notEqualValue = ParseArgDouble(args, 0);
                    return IsFuzzyValueChecked() ? !FuzzyDoubleEquals(currentValue, notEqualValue, args == null || args.Length == 0 ? null : args[0]) : currentValue != notEqualValue;

                case SnapshotNextModeKind.LessThan:
                    return currentValue < ParseArgDouble(args, 0);

                case SnapshotNextModeKind.GreaterThan:
                    return currentValue > ParseArgDouble(args, 0);

                case SnapshotNextModeKind.Between:
                    return currentValue >= ParseArgDouble(args, 0) && currentValue <= ParseArgDouble(args, 1);
            }

            return false;
        }

        private long ParseArgSigned(string[] args, int typeIndex, int index)
        {
            byte[] bytes = ParseArgBytes(args, typeIndex, index);
            return ReadSignedDisplay(bytes);
        }

        private double ParseArgDouble(string[] args, int index)
        {
            if (args == null || index < 0 || index >= args.Length || args[index] == null)
                return 0.0;

            double value;
            string text = args[index].Trim();

            if (Double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return value;

            if (Double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
                return value;

            return 0.0;
        }

        private byte[] ParseArgBytes(string[] args, int typeIndex, int index)
        {
            int byteSize = activeSnapshotByteSize > 0 ? activeSnapshotByteSize : SearchTypes[typeIndex].ByteSize;
            byte[] fallback = new byte[byteSize];

            if (args == null || index < 0 || index >= args.Length || args[index] == null)
                return fallback;

            bool oldHex = Form1.ValHex;
            try
            {
                Form1.ValHex = false;
                byte[] parsed = SearchTypes[typeIndex].ToByteArray(args[index]);

                if (parsed == null || parsed.Length == 0)
                    return fallback;

                if (parsed.Length == byteSize)
                    return parsed;

                byte[] normalized = new byte[byteSize];

                if (parsed.Length > byteSize)
                    Buffer.BlockCopy(parsed, parsed.Length - byteSize, normalized, 0, byteSize);
                else
                    Buffer.BlockCopy(parsed, 0, normalized, byteSize - parsed.Length, parsed.Length);

                return normalized;
            }
            catch
            {
                return fallback;
            }
            finally
            {
                Form1.ValHex = oldHex;
            }
        }

        private long ReadSignedDisplay(byte[] value)
        {
            if (value == null || value.Length == 0)
                return 0;

            int len = Math.Min(value.Length, 8);
            ulong unsigned = 0;

            for (int i = 0; i < len; i++)
                unsigned = (unsigned << 8) | value[i];

            if (len >= 8)
                return (long)unsigned;

            int bits = len * 8;
            ulong signBit = 1UL << (bits - 1);

            if ((unsigned & signBit) == 0)
                return (long)unsigned;

            ulong mask = UInt64.MaxValue << bits;
            return (long)(unsigned | mask);
        }

        private float ReadFloatDisplay(byte[] value)
        {
            if (value == null || value.Length < 4)
                return 0.0f;

            byte[] tmp = new byte[4];
            Buffer.BlockCopy(value, 0, tmp, 0, 4);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(tmp);

            return BitConverter.ToSingle(tmp, 0);
        }

        private double ReadDoubleDisplay(byte[] value)
        {
            if (value == null || value.Length < 8)
                return 0.0;

            byte[] tmp = new byte[8];
            Buffer.BlockCopy(value, 0, tmp, 0, 8);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(tmp);

            return BitConverter.ToDouble(tmp, 0);
        }

        private bool IsZeroValue(byte[] value)
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

        private bool IsSignedNegative(byte[] value)
        {
            if (value == null || value.Length == 0)
                return false;

            return (value[0] & 0x80) != 0;
        }

        private bool IsCleanFloatValue(float value)
        {
            if (Single.IsNaN(value) || Single.IsInfinity(value))
                return false;

            float abs = Math.Abs(value);
            if (abs > 0.0f && abs < (float)CleanFloatMinAbs)
                return false;

            if (abs > (float)CleanFloatMaxAbs)
                return false;

            return true;
        }

        private bool IsCleanDoubleValue(double value)
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

        private bool BytesEqual(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                    return false;
            }

            return true;
        }
    }
}