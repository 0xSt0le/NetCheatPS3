using System;
using System.Collections.Generic;
using System.IO;
using NetCheatPS3.Scanner;

namespace NetCheatPS3
{
    public partial class SearchControl
    {
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

            string mode = searcher.Name.Trim();

            if (!IsSnapshotNextModeSupported(mode))
                return false;

            string oldSnapshotPath = activeSnapshotPath;
            if (String.IsNullOrEmpty(oldSnapshotPath) || !File.Exists(oldSnapshotPath))
                return false;

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

            try
            {
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

                        if (blockRecords.Count > 0)
                        {
                            reader.ReadReadableSegments(
                                blockStart,
                                readBlock,
                                delegate(int segmentOffset, int segmentLength)
                                {
                                    ProcessSnapshotNextSegment(
                                        mode,
                                        args,
                                        typeIndex,
                                        byteSize,
                                        blockStart,
                                        readBlock,
                                        segmentOffset,
                                        segmentLength,
                                        blockRecords,
                                        output,
                                        visible,
                                        ref matchCount);
                                });
                        }

                        IncProgBar(1);
                    }

                    output.Complete();
                }

                reader.PublishStats("Snapshot next scan completed");

                activeSnapshotPath = newSnapshotPath;
                activeSnapshotTypeIndex = typeIndex;
                activeSnapshotByteSize = byteSize;
                activeSnapshotResultCount = matchCount;
                activeScanUsesNewEngine = true;

                try
                {
                    if (!String.Equals(oldSnapshotPath, newSnapshotPath, StringComparison.OrdinalIgnoreCase) && File.Exists(oldSnapshotPath))
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
        }

        private bool IsSnapshotNextModeSupported(string mode)
        {
            if (String.IsNullOrEmpty(mode))
                return false;

            if (String.Equals(mode, "Pointer", StringComparison.OrdinalIgnoreCase))
                return false;

            if (mode.IndexOf("Text", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            if (String.Equals(mode, "Unknown Value", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        private string CreateSnapshotTempPath()
        {
            string dir = Path.Combine(Path.GetTempPath(), "NetCheatPS3");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "snapshot_" + Guid.NewGuid().ToString("N") + ".ncs");
        }

        private void ProcessSnapshotNextSegment(
            string mode,
            string[] args,
            int typeIndex,
            int byteSize,
            ulong blockStart,
            byte[] readBlock,
            int segmentOffset,
            int segmentLength,
            List<SnapshotRecord> blockRecords,
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

                byte[] oldValue = record.Value;

                if (oldValue == null || oldValue.Length != byteSize)
                    continue;

                if (!SnapshotNextMatches(mode, args, typeIndex, oldValue, currentValue))
                    continue;

                if (!ShouldKeepSnapshotResult(typeIndex, currentValue))
                    continue;

                output.WriteRecord(record.Address, currentValue);
                matchCount++;

                if (visible.Count < MaxVisibleSnapshotResults)
                    visible.Add(SearchTypes[typeIndex].ToItem(record.Address, currentValue, oldValue, typeIndex));
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

        private bool SnapshotNextMatches(string mode, string[] args, int typeIndex, byte[] oldValue, byte[] currentValue)
        {
            string typeName = SearchTypes[typeIndex].Name;

            if (String.Equals(typeName, "Float", StringComparison.OrdinalIgnoreCase))
                return SnapshotNextMatchesDouble(mode, args, ReadFloatDisplay(oldValue), ReadFloatDisplay(currentValue));

            if (String.Equals(typeName, "Double", StringComparison.OrdinalIgnoreCase))
                return SnapshotNextMatchesDouble(mode, args, ReadDoubleDisplay(oldValue), ReadDoubleDisplay(currentValue));

            ulong oldUnsigned = ReadUnsignedDisplay(oldValue);
            ulong currentUnsigned = ReadUnsignedDisplay(currentValue);
            long oldSigned = ReadSignedDisplay(oldValue);
            long currentSigned = ReadSignedDisplay(currentValue);

            bool signed = mode.IndexOf("(S)", StringComparison.OrdinalIgnoreCase) >= 0;

            if (mode.IndexOf("Changed", StringComparison.OrdinalIgnoreCase) >= 0 && mode.IndexOf("Unchanged", StringComparison.OrdinalIgnoreCase) < 0)
                return !BytesEqual(oldValue, currentValue);

            if (mode.IndexOf("Unchanged", StringComparison.OrdinalIgnoreCase) >= 0)
                return BytesEqual(oldValue, currentValue);

            if (mode.IndexOf("Increased", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (mode.IndexOf("By", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    ulong deltaUnsigned = ParseArgUnsigned(args, typeIndex, 0);
                    long deltaSigned = ParseArgSigned(args, typeIndex, 0);

                    if (signed)
                        return currentSigned == oldSigned + deltaSigned;

                    return currentUnsigned == oldUnsigned + deltaUnsigned;
                }

                return signed ? currentSigned > oldSigned : currentUnsigned > oldUnsigned;
            }

            if (mode.IndexOf("Decreased", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (mode.IndexOf("By", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    ulong deltaUnsigned = ParseArgUnsigned(args, typeIndex, 0);
                    long deltaSigned = ParseArgSigned(args, typeIndex, 0);

                    if (signed)
                        return currentSigned == oldSigned - deltaSigned;

                    return currentUnsigned == oldUnsigned - deltaUnsigned;
                }

                return signed ? currentSigned < oldSigned : currentUnsigned < oldUnsigned;
            }

            if (mode.IndexOf("Equal To", StringComparison.OrdinalIgnoreCase) >= 0 && mode.IndexOf("Not Equal", StringComparison.OrdinalIgnoreCase) < 0)
                return currentUnsigned == ParseArgUnsigned(args, typeIndex, 0);

            if (mode.IndexOf("Not Equal", StringComparison.OrdinalIgnoreCase) >= 0)
                return currentUnsigned != ParseArgUnsigned(args, typeIndex, 0);

            if (mode.IndexOf("Less Than or Equal", StringComparison.OrdinalIgnoreCase) >= 0)
                return signed ? currentSigned <= ParseArgSigned(args, typeIndex, 0) : currentUnsigned <= ParseArgUnsigned(args, typeIndex, 0);

            if (mode.IndexOf("Less Than", StringComparison.OrdinalIgnoreCase) >= 0)
                return signed ? currentSigned < ParseArgSigned(args, typeIndex, 0) : currentUnsigned < ParseArgUnsigned(args, typeIndex, 0);

            if (mode.IndexOf("Greater Than or Equal", StringComparison.OrdinalIgnoreCase) >= 0)
                return signed ? currentSigned >= ParseArgSigned(args, typeIndex, 0) : currentUnsigned >= ParseArgUnsigned(args, typeIndex, 0);

            if (mode.IndexOf("Greater Than", StringComparison.OrdinalIgnoreCase) >= 0)
                return signed ? currentSigned > ParseArgSigned(args, typeIndex, 0) : currentUnsigned > ParseArgUnsigned(args, typeIndex, 0);

            if (mode.IndexOf("Value Between", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ulong min = ParseArgUnsigned(args, typeIndex, 0);
                ulong max = ParseArgUnsigned(args, typeIndex, 1);
                return currentUnsigned >= min && currentUnsigned <= max;
            }

            return false;
        }

        private bool SnapshotNextMatchesDouble(string mode, string[] args, double oldValue, double currentValue)
        {
            if (mode.IndexOf("Changed", StringComparison.OrdinalIgnoreCase) >= 0 && mode.IndexOf("Unchanged", StringComparison.OrdinalIgnoreCase) < 0)
                return currentValue != oldValue;

            if (mode.IndexOf("Unchanged", StringComparison.OrdinalIgnoreCase) >= 0)
                return currentValue == oldValue;

            if (mode.IndexOf("Increased", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (mode.IndexOf("By", StringComparison.OrdinalIgnoreCase) >= 0)
                    return currentValue == oldValue + ParseArgDouble(args, 0);

                return currentValue > oldValue;
            }

            if (mode.IndexOf("Decreased", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (mode.IndexOf("By", StringComparison.OrdinalIgnoreCase) >= 0)
                    return currentValue == oldValue - ParseArgDouble(args, 0);

                return currentValue < oldValue;
            }

            if (mode.IndexOf("Equal To", StringComparison.OrdinalIgnoreCase) >= 0 && mode.IndexOf("Not Equal", StringComparison.OrdinalIgnoreCase) < 0)
                return currentValue == ParseArgDouble(args, 0);

            if (mode.IndexOf("Not Equal", StringComparison.OrdinalIgnoreCase) >= 0)
                return currentValue != ParseArgDouble(args, 0);

            if (mode.IndexOf("Less Than or Equal", StringComparison.OrdinalIgnoreCase) >= 0)
                return currentValue <= ParseArgDouble(args, 0);

            if (mode.IndexOf("Less Than", StringComparison.OrdinalIgnoreCase) >= 0)
                return currentValue < ParseArgDouble(args, 0);

            if (mode.IndexOf("Greater Than or Equal", StringComparison.OrdinalIgnoreCase) >= 0)
                return currentValue >= ParseArgDouble(args, 0);

            if (mode.IndexOf("Greater Than", StringComparison.OrdinalIgnoreCase) >= 0)
                return currentValue > ParseArgDouble(args, 0);

            if (mode.IndexOf("Value Between", StringComparison.OrdinalIgnoreCase) >= 0)
                return currentValue >= ParseArgDouble(args, 0) && currentValue <= ParseArgDouble(args, 1);

            return false;
        }

        private ulong ParseArgUnsigned(string[] args, int typeIndex, int index)
        {
            byte[] bytes = ParseArgBytes(args, typeIndex, index);
            return ReadUnsignedDisplay(bytes);
        }

        private long ParseArgSigned(string[] args, int typeIndex, int index)
        {
            byte[] bytes = ParseArgBytes(args, typeIndex, index);
            return ReadSignedDisplay(bytes);
        }

        private double ParseArgDouble(string[] args, int index)
        {
            if (args == null || index < 0 || index >= args.Length)
                return 0.0;

            double value;
            if (Double.TryParse(args[index], out value))
                return value;

            return 0.0;
        }

        private byte[] ParseArgBytes(string[] args, int typeIndex, int index)
        {
            if (args == null || index < 0 || index >= args.Length)
                return new byte[activeSnapshotByteSize];

            bool oldHex = Form1.ValHex;
            try
            {
                Form1.ValHex = false;
                return SearchTypes[typeIndex].ToByteArray(args[index]);
            }
            catch
            {
                return new byte[activeSnapshotByteSize];
            }
            finally
            {
                Form1.ValHex = oldHex;
            }
        }

        private ulong ReadUnsignedDisplay(byte[] value)
        {
            if (value == null)
                return 0;

            return misc.ByteArrayToLong(value, 0, value.Length);
        }

        private long ReadSignedDisplay(byte[] value)
        {
            if (value == null || value.Length == 0)
                return 0;

            ulong unsigned = ReadUnsignedDisplay(value);

            switch (value.Length)
            {
                case 1:
                    return (sbyte)(byte)unsigned;
                case 2:
                    return (short)(ushort)unsigned;
                case 4:
                    return (int)(uint)unsigned;
                case 8:
                    return (long)unsigned;
                default:
                    return (long)unsigned;
            }
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