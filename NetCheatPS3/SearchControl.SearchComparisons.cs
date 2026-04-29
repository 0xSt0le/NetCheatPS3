using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NetCheatPS3.Scanner;

namespace NetCheatPS3
{
    public partial class SearchControl
    {
        #region Initial Searches

        void standardByte_InitSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args, int cmpIntType)
        {
            bool gotRes = false;
            ncSearchType type = SearchTypes[typeIndex];
            if (args.Length > 0)
            {
                type.Initialize((string)args[0], typeIndex);
                type = SearchTypes[typeIndex];
            }

            bool updateCont = false;
            int size = 0x10000;
            //if ((stopAddr - startAddr) <= (ulong)size)
            //    updateCont = true;

            byte[] cmp = new byte[size];

            int realDif = 0;
            if (updateCont)
                realDif = (int)(stopAddr - startAddr);
            else
                realDif = (int)GetRealDif(startAddr, stopAddr, (ulong)size);
            BeginScanProgress("Initial Scan", realDif, "blocks");

            byte[] cmpArray = type.ToByteArray((string)args[0]);
            int resCnt = 0;
            int incSize = type.ByteSize;
            if (type.ignoreAlignment)
                incSize = GetAlign(1);
            List<SearchListView.SearchListViewItem> itemsToAdd = new List<SearchListView.SearchListViewItem>();

            bool isMatchCase = false;
            if (type.Name == "String")
            {
                Invoke((MethodInvoker)delegate
                {
                    isMatchCase = SearchArgs[0].GetState();
                });

                if (!isMatchCase)
                {
                    for (int x = 0; x < cmpArray.Length; x++)
                        if (cmpArray[x] > 0x60)
                            cmpArray[x] -= 0x20;
                }
            }

            for (ulong addr = startAddr; addr < stopAddr; addr += (ulong)size)
            {
                if (_shouldStopSearch)
                    break;

                gotRes = false;
                addr = misc.ParseSchAddr(addr);

                if (addr != startAddr && incSize == 1 && type.ByteSize != 1)
                {
                    addr -= (ulong)(cmpArray.Length + 1);
                }

                if (Form1.apiGetMem(addr, ref cmp))
                {
                    for (int x = 0; x <= (size - type.ByteSize); x += incSize)
                    {
                        if (_shouldStopSearch)
                            break;

                        byte[] tempArr = new byte[type.ByteSize];
                        Array.Copy(cmp, x, tempArr, 0, type.ByteSize);
                        tempArr = misc.notrevif(tempArr);
                        bool isTrue = false;
                        if (type.Name == "String")
                        {
                            isTrue = misc.ArrayCompareText(cmpArray, tempArr, isMatchCase, cmpIntType);
                        }
                        else
                        {
                            isTrue = misc.ArrayCompare(cmpArray, tempArr, null, cmpIntType);
                        }

                        if (isTrue)
                        {
                            itemsToAdd.Add(type.ToItem(addr + (ulong)x, tempArr, new byte[0], typeIndex));
                            resCnt++;
                            gotRes = true;
                            //SetStatusLabel("Results: " + resCnt.ToString());
                        }
                    }

                    if (gotRes)
                    {
                        AddResultRange(itemsToAdd);
                        itemsToAdd.Clear();
                    }

                }

                UpdateScanProgress(addr >= stopAddr ? realDif : (long)Math.Min(realDif, GetRealDif(startAddr, addr + (ulong)size, (ulong)size)), resCnt);
            }

            Invoke((MethodInvoker)delegate
            {
                searchListView1.AddItemsFromList();
            });
        }

        private void ClearActiveSnapshot()
        {
            try
            {
                if (!String.IsNullOrEmpty(activeSnapshotPath) && File.Exists(activeSnapshotPath))
                    File.Delete(activeSnapshotPath);
            }
            catch
            {
            }

            activeSnapshotPath = null;
            activeSnapshotTypeIndex = -1;
            activeSnapshotByteSize = 0;
            activeSnapshotResultCount = 0;
        }

        private string CreateSnapshotPath()
        {
            string dir = Path.Combine(Application.StartupPath, "Scans");

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            return Path.Combine(dir, "scan_" + DateTime.Now.Ticks.ToString() + ".ncsnap");
        }

        private bool HasActiveSnapshot()
        {
            return !String.IsNullOrEmpty(activeSnapshotPath) && File.Exists(activeSnapshotPath);
        }

        private bool IsLittleEndianModeChecked()
        {
            if (littleEndianCB == null)
                return false;

            bool result = false;

            if (littleEndianCB.InvokeRequired)
            {
                littleEndianCB.Invoke((MethodInvoker)delegate
                {
                    result = littleEndianCB.Checked;
                });
            }
            else
            {
                result = littleEndianCB.Checked;
            }

            return result;
        }

        private bool IsNoNegativeChecked()
        {
            if (noNegativeCB == null)
                return false;

            bool result = false;

            if (noNegativeCB.InvokeRequired)
            {
                noNegativeCB.Invoke((MethodInvoker)delegate
                {
                    result = noNegativeCB.Checked;
                });
            }
            else
            {
                result = noNegativeCB.Checked;
            }

            return result;
        }

        private bool IsCleanFloatChecked()
        {
            if (cleanFloatCB == null)
                return false;

            bool result = false;

            if (cleanFloatCB.InvokeRequired)
            {
                cleanFloatCB.Invoke((MethodInvoker)delegate
                {
                    result = cleanFloatCB.Checked;
                });
            }
            else
            {
                result = cleanFloatCB.Checked;
            }

            return result;
        }

        private bool IsNoZeroChecked()
        {
            if (noZeroCB == null)
                return false;

            bool result = false;

            if (noZeroCB.InvokeRequired)
            {
                noZeroCB.Invoke((MethodInvoker)delegate
                {
                    result = noZeroCB.Checked;
                });
            }
            else
            {
                result = noZeroCB.Checked;
            }

            return result;
        }

        private void RefreshNewScannerFilterOptions()
        {
            activeFilterNoNegative = IsNoNegativeChecked();
            activeFilterCleanFloat = IsCleanFloatChecked();
            activeFilterNoZero = IsNoZeroChecked();
        }

        private bool CanUseNewExactEqualSearch(int typeIndex)
        {
            string typeName = SearchTypes[typeIndex].Name;
            return typeName == "1 byte" ||
                   typeName == "2 bytes" ||
                   typeName == "4 bytes" ||
                   typeName == "8 bytes" ||
                   typeName == "Float" ||
                   typeName == "Double";
        }

        private void RunNewExactEqualSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args)
        {
            ncSearchType type = SearchTypes[typeIndex];
            if (args.Length > 0)
            {
                type.Initialize((string)args[0], typeIndex);
                type = SearchTypes[typeIndex];
            }

            int alignment = type.ignoreAlignment ? GetAlign(1) : type.ByteSize;
            if (alignment <= 0)
                alignment = type.ByteSize;

            byte[] compareBytes = type.ToByteArray((string)args[0]);

            int totalBlocks = (int)(((stopAddr - startAddr) + (ulong)ExactScanner.DefaultBlockSize - 1) / (ulong)ExactScanner.DefaultBlockSize);
            if (totalBlocks <= 0)
                totalBlocks = 1;

            BeginScanProgress("Initial Scan", totalBlocks, "blocks");

            MemoryReader reader = new MemoryReader();
            ExactScanRequest request = new ExactScanRequest();
            request.Start = startAddr;
            request.Stop = stopAddr;
            request.BlockSize = GetSelectedExactScanBlockSize();
            request.ByteSize = type.ByteSize;
            request.Alignment = alignment;
            request.CompareBytes = compareBytes;
            activeScanUsesNewEngine = true;
            activeScanLittleEndian = IsLittleEndianModeChecked();
            RefreshNewScannerFilterOptions();
            request.EndianMode = activeScanLittleEndian ? EndianMode.Little : EndianMode.Big;
            request.KeepRawBytes = false;

            List<SearchListView.SearchListViewItem> batch = new List<SearchListView.SearchListViewItem>(512);
            int resCnt = 0;

            ExactScanner.Scan(
                request,
                reader,
                delegate () { return _shouldStopSearch; },
                delegate (int blockIndex)
                {
                    UpdateScanProgress(blockIndex + 1, resCnt);
                },
                delegate (ulong addr, byte[] displayBytes)
                {
                    if (!NewScanner_ShouldKeepDisplayedValue(displayBytes, typeIndex, false))
                        return;

                    batch.Add(type.ToItem(addr, displayBytes, new byte[0], typeIndex));
                    resCnt++;

                    if (batch.Count >= 512)
                    {
                        AddResultRange(batch);
                        batch.Clear();
                    }
                });

            if (batch.Count > 0)
            {
                AddResultRange(batch);
                batch.Clear();
            }

            Invoke((MethodInvoker)delegate
            {
                searchListView1.AddItemsFromList();
            });

            CompleteScanProgress("Initial Scan complete | Results: " + resCnt.ToString("N0"));
        }

        private byte[] NewScanner_ValueBytesToRaw(byte[] valueBytes, int byteSize)
        {
            byte[] raw = new byte[byteSize];

            if (valueBytes != null)
            {
                int count = Math.Min(valueBytes.Length, byteSize);
                Buffer.BlockCopy(valueBytes, 0, raw, 0, count);
            }

            if (activeScanLittleEndian && byteSize > 1)
                Array.Reverse(raw);

            return raw;
        }

        private byte[] NewScanner_RawBytesToValue(byte[] rawBytes, int byteSize)
        {
            byte[] value = new byte[byteSize];

            if (rawBytes != null)
            {
                int count = Math.Min(rawBytes.Length, byteSize);
                Buffer.BlockCopy(rawBytes, 0, value, 0, count);
            }

            if (activeScanLittleEndian && byteSize > 1)
                Array.Reverse(value);

            return value;
        }

        private bool NewScanner_BytesEqual(byte[] a, byte[] b, int size)
        {
            if (a == null || b == null)
                return false;
            if (a.Length < size || b.Length < size)
                return false;

            for (int i = 0; i < size; i++)
            {
                if (a[i] != b[i])
                    return false;
            }

            return true;
        }

        private bool NewScanner_TryReadCurrentRawValue(
            MemoryReader reader,
            ulong addr,
            int byteSize,
            ref ulong cachedBlockBase,
            ref byte[] cachedBlock,
            ref bool cachedBlockValid,
            out byte[] rawValue)
        {
            rawValue = null;

            if (reader == null)
                return false;

            int blockSize = ExactScanner.DefaultBlockSize;
            ulong blockBase = addr - (addr % (ulong)blockSize);
            int offset = (int)(addr - blockBase);

            if (!cachedBlockValid || cachedBlockBase != blockBase)
            {
                cachedBlockBase = blockBase;
                cachedBlockValid = reader.TryReadBlock(cachedBlockBase, cachedBlock);
            }

            if (cachedBlockValid && offset >= 0 && (offset + byteSize) <= cachedBlock.Length)
            {
                rawValue = new byte[byteSize];
                Buffer.BlockCopy(cachedBlock, offset, rawValue, 0, byteSize);
                return true;
            }

            byte[] direct = new byte[byteSize];
            if (reader.TryReadBlock(addr, direct))
            {
                rawValue = direct;
                return true;
            }

            return false;
        }

        private void RunNewBasicNextSearch(SearchListView.SearchListViewItem[] old, string[] args, int nextMode)
        {
            RefreshNewScannerFilterOptions();

            if (old == null || old.Length == 0)
            {
                CompleteScanProgress("Next Scan complete | Results: 0");

                Invoke((MethodInvoker)delegate
                {
                    searchListView1.AddItemsFromList();
                });

                return;
            }

            BeginScanProgress("Next Scan", old.Length, "candidates");

            ClearItems();

            MemoryReader reader = new MemoryReader();
            byte[] cachedBlock = new byte[ExactScanner.DefaultBlockSize];
            ulong cachedBlockBase = 0;
            bool cachedBlockValid = false;

            List<SearchListView.SearchListViewItem> batch = new List<SearchListView.SearchListViewItem>(512);
            int resCnt = 0;

            byte[] equalCompareRaw = null;
            int equalCompareAlign = -1;

            for (int cnt = 0; cnt < old.Length; cnt++)
            {
                if (_shouldStopSearch)
                    break;

                SearchListView.SearchListViewItem oldItem = old[cnt];
                ncSearchType type = SearchTypes[oldItem.align];
                int byteSize = type.ByteSize;

                if (byteSize <= 0)
                    continue;

                byte[] currentRaw;
                bool readOk = NewScanner_TryReadCurrentRawValue(
                    reader,
                    oldItem.addr,
                    byteSize,
                    ref cachedBlockBase,
                    ref cachedBlock,
                    ref cachedBlockValid,
                    out currentRaw);

                if (!readOk)
                    continue;

                bool isMatch = false;

                if (nextMode == Form1.compEq || nextMode == Form1.compNEq)
                {
                    if (equalCompareRaw == null || equalCompareAlign != oldItem.align)
                    {
                        byte[] valueBytes = type.ToByteArray((string)args[0]);
                        equalCompareRaw = NewScanner_ValueBytesToRaw(valueBytes, byteSize);
                        equalCompareAlign = oldItem.align;
                    }

                    bool equal = NewScanner_BytesEqual(currentRaw, equalCompareRaw, byteSize);
                    isMatch = nextMode == Form1.compEq ? equal : !equal;
                }
                else if (nextMode == Form1.compChg)
                {
                    byte[] oldRaw = NewScanner_ValueBytesToRaw(oldItem.newVal, byteSize);
                    isMatch = !NewScanner_BytesEqual(currentRaw, oldRaw, byteSize);
                }
                else if (nextMode == Form1.compUChg)
                {
                    byte[] oldRaw = NewScanner_ValueBytesToRaw(oldItem.newVal, byteSize);
                    isMatch = NewScanner_BytesEqual(currentRaw, oldRaw, byteSize);
                }

                if (isMatch)
                {
                    byte[] newValue = NewScanner_RawBytesToValue(currentRaw, byteSize);

                    if (!NewScanner_ShouldKeepDisplayedValue(newValue, oldItem.align, false))
                        continue;

                    SearchListView.SearchListViewItem newItem = oldItem;
                    newItem.oldVal = oldItem.newVal;
                    newItem.newVal = newValue;
                    batch.Add(newItem);
                    resCnt++;

                    if (batch.Count >= 512)
                    {
                        AddResultRange(batch);
                        batch.Clear();
                    }
                }

                UpdateScanProgress(cnt + 1, resCnt);
            }

            if (batch.Count > 0)
            {
                AddResultRange(batch);
                batch.Clear();
            }

            Invoke((MethodInvoker)delegate
            {
                searchListView1.AddItemsFromList();
            });

            CompleteScanProgress(
                "Next Scan complete | Results: " + resCnt.ToString("N0") +
                " | Reads OK: " + reader.Stats.ReadSuccesses.ToString("N0") +
                " | Failed: " + reader.Stats.ReadFailures.ToString("N0"));
        }

        private long NewScanner_ToSignedInteger(byte[] valueBytes, int byteSize)
        {
            if (valueBytes == null || valueBytes.Length < byteSize)
                return 0;

            ulong raw = 0;
            for (int i = 0; i < byteSize; i++)
                raw = (raw << 8) | valueBytes[i];

            switch (byteSize)
            {
                case 1:
                    return unchecked((sbyte)raw);
                case 2:
                    return unchecked((short)raw);
                case 4:
                    return unchecked((int)raw);
                case 8:
                    return unchecked((long)raw);
            }

            return unchecked((long)raw);
        }

        private float NewScanner_ToFloat(byte[] valueBytes)
        {
            if (valueBytes == null || valueBytes.Length < 4)
                return 0.0f;

            byte[] temp = new byte[4];
            Buffer.BlockCopy(valueBytes, 0, temp, 0, 4);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(temp);

            return BitConverter.ToSingle(temp, 0);
        }

        private double NewScanner_ToDouble(byte[] valueBytes)
        {
            if (valueBytes == null || valueBytes.Length < 8)
                return 0.0;

            byte[] temp = new byte[8];
            Buffer.BlockCopy(valueBytes, 0, temp, 0, 8);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(temp);

            return BitConverter.ToDouble(temp, 0);
        }

        private bool NewScanner_IsDirectionMatch(byte[] previousValue, byte[] currentValue, int typeIndex, bool increased)
        {
            if (previousValue == null || currentValue == null)
                return false;

            ncSearchType type = SearchTypes[typeIndex];
            string typeName = type.Name;

            if (typeName == "Float")
            {
                float oldFloat = NewScanner_ToFloat(previousValue);
                float newFloat = NewScanner_ToFloat(currentValue);

                if (float.IsNaN(oldFloat) || float.IsNaN(newFloat))
                    return false;

                return increased ? newFloat > oldFloat : newFloat < oldFloat;
            }

            if (typeName == "Double")
            {
                double oldDouble = NewScanner_ToDouble(previousValue);
                double newDouble = NewScanner_ToDouble(currentValue);

                if (double.IsNaN(oldDouble) || double.IsNaN(newDouble))
                    return false;

                return increased ? newDouble > oldDouble : newDouble < oldDouble;
            }

            long oldInt = NewScanner_ToSignedInteger(previousValue, type.ByteSize);
            long newInt = NewScanner_ToSignedInteger(currentValue, type.ByteSize);

            return increased ? newInt > oldInt : newInt < oldInt;
        }

        private void RunNewDirectionNextSearch(SearchListView.SearchListViewItem[] old, bool increased)
        {
            RefreshNewScannerFilterOptions();

            if (old == null || old.Length == 0)
            {
                CompleteScanProgress("Next Scan complete | Results: 0");

                Invoke((MethodInvoker)delegate
                {
                    searchListView1.AddItemsFromList();
                });

                return;
            }

            BeginScanProgress("Next Scan", old.Length, "candidates");

            ClearItems();

            MemoryReader reader = new MemoryReader();
            byte[] cachedBlock = new byte[ExactScanner.DefaultBlockSize];
            ulong cachedBlockBase = 0;
            bool cachedBlockValid = false;

            List<SearchListView.SearchListViewItem> batch = new List<SearchListView.SearchListViewItem>(512);
            int resCnt = 0;

            for (int cnt = 0; cnt < old.Length; cnt++)
            {
                if (_shouldStopSearch)
                    break;

                SearchListView.SearchListViewItem oldItem = old[cnt];
                ncSearchType type = SearchTypes[oldItem.align];
                int byteSize = type.ByteSize;

                if (byteSize <= 0)
                    continue;

                byte[] currentRaw;
                bool readOk = NewScanner_TryReadCurrentRawValue(
                    reader,
                    oldItem.addr,
                    byteSize,
                    ref cachedBlockBase,
                    ref cachedBlock,
                    ref cachedBlockValid,
                    out currentRaw);

                if (!readOk)
                    continue;

                byte[] currentValue = NewScanner_RawBytesToValue(currentRaw, byteSize);

                if (NewScanner_IsDirectionMatch(oldItem.newVal, currentValue, oldItem.align, increased))
                {
                    if (!NewScanner_ShouldKeepDisplayedValue(currentValue, oldItem.align, false))
                        continue;

                    SearchListView.SearchListViewItem newItem = oldItem;
                    newItem.oldVal = oldItem.newVal;
                    newItem.newVal = currentValue;
                    batch.Add(newItem);
                    resCnt++;

                    if (batch.Count >= 512)
                    {
                        AddResultRange(batch);
                        batch.Clear();
                    }
                }

                UpdateScanProgress(cnt + 1, resCnt);
            }

            if (batch.Count > 0)
            {
                AddResultRange(batch);
                batch.Clear();
            }

            Invoke((MethodInvoker)delegate
            {
                searchListView1.AddItemsFromList();
            });

            CompleteScanProgress(
                "Next Scan complete | Results: " + resCnt.ToString("N0") +
                " | Reads OK: " + reader.Stats.ReadSuccesses.ToString("N0") +
                " | Failed: " + reader.Stats.ReadFailures.ToString("N0"));
        }

        private ulong NewScanner_ToUnsignedInteger(byte[] valueBytes, int byteSize)
        {
            if (valueBytes == null || valueBytes.Length < byteSize)
                return 0;

            ulong raw = 0;
            for (int i = 0; i < byteSize; i++)
                raw = (raw << 8) | valueBytes[i];

            return raw;
        }

        private bool NewScanner_AreDoublesClose(double a, double b)
        {
            double diff = Math.Abs(a - b);
            double scale = Math.Max(1.0, Math.Max(Math.Abs(a), Math.Abs(b)));
            return diff <= (scale * 0.000001);
        }

        private bool NewScanner_IsDeltaMatch(byte[] previousValue, byte[] currentValue, byte[] deltaValue, int typeIndex, bool increased)
        {
            if (previousValue == null || currentValue == null || deltaValue == null)
                return false;

            ncSearchType type = SearchTypes[typeIndex];
            string typeName = type.Name;

            if (typeName == "Float")
            {
                float oldFloat = NewScanner_ToFloat(previousValue);
                float newFloat = NewScanner_ToFloat(currentValue);
                float deltaFloat = NewScanner_ToFloat(deltaValue);

                if (float.IsNaN(oldFloat) || float.IsNaN(newFloat) || float.IsNaN(deltaFloat))
                    return false;

                double actual = increased ? (newFloat - oldFloat) : (oldFloat - newFloat);
                if (actual < 0.0)
                    return false;

                return NewScanner_AreDoublesClose(actual, deltaFloat);
            }

            if (typeName == "Double")
            {
                double oldDouble = NewScanner_ToDouble(previousValue);
                double newDouble = NewScanner_ToDouble(currentValue);
                double deltaDouble = NewScanner_ToDouble(deltaValue);

                if (double.IsNaN(oldDouble) || double.IsNaN(newDouble) || double.IsNaN(deltaDouble))
                    return false;

                double actual = increased ? (newDouble - oldDouble) : (oldDouble - newDouble);
                if (actual < 0.0)
                    return false;

                return NewScanner_AreDoublesClose(actual, deltaDouble);
            }

            ulong oldInt = NewScanner_ToUnsignedInteger(previousValue, type.ByteSize);
            ulong newInt = NewScanner_ToUnsignedInteger(currentValue, type.ByteSize);
            ulong deltaInt = NewScanner_ToUnsignedInteger(deltaValue, type.ByteSize);

            if (increased)
            {
                if (newInt < oldInt)
                    return false;

                return (newInt - oldInt) == deltaInt;
            }
            else
            {
                if (oldInt < newInt)
                    return false;

                return (oldInt - newInt) == deltaInt;
            }
        }

        private void RunNewDeltaNextSearch(SearchListView.SearchListViewItem[] old, string[] args, bool increased)
        {
            RefreshNewScannerFilterOptions();

            if (old == null || old.Length == 0 || args == null || args.Length == 0)
            {
                CompleteScanProgress("Next Scan complete | Results: 0");

                Invoke((MethodInvoker)delegate
                {
                    searchListView1.AddItemsFromList();
                });

                return;
            }

            BeginScanProgress("Next Scan", old.Length, "candidates");

            ClearItems();

            MemoryReader reader = new MemoryReader();
            byte[] cachedBlock = new byte[ExactScanner.DefaultBlockSize];
            ulong cachedBlockBase = 0;
            bool cachedBlockValid = false;

            List<SearchListView.SearchListViewItem> batch = new List<SearchListView.SearchListViewItem>(512);
            int resCnt = 0;

            byte[] deltaValue = null;
            int deltaAlign = -1;

            for (int cnt = 0; cnt < old.Length; cnt++)
            {
                if (_shouldStopSearch)
                    break;

                SearchListView.SearchListViewItem oldItem = old[cnt];
                ncSearchType type = SearchTypes[oldItem.align];
                int byteSize = type.ByteSize;

                if (byteSize <= 0)
                    continue;

                if (deltaValue == null || deltaAlign != oldItem.align)
                {
                    deltaValue = type.ToByteArray((string)args[0]);
                    deltaAlign = oldItem.align;
                }

                byte[] currentRaw;
                bool readOk = NewScanner_TryReadCurrentRawValue(
                    reader,
                    oldItem.addr,
                    byteSize,
                    ref cachedBlockBase,
                    ref cachedBlock,
                    ref cachedBlockValid,
                    out currentRaw);

                if (!readOk)
                    continue;

                byte[] currentValue = NewScanner_RawBytesToValue(currentRaw, byteSize);

                if (NewScanner_IsDeltaMatch(oldItem.newVal, currentValue, deltaValue, oldItem.align, increased))
                {
                    if (!NewScanner_ShouldKeepDisplayedValue(currentValue, oldItem.align, false))
                        continue;

                    SearchListView.SearchListViewItem newItem = oldItem;
                    newItem.oldVal = oldItem.newVal;
                    newItem.newVal = currentValue;
                    batch.Add(newItem);
                    resCnt++;

                    if (batch.Count >= 512)
                    {
                        AddResultRange(batch);
                        batch.Clear();
                    }
                }

                UpdateScanProgress(cnt + 1, resCnt);
            }

            if (batch.Count > 0)
            {
                AddResultRange(batch);
                batch.Clear();
            }

            Invoke((MethodInvoker)delegate
            {
                searchListView1.AddItemsFromList();
            });

            CompleteScanProgress(
                "Next Scan complete | Results: " + resCnt.ToString("N0") +
                " | Reads OK: " + reader.Stats.ReadSuccesses.ToString("N0") +
                " | Failed: " + reader.Stats.ReadFailures.ToString("N0"));
        }

        private ulong NewScanner_ToUnsignedIntegerFromRaw(byte[] rawBytes, int offset, int byteSize)
        {
            if (rawBytes == null || offset < 0 || byteSize <= 0 || (offset + byteSize) > rawBytes.Length)
                return 0;

            ulong value = 0;

            if (activeScanLittleEndian && byteSize > 1)
            {
                for (int i = byteSize - 1; i >= 0; i--)
                    value = (value << 8) | rawBytes[offset + i];
            }
            else
            {
                for (int i = 0; i < byteSize; i++)
                    value = (value << 8) | rawBytes[offset + i];
            }

            return value;
        }

        private double NewScanner_ToFloatingFromRaw(byte[] rawBytes, int offset, int byteSize, byte[] scratch)
        {
            if (rawBytes == null || scratch == null)
                return 0.0;
            if (offset < 0 || byteSize <= 0 || (offset + byteSize) > rawBytes.Length)
                return 0.0;
            if (scratch.Length < byteSize)
                return 0.0;

            bool rawIsLittleEndian = activeScanLittleEndian;

            if (BitConverter.IsLittleEndian)
            {
                if (rawIsLittleEndian)
                {
                    Buffer.BlockCopy(rawBytes, offset, scratch, 0, byteSize);
                }
                else
                {
                    for (int i = 0; i < byteSize; i++)
                        scratch[i] = rawBytes[offset + (byteSize - 1 - i)];
                }
            }
            else
            {
                if (rawIsLittleEndian)
                {
                    for (int i = 0; i < byteSize; i++)
                        scratch[i] = rawBytes[offset + (byteSize - 1 - i)];
                }
                else
                {
                    Buffer.BlockCopy(rawBytes, offset, scratch, 0, byteSize);
                }
            }

            if (byteSize == 4)
                return BitConverter.ToSingle(scratch, 0);

            if (byteSize == 8)
                return BitConverter.ToDouble(scratch, 0);

            return 0.0;
        }

        private bool NewScanner_ShouldKeepDisplayedValue(byte[] valueBytes, int typeIndex, bool applyNoZeroFilter)
        {
            if (valueBytes == null)
                return false;

            ncSearchType type = SearchTypes[typeIndex];
            string typeName = type.Name;

            bool noNegative = activeFilterNoNegative;
            bool cleanFloat = activeFilterCleanFloat;
            bool noZero = applyNoZeroFilter && activeFilterNoZero;

            if (typeName == "Float")
            {
                float value = NewScanner_ToFloat(valueBytes);

                if (float.IsNaN(value) || float.IsInfinity(value))
                    return false;

                if (noZero && value == 0.0f)
                    return false;

                if (noNegative && value < 0.0f)
                    return false;

                if (cleanFloat)
                {
                    double abs = Math.Abs(value);

                    // Keep useful normal values, remove tiny float noise like 1.04425E-05.
                    if (value != 0.0f && abs < CleanFloatMinAbs)
                        return false;

                    // Remove huge scientific-notation garbage like 3.355456E+07.
                    if (abs > CleanFloatMaxAbs)
                        return false;
                }

                return true;
            }

            if (typeName == "Double")
            {
                double value = NewScanner_ToDouble(valueBytes);

                if (double.IsNaN(value) || double.IsInfinity(value))
                    return false;

                if (noZero && value == 0.0)
                    return false;

                if (noNegative && value < 0.0)
                    return false;

                if (cleanFloat)
                {
                    double abs = Math.Abs(value);

                    // Keep useful normal values, remove tiny double noise.
                    if (value != 0.0 && abs < CleanFloatMinAbs)
                        return false;

                    // Remove huge scientific-notation garbage.
                    if (abs > CleanFloatMaxAbs)
                        return false;
                }

                return true;
            }

            if (noZero)
            {
                ulong unsignedValue = NewScanner_ToUnsignedInteger(valueBytes, type.ByteSize);
                if (unsignedValue == 0)
                    return false;
            }

            if (noNegative && type.ByteSize > 1)
            {
                long signedValue = NewScanner_ToSignedInteger(valueBytes, type.ByteSize);
                if (signedValue < 0)
                    return false;
            }

            return true;
        }

        private bool NewScanner_IsValueBetween(byte[] valueBytes, byte[] minBytes, byte[] maxBytes, int typeIndex)
        {
            if (valueBytes == null || minBytes == null || maxBytes == null)
                return false;

            ncSearchType type = SearchTypes[typeIndex];
            string typeName = type.Name;

            if (typeName == "Float")
            {
                double value = NewScanner_ToFloat(valueBytes);
                double min = NewScanner_ToFloat(minBytes);
                double max = NewScanner_ToFloat(maxBytes);

                if (double.IsNaN(value) || double.IsNaN(min) || double.IsNaN(max))
                    return false;

                if (min > max)
                {
                    double tmp = min;
                    min = max;
                    max = tmp;
                }

                return value >= min && value <= max;
            }

            if (typeName == "Double")
            {
                double value = NewScanner_ToDouble(valueBytes);
                double min = NewScanner_ToDouble(minBytes);
                double max = NewScanner_ToDouble(maxBytes);

                if (double.IsNaN(value) || double.IsNaN(min) || double.IsNaN(max))
                    return false;

                if (min > max)
                {
                    double tmp = min;
                    min = max;
                    max = tmp;
                }

                return value >= min && value <= max;
            }

            ulong uintValue = NewScanner_ToUnsignedInteger(valueBytes, type.ByteSize);
            ulong uintMin = NewScanner_ToUnsignedInteger(minBytes, type.ByteSize);
            ulong uintMax = NewScanner_ToUnsignedInteger(maxBytes, type.ByteSize);

            if (uintMin > uintMax)
            {
                ulong tmp = uintMin;
                uintMin = uintMax;
                uintMax = tmp;
            }

            return uintValue >= uintMin && uintValue <= uintMax;
        }

        private bool NewScanner_IsRawValueBetween(
            byte[] rawBytes,
            int offset,
            int byteSize,
            int typeIndex,
            byte[] minBytes,
            byte[] maxBytes,
            byte[] scratch)
        {
            if (rawBytes == null || minBytes == null || maxBytes == null)
                return false;

            ncSearchType type = SearchTypes[typeIndex];
            string typeName = type.Name;

            if (typeName == "Float")
            {
                double value = NewScanner_ToFloatingFromRaw(rawBytes, offset, 4, scratch);
                double min = NewScanner_ToFloat(minBytes);
                double max = NewScanner_ToFloat(maxBytes);

                if (double.IsNaN(value) || double.IsNaN(min) || double.IsNaN(max))
                    return false;

                if (min > max)
                {
                    double tmp = min;
                    min = max;
                    max = tmp;
                }

                return value >= min && value <= max;
            }

            if (typeName == "Double")
            {
                double value = NewScanner_ToFloatingFromRaw(rawBytes, offset, 8, scratch);
                double min = NewScanner_ToDouble(minBytes);
                double max = NewScanner_ToDouble(maxBytes);

                if (double.IsNaN(value) || double.IsNaN(min) || double.IsNaN(max))
                    return false;

                if (min > max)
                {
                    double tmp = min;
                    min = max;
                    max = tmp;
                }

                return value >= min && value <= max;
            }

            ulong valueInt = NewScanner_ToUnsignedIntegerFromRaw(rawBytes, offset, byteSize);
            ulong minInt = NewScanner_ToUnsignedInteger(minBytes, byteSize);
            ulong maxInt = NewScanner_ToUnsignedInteger(maxBytes, byteSize);

            if (minInt > maxInt)
            {
                ulong tmp = minInt;
                minInt = maxInt;
                maxInt = tmp;
            }

            return valueInt >= minInt && valueInt <= maxInt;
        }

        private void RunNewValueBetweenInitialSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args)
        {
            if (args == null || args.Length < 2)
            {
                SetStatusLabel("Value Between requires min and max values");
                return;
            }

            ncSearchType type = SearchTypes[typeIndex];

            type.Initialize((string)args[0], typeIndex);
            type = SearchTypes[typeIndex];

            int alignment = type.ignoreAlignment ? GetAlign(1) : type.ByteSize;
            if (alignment <= 0)
                alignment = type.ByteSize;

            activeScanUsesNewEngine = true;
            activeScanLittleEndian = IsLittleEndianModeChecked();
            RefreshNewScannerFilterOptions();

            byte[] minBytes = type.ToByteArray((string)args[0]);
            byte[] maxBytes = type.ToByteArray((string)args[1]);

            int blockSize = ExactScanner.DefaultBlockSize;
            int totalBlocks = (int)(((stopAddr - startAddr) + (ulong)blockSize - 1) / (ulong)blockSize);
            if (totalBlocks <= 0)
                totalBlocks = 1;

            BeginScanProgress("Initial Scan", totalBlocks, "blocks");

            MemoryReader reader = new MemoryReader();
            byte[] fullBlock = new byte[blockSize];
            byte[] floatScratch = new byte[8];

            List<SearchListView.SearchListViewItem> batch = new List<SearchListView.SearchListViewItem>(512);
            int resCnt = 0;
            int blockIndex = 0;

            for (ulong addr = startAddr; addr < stopAddr; addr += (ulong)blockSize, blockIndex++)
            {
                if (_shouldStopSearch)
                    break;

                int usable = (int)Math.Min((ulong)blockSize, stopAddr - addr);
                if (usable <= 0)
                    break;

                byte[] readBlock = usable == blockSize ? fullBlock : new byte[usable];

                if (reader.TryReadBlock(addr, readBlock))
                {
                    int maxOffset = usable - type.ByteSize;

                    for (int off = 0; off <= maxOffset; off += alignment)
                    {
                        if (_shouldStopSearch)
                            break;

                        if (NewScanner_IsRawValueBetween(readBlock, off, type.ByteSize, typeIndex, minBytes, maxBytes, floatScratch))
                        {
                            byte[] hitRaw = new byte[type.ByteSize];
                            Buffer.BlockCopy(readBlock, off, hitRaw, 0, type.ByteSize);

                            byte[] displayBytes = NewScanner_RawBytesToValue(hitRaw, type.ByteSize);

                            if (!NewScanner_ShouldKeepDisplayedValue(displayBytes, typeIndex, false))
                                continue;

                            batch.Add(type.ToItem(addr + (ulong)off, displayBytes, new byte[0], typeIndex));
                            resCnt++;

                            if (batch.Count >= 512)
                            {
                                AddResultRange(batch);
                                batch.Clear();
                            }
                        }
                    }
                }

                UpdateScanProgress(blockIndex + 1, resCnt);
            }

            if (batch.Count > 0)
            {
                AddResultRange(batch);
                batch.Clear();
            }

            Invoke((MethodInvoker)delegate
            {
                searchListView1.AddItemsFromList();
            });

            CompleteScanProgress(
                "Initial Scan complete | Results: " + resCnt.ToString("N0") +
                " | Reads OK: " + reader.Stats.ReadSuccesses.ToString("N0") +
                " | Failed: " + reader.Stats.ReadFailures.ToString("N0"));
        }

        private void RunNewValueBetweenNextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            RefreshNewScannerFilterOptions();

            if (old == null || old.Length == 0 || args == null || args.Length < 2)
            {
                CompleteScanProgress("Next Scan complete | Results: 0");

                Invoke((MethodInvoker)delegate
                {
                    searchListView1.AddItemsFromList();
                });

                return;
            }

            BeginScanProgress("Next Scan", old.Length, "candidates");

            ClearItems();

            MemoryReader reader = new MemoryReader();
            byte[] cachedBlock = new byte[ExactScanner.DefaultBlockSize];
            ulong cachedBlockBase = 0;
            bool cachedBlockValid = false;

            List<SearchListView.SearchListViewItem> batch = new List<SearchListView.SearchListViewItem>(512);
            int resCnt = 0;

            byte[] minBytes = null;
            byte[] maxBytes = null;
            int cachedAlign = -1;

            for (int cnt = 0; cnt < old.Length; cnt++)
            {
                if (_shouldStopSearch)
                    break;

                SearchListView.SearchListViewItem oldItem = old[cnt];
                ncSearchType type = SearchTypes[oldItem.align];
                int byteSize = type.ByteSize;

                if (byteSize <= 0)
                    continue;

                if (minBytes == null || maxBytes == null || cachedAlign != oldItem.align)
                {
                    minBytes = type.ToByteArray((string)args[0]);
                    maxBytes = type.ToByteArray((string)args[1]);
                    cachedAlign = oldItem.align;
                }

                byte[] currentRaw;
                bool readOk = NewScanner_TryReadCurrentRawValue(
                    reader,
                    oldItem.addr,
                    byteSize,
                    ref cachedBlockBase,
                    ref cachedBlock,
                    ref cachedBlockValid,
                    out currentRaw);

                if (!readOk)
                    continue;

                byte[] currentValue = NewScanner_RawBytesToValue(currentRaw, byteSize);

                if (NewScanner_IsValueBetween(currentValue, minBytes, maxBytes, oldItem.align))
                {
                    if (!NewScanner_ShouldKeepDisplayedValue(currentValue, oldItem.align, false))
                        continue;

                    SearchListView.SearchListViewItem newItem = oldItem;
                    newItem.oldVal = oldItem.newVal;
                    newItem.newVal = currentValue;
                    batch.Add(newItem);
                    resCnt++;

                    if (batch.Count >= 512)
                    {
                        AddResultRange(batch);
                        batch.Clear();
                    }
                }

                UpdateScanProgress(cnt + 1, resCnt);
            }

            if (batch.Count > 0)
            {
                AddResultRange(batch);
                batch.Clear();
            }

            Invoke((MethodInvoker)delegate
            {
                searchListView1.AddItemsFromList();
            });

            CompleteScanProgress(
                "Next Scan complete | Results: " + resCnt.ToString("N0") +
                " | Reads OK: " + reader.Stats.ReadSuccesses.ToString("N0") +
                " | Failed: " + reader.Stats.ReadFailures.ToString("N0"));
        }

        private void RunNewUnknownInitialSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args)
        {
            ncSearchType type = SearchTypes[typeIndex];

            int alignment = type.ignoreAlignment ? GetAlign(1) : type.ByteSize;
            if (alignment <= 0)
                alignment = type.ByteSize;

            activeScanUsesNewEngine = true;
            activeScanLittleEndian = IsLittleEndianModeChecked();
            RefreshNewScannerFilterOptions();

            activeSnapshotTypeIndex = typeIndex;
            activeSnapshotByteSize = type.ByteSize;
            activeSnapshotResultCount = 0;

            int blockSize = ExactScanner.DefaultBlockSize;
            int totalBlocks = (int)(((stopAddr - startAddr) + (ulong)blockSize - 1) / (ulong)blockSize);
            if (totalBlocks <= 0)
                totalBlocks = 1;

            long estimatedResults = 0;
            if (type.ByteSize > 0)
                estimatedResults = (long)((stopAddr - startAddr) / (ulong)alignment);

            string msg =
                "This Unknown Value scan will write a disk-backed snapshot.\r\n\r\n" +
                "Estimated records: " + estimatedResults.ToString("N0") + "\r\n" +
                "Only the first " + MaxVisibleSnapshotResults.ToString("N0") + " results will be shown in the UI.\r\n" +
                "All records will still be kept on disk for Next Scan.\r\n\r\n" +
                "Continue?";

            if (MessageBox.Show(msg, "Unknown Value Snapshot", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.No)
                return;

            BeginScanProgress("Initial Scan", totalBlocks, "blocks");

            string snapshotPath = CreateSnapshotPath();
            MemoryReader reader = new MemoryReader();
            byte[] fullBlock = new byte[blockSize];

            List<SearchListView.SearchListViewItem> batch = new List<SearchListView.SearchListViewItem>(512);
            int visibleCount = 0;
            long totalCount = 0;
            int blockIndex = 0;

            using (SnapshotStore store = SnapshotStore.Create(snapshotPath, typeIndex, type.ByteSize, activeScanLittleEndian))
            {
                for (ulong addr = startAddr; addr < stopAddr; addr += (ulong)blockSize, blockIndex++)
                {
                    if (_shouldStopSearch)
                        break;

                    int usable = (int)Math.Min((ulong)blockSize, stopAddr - addr);
                    if (usable <= 0)
                        break;

                    byte[] readBlock = usable == blockSize ? fullBlock : new byte[usable];

                    if (reader.TryReadBlock(addr, readBlock))
                    {
                        int maxOffset = usable - type.ByteSize;

                        for (int off = 0; off <= maxOffset; off += alignment)
                        {
                            if (_shouldStopSearch)
                                break;

                            byte[] raw = new byte[type.ByteSize];
                            Buffer.BlockCopy(readBlock, off, raw, 0, type.ByteSize);

                            byte[] valueBytes = NewScanner_RawBytesToValue(raw, type.ByteSize);

                            if (!NewScanner_ShouldKeepDisplayedValue(valueBytes, typeIndex, true))
                                continue;

                            store.WriteRecord(addr + (ulong)off, valueBytes);
                            totalCount++;

                            if (visibleCount < MaxVisibleSnapshotResults)
                            {
                                batch.Add(type.ToItem(addr + (ulong)off, valueBytes, new byte[0], typeIndex));
                                visibleCount++;

                                if (batch.Count >= 512)
                                {
                                    AddResultRange(batch);
                                    batch.Clear();
                                }
                            }
                        }
                    }

                    UpdateScanProgress(blockIndex + 1, totalCount, visibleCount);
                }
            }

            if (batch.Count > 0)
            {
                AddResultRange(batch);
                batch.Clear();
            }

            activeSnapshotPath = snapshotPath;
            activeSnapshotResultCount = totalCount;

            Invoke((MethodInvoker)delegate
            {
                searchListView1.AddItemsFromList();
            });

            CompleteScanProgress(
                "Initial Scan complete | " + FormatResultCount(totalCount, visibleCount) +
                " | Reads OK: " + reader.Stats.ReadSuccesses.ToString("N0") +
                " | Failed: " + reader.Stats.ReadFailures.ToString("N0"));
        }

        private void RunNewSnapshotNextSearch(string[] args, int mode)
        {
            if (!HasActiveSnapshot())
            {
                SetStatusLabel("No active snapshot");
                return;
            }

            SnapshotHeader header = SnapshotStore.ReadHeader(activeSnapshotPath);

            activeScanUsesNewEngine = true;
            activeScanLittleEndian = header.LittleEndian;
            RefreshNewScannerFilterOptions();

            activeSnapshotTypeIndex = header.TypeIndex;
            activeSnapshotByteSize = header.ByteSize;

            ncSearchType type = SearchTypes[header.TypeIndex];

            BeginScanProgress("Next Scan", header.Count, "snapshot records");

            ClearItems();

            string newSnapshotPath = CreateSnapshotPath();

            MemoryReader reader = new MemoryReader();
            byte[] cachedBlock = new byte[ExactScanner.DefaultBlockSize];
            ulong cachedBlockBase = 0;
            bool cachedBlockValid = false;

            List<SearchListView.SearchListViewItem> batch = new List<SearchListView.SearchListViewItem>(512);
            int visibleCount = 0;
            long resultCount = 0;
            long processed = 0;

            byte[] compareValue = null;
            byte[] minValue = null;
            byte[] maxValue = null;

            if ((mode == Form1.compEq || mode == Form1.compNEq) && args != null && args.Length > 0)
                compareValue = type.ToByteArray((string)args[0]);

            if (mode == Form1.compVBet && args != null && args.Length >= 2)
            {
                minValue = type.ToByteArray((string)args[0]);
                maxValue = type.ToByteArray((string)args[1]);
            }

            byte[] deltaValue = null;
            if ((mode == Form1.compINC || mode == Form1.compDEC) && args != null && args.Length > 0)
                deltaValue = type.ToByteArray((string)args[0]);

            using (SnapshotStore newStore = SnapshotStore.Create(newSnapshotPath, header.TypeIndex, header.ByteSize, header.LittleEndian))
            {
                foreach (SnapshotRecord record in SnapshotStore.ReadRecords(activeSnapshotPath))
                {
                    if (_shouldStopSearch)
                        break;

                    byte[] currentRaw;
                    bool readOk = NewScanner_TryReadCurrentRawValue(
                        reader,
                        record.Address,
                        header.ByteSize,
                        ref cachedBlockBase,
                        ref cachedBlock,
                        ref cachedBlockValid,
                        out currentRaw);

                    if (!readOk)
                    {
                        processed++;
                        continue;
                    }

                    byte[] currentValue = NewScanner_RawBytesToValue(currentRaw, header.ByteSize);

                    if (!NewScanner_ShouldKeepDisplayedValue(currentValue, header.TypeIndex, true))
                    {
                        processed++;
                        continue;
                    }

                    bool isMatch = false;

                    if (mode == Form1.compEq)
                    {
                        isMatch = NewScanner_BytesEqual(currentValue, compareValue, header.ByteSize);
                    }
                    else if (mode == Form1.compNEq)
                    {
                        isMatch = !NewScanner_BytesEqual(currentValue, compareValue, header.ByteSize);
                    }
                    else if (mode == Form1.compChg)
                    {
                        isMatch = !NewScanner_BytesEqual(currentValue, record.Value, header.ByteSize);
                    }
                    else if (mode == Form1.compUChg)
                    {
                        isMatch = NewScanner_BytesEqual(currentValue, record.Value, header.ByteSize);
                    }
                    else if (mode == Form1.compGT)
                    {
                        isMatch = NewScanner_IsDirectionMatch(record.Value, currentValue, header.TypeIndex, true);
                    }
                    else if (mode == Form1.compLT)
                    {
                        isMatch = NewScanner_IsDirectionMatch(record.Value, currentValue, header.TypeIndex, false);
                    }
                    else if (mode == Form1.compINC)
                    {
                        isMatch = NewScanner_IsDeltaMatch(record.Value, currentValue, deltaValue, header.TypeIndex, true);
                    }
                    else if (mode == Form1.compDEC)
                    {
                        isMatch = NewScanner_IsDeltaMatch(record.Value, currentValue, deltaValue, header.TypeIndex, false);
                    }
                    else if (mode == Form1.compVBet)
                    {
                        isMatch = NewScanner_IsValueBetween(currentValue, minValue, maxValue, header.TypeIndex);
                    }

                    if (isMatch)
                    {
                        newStore.WriteRecord(record.Address, currentValue);
                        resultCount++;

                        if (visibleCount < MaxVisibleSnapshotResults)
                        {
                            SearchListView.SearchListViewItem item = type.ToItem(record.Address, currentValue, record.Value, header.TypeIndex);
                            batch.Add(item);
                            visibleCount++;

                            if (batch.Count >= 512)
                            {
                                AddResultRange(batch);
                                batch.Clear();
                            }
                        }
                    }

                    processed++;

                    UpdateScanProgress(processed, resultCount, visibleCount);
                }
            }

            if (batch.Count > 0)
            {
                AddResultRange(batch);
                batch.Clear();
            }

            string oldSnapshotPath = activeSnapshotPath;
            activeSnapshotPath = newSnapshotPath;
            activeSnapshotResultCount = resultCount;

            try
            {
                if (!String.IsNullOrEmpty(oldSnapshotPath) && File.Exists(oldSnapshotPath))
                    File.Delete(oldSnapshotPath);
            }
            catch
            {
            }

            Invoke((MethodInvoker)delegate
            {
                searchListView1.AddItemsFromList();
            });

            CompleteScanProgress(
                "Next Scan complete | " + FormatResultCount(resultCount, visibleCount) +
                " | Reads OK: " + reader.Stats.ReadSuccesses.ToString("N0") +
                " | Failed: " + reader.Stats.ReadFailures.ToString("N0"));
        }

        void EqualTo_InitSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args)
        {
            if (CanUseNewExactEqualSearch(typeIndex))
            {
                RunNewExactEqualSearch(startAddr, stopAddr, typeIndex, args);
                return;
            }

            standardByte_InitSearch(startAddr, stopAddr, typeIndex, args, Form1.compEq);
        }

        void NotEqualTo_InitSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args)
        {
            standardByte_InitSearch(startAddr, stopAddr, typeIndex, args, Form1.compNEq);
        }

        void LessThan_InitSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args)
        {
            standardByte_InitSearch(startAddr, stopAddr, typeIndex, args, Form1.compLT);
        }

        void LessThanEqualTo_InitSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args)
        {
            standardByte_InitSearch(startAddr, stopAddr, typeIndex, args, Form1.compLTE);
        }

        void LessThanU_InitSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args)
        {
            standardByte_InitSearch(startAddr, stopAddr, typeIndex, args, Form1.compLTU);
        }

        void LessThanEqualToU_InitSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args)
        {
            standardByte_InitSearch(startAddr, stopAddr, typeIndex, args, Form1.compLTEU);
        }

        void GreaterThan_InitSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args)
        {
            standardByte_InitSearch(startAddr, stopAddr, typeIndex, args, Form1.compGT);
        }

        void GreaterThanEqualTo_InitSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args)
        {
            standardByte_InitSearch(startAddr, stopAddr, typeIndex, args, Form1.compGTE);
        }

        void GreaterThanU_InitSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args)
        {
            standardByte_InitSearch(startAddr, stopAddr, typeIndex, args, Form1.compGTU);
        }

        void GreaterThanEqualToU_InitSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args)
        {
            standardByte_InitSearch(startAddr, stopAddr, typeIndex, args, Form1.compGTEU);
        }

        void ValueBetween_InitSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args)
        {
            if (CanUseNewExactEqualSearch(typeIndex))
            {
                RunNewValueBetweenInitialSearch(startAddr, stopAddr, typeIndex, args);
                return;
            }

            ncSearchType type = SearchTypes[typeIndex];
            type.Initialize((string)args[0], typeIndex);
            type = SearchTypes[typeIndex];

            bool gotRes = false;
            bool updateCont = false;
            int size = 0x10000;
            //if ((stopAddr - startAddr) <= (ulong)size)
            //    updateCont = true;

            byte[] cmp = new byte[size];

            int realDif = 0;
            if (updateCont)
                realDif = (int)(stopAddr - startAddr);
            else
                realDif = (int)GetRealDif(startAddr, stopAddr, (ulong)size);
            BeginScanProgress("Initial Scan", realDif, "blocks");

            byte[] cmpArrayMin = type.ToByteArray((string)args[0]);
            byte[] cmpArrayMax = type.ToByteArray((string)args[1]);
            int resCnt = 0;
            int incSize = type.ByteSize;
            if (type.ignoreAlignment)
                incSize = GetAlign(1);
            //List<SearchListView.SearchListViewItem> itemsToAdd = new List<SearchListView.SearchListViewItem>();

            for (ulong addr = startAddr; addr < stopAddr; addr += (ulong)size)
            {
                if (_shouldStopSearch)
                    break;

                addr = misc.ParseSchAddr(addr);
                gotRes = false;
                searchListView1.BeginUpdate();

                if (addr != startAddr && incSize == 1 && type.ByteSize != 1)
                {
                    addr -= (ulong)(cmpArrayMin.Length + 1);
                }

                List<SearchListView.SearchListViewItem> resItems = new List<SearchListView.SearchListViewItem>();

                if (Form1.apiGetMem(addr, ref cmp))
                {
                    for (int x = 0; x <= (size - type.ByteSize); x += incSize)
                    {
                        if (_shouldStopSearch)
                            break;

                        byte[] tempArr = new byte[type.ByteSize];
                        Array.Copy(cmp, x, tempArr, 0, type.ByteSize);
                        tempArr = misc.notrevif(tempArr);

                        bool isTrue = misc.ArrayCompare(cmpArrayMin, tempArr, cmpArrayMax, Form1.compVBet);

                        if (isTrue)
                        {
                            resItems.Add(type.ToItem(addr + (ulong)x, tempArr, new byte[0], typeIndex));
                            resCnt++;
                            gotRes = true;
                        }
                    }

                    if (gotRes)
                    {
                        AddResultRange(resItems);
                        resItems.Clear();
                    }
                }

                searchListView1.EndUpdate();
                Invoke((MethodInvoker)delegate
                {
                    searchListView1.AddItemsFromList();
                });
                UpdateScanProgress(addr >= stopAddr ? realDif : (long)Math.Min(realDif, GetRealDif(startAddr, addr + (ulong)size, (ulong)size)), resCnt);
            }
        }

        void Pointer_InitSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args)
        {
            uint val = uint.Parse((string)args[0], System.Globalization.NumberStyles.HexNumber);

            ncSearchType type = SearchTypes[typeIndex];
            type.Initialize((string)args[0], typeIndex);
            type = SearchTypes[typeIndex];

            bool gotRes = false;
            bool updateCont = false;
            int size = 0x10000;
            //if ((stopAddr - startAddr) <= (ulong)size)
            //    updateCont = true;

            byte[] cmp = new byte[size];

            int realDif = 0;
            if (updateCont)
                realDif = (int)(stopAddr - startAddr);
            else
                realDif = (int)GetRealDif(startAddr, stopAddr, (ulong)size);
            BeginScanProgress("Initial Scan", realDif, "blocks");

            byte[] cmpArrayMin = type.ToByteArray((val - 0x7FFF).ToString("X8"));
            byte[] cmpArrayMax = type.ToByteArray((val + 0x7FFF).ToString("X8"));
            int resCnt = 0;
            int incSize = type.ByteSize;
            if (type.ignoreAlignment)
                incSize = GetAlign(1);
            //List<SearchListView.SearchListViewItem> itemsToAdd = new List<SearchListView.SearchListViewItem>();

            for (ulong addr = startAddr; addr < stopAddr; addr += (ulong)size)
            {
                if (_shouldStopSearch)
                    break;

                addr = misc.ParseSchAddr(addr);
                gotRes = false;
                searchListView1.BeginUpdate();

                if (addr != startAddr && incSize == 1 && type.ByteSize != 1)
                {
                    addr -= (ulong)(cmpArrayMin.Length + 1);
                }

                List<SearchListView.SearchListViewItem> resItems = new List<SearchListView.SearchListViewItem>();

                if (Form1.apiGetMem(addr, ref cmp))
                {
                    for (int x = 0; x <= (size - type.ByteSize); x += incSize)
                    {
                        if (_shouldStopSearch)
                            break;

                        byte[] tempArr = new byte[type.ByteSize];
                        Array.Copy(cmp, x, tempArr, 0, type.ByteSize);
                        tempArr = misc.notrevif(tempArr);

                        bool isTrue = misc.ArrayCompare(cmpArrayMin, tempArr, cmpArrayMax, Form1.compVBet);

                        //Also check if it is 4 byte aligned
                        if (isTrue && (tempArr[3] % 4) == 0)
                        {
                            resItems.Add(type.ToItem(addr + (ulong)x, tempArr, new byte[0], typeIndex, val));
                            resCnt++;
                            gotRes = true;
                        }
                    }

                    if (gotRes)
                    {
                        AddResultRange(resItems);
                        resItems.Clear();
                    }
                }

                searchListView1.EndUpdate();
                Invoke((MethodInvoker)delegate
                {
                    searchListView1.AddItemsFromList();
                });
                UpdateScanProgress(addr >= stopAddr ? realDif : (long)Math.Min(realDif, GetRealDif(startAddr, addr + (ulong)size, (ulong)size)), resCnt);
            }
        }

        void UnknownValue_InitSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args)
        {
            if (CanUseNewExactEqualSearch(typeIndex))
            {
                RunNewUnknownInitialSearch(startAddr, stopAddr, typeIndex, args);
                return;
            }

            ncSearchType type = SearchTypes[typeIndex];

            int size = 0x10000;
            byte[] cmp = new byte[size];

            int realDif = (int)GetRealDif(startAddr, stopAddr, (ulong)size);
            ulong dif = (ulong)(GetRealDif(startAddr, stopAddr, 1) / (type.ignoreAlignment ? 1 : type.ByteSize));
            string disp = "This search will have " + dif.ToString() + " results! During this search no results will be shown in the list until it is complete.\nDo you wish to continue?";
            if (MessageBox.Show(disp, "Do you wish to continue?", MessageBoxButtons.YesNo) == DialogResult.No)
            {
                return;
            }

            Invoke((MethodInvoker)delegate
            {
                searchListView1.Enabled = false;
            });

            BeginScanProgress("Initial Scan", realDif, "blocks");

            int resCnt = 0;
            int incSize = type.ByteSize;
            if (type.ignoreAlignment)
                incSize = GetAlign(1);
            List<SearchListView.SearchListViewItem> itemsToAdd = new List<SearchListView.SearchListViewItem>();

            for (ulong addr = startAddr; addr < stopAddr; addr += (ulong)size)
            {
                if (_shouldStopSearch)
                    break;

                addr = misc.ParseSchAddr(addr);
                if (Form1.apiGetMem(addr, ref cmp))
                {
                    for (int x = 0; x <= (size - type.ByteSize); x += incSize)
                    {
                        if (_shouldStopSearch)
                            break;

                        byte[] tempArr = new byte[type.ByteSize];
                        Array.Copy(cmp, x, tempArr, 0, type.ByteSize);
                        tempArr = misc.notrevif(tempArr);
                        itemsToAdd.Add(type.ToItem(addr + (ulong)x, tempArr, new byte[0], typeIndex));
                        resCnt++;
                        //gotRes = true;
                        //SetStatusLabel("Results: " + resCnt.ToString());
                    }
                    AddResultRange(itemsToAdd);
                    itemsToAdd.Clear();

                }

                UpdateScanProgress(addr >= stopAddr ? realDif : (long)Math.Min(realDif, GetRealDif(startAddr, addr + (ulong)size, (ulong)size)), resCnt);
            }

            Invoke((MethodInvoker)delegate
            {
                searchListView1.Enabled = true;
                searchListView1.AddItemsFromList();
            });
        }

        void Joker_InitSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args)
        {
            bool gotRes = false;
            ncSearchType type = SearchTypes[typeIndex];
            if (args.Length > 0)
            {
                type.Initialize((string)args[0], typeIndex);
                type = SearchTypes[typeIndex];
            }

            bool updateCont = false;
            int size = 0x10000;
            //if ((stopAddr - startAddr) <= (ulong)size)
            //    updateCont = true;

            byte[] cmp = new byte[size];

            int realDif = 0;
            if (updateCont)
                realDif = (int)(stopAddr - startAddr);
            else
                realDif = (int)GetRealDif(startAddr, stopAddr, (ulong)size);
            BeginScanProgress("Initial Scan", realDif, "blocks");

            int resCnt = 0;
            int incSize = type.ByteSize;
            if (type.ignoreAlignment)
                incSize = GetAlign(1);
            List<SearchListView.SearchListViewItem> itemsToAdd = new List<SearchListView.SearchListViewItem>();

            MessageBox.Show("Please press and hold the following button: " + Joker_ButtonChecks[0], "Button to hold", MessageBoxButtons.OK);

            for (ulong addr = startAddr; addr < stopAddr; addr += (ulong)size)
            {
                if (_shouldStopSearch)
                    break;

                gotRes = false;
                addr = misc.ParseSchAddr(addr);

                if (addr != startAddr && incSize == 1 && type.ByteSize != 1)
                {
                    addr -= (ulong)(type.ByteSize + 1);
                }

                if (Form1.apiGetMem(addr, ref cmp))
                {
                    for (int x = 0; x <= (size - type.ByteSize); x += incSize)
                    {
                        if (_shouldStopSearch)
                            break;

                        byte[] tempArr = new byte[type.ByteSize];
                        Array.Copy(cmp, x, tempArr, 0, type.ByteSize);
                        tempArr = misc.notrevif(tempArr);
                        bool isTrue = false; // misc.ArrayCompare(cmpArray, tempArr, null, cmpIntType);
                        int arrCnt = 0;
                        for (int i = 0; i < tempArr.Length; i++)
                        {
                            if (tempArr[i] != 0)
                            {
                                arrCnt++;

                                uint tempVal = BitConverter.ToUInt32(tempArr, 0);
                                //Make sure only 1 bit is set
                                if ((tempVal & (tempVal - 1)) == 0)
                                {
                                    isTrue = true;
                                }
                            }
                        }

                        if (isTrue && arrCnt == 1)
                        {
                            itemsToAdd.Add(type.ToItem(addr + (ulong)x, tempArr, new byte[0], typeIndex, 0));
                            resCnt++;
                            gotRes = true;
                            //SetStatusLabel("Results: " + resCnt.ToString());
                        }
                    }

                    if (gotRes)
                    {
                        AddResultRange(itemsToAdd);
                        itemsToAdd.Clear();
                    }

                }

                UpdateScanProgress(addr >= stopAddr ? realDif : (long)Math.Min(realDif, GetRealDif(startAddr, addr + (ulong)size, (ulong)size)), resCnt);
            }

            Invoke((MethodInvoker)delegate
            {
                searchListView1.AddItemsFromList();
            });
        }

        #endregion

        #region Next Searches

        void standardByte_NextSearch(SearchListView.SearchListViewItem[] old, string[] args, int cmpIntType)
        {
            BeginScanProgress("Next Scan", old == null ? 0 : old.Length, "candidates");
            int resCnt = 0;
            int updateCnt = 0;

            ncSearchType type = new ncSearchType();
            byte[] cmpArray = null;

            ClearItems();

            List<SearchListView.SearchListViewItem> itemsToAdd = new List<SearchListView.SearchListViewItem>();
            ulong curAddrIndex = 0;
            byte[] rec = new byte[0x10000];
            for (int cnt = 0; cnt < old.Length; cnt++)
            {
                int off = (int)(old[cnt].addr - curAddrIndex);
                if ((uint)off >= (uint)rec.Length)
                {
                    curAddrIndex = old[cnt].addr - (old[cnt].addr % (ulong)rec.Length);
                    Form1.apiGetMem(curAddrIndex, ref rec);
                    off = (int)(old[cnt].addr - curAddrIndex);
                    updateCnt++;
                }

                if (_shouldStopSearch)
                    break;

                if (type.ToByteArray == null)
                    type = SearchTypes[old[cnt].align];
                if (cmpArray == null)
                    cmpArray = type.ToByteArray((string)args[0]);

                byte[] tempArr = new byte[type.ByteSize];
                Array.Copy(rec, off, tempArr, 0, type.ByteSize);
                tempArr = misc.notrevif(tempArr);
                if (misc.ArrayCompare(cmpArray, tempArr, null, cmpIntType))
                {
                    SearchListView.SearchListViewItem item = old[cnt];
                    item.oldVal = item.newVal;
                    item.newVal = tempArr;
                    itemsToAdd.Add(item);
                    resCnt++;
                }

                if ((cnt % rec.Length) == 0 || updateCnt > 50)
                {
                    AddResultRange(itemsToAdd);
                    itemsToAdd.Clear();
                    UpdateScanProgress(cnt + 1, resCnt);
                    updateCnt = 0;
                }

            }

            if (itemsToAdd.Count > 0)
            {
                AddResultRange(itemsToAdd);
                itemsToAdd.Clear();
            }

            Invoke((MethodInvoker)delegate
            {
                searchListView1.AddItemsFromList();
            });
            CompleteScanProgress("Next Scan complete | Results: " + resCnt.ToString("N0"));
        }

        void EqualTo_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            if (HasActiveSnapshot())
            {
                RunNewSnapshotNextSearch(args, Form1.compEq);
                return;
            }

            if (activeScanUsesNewEngine && old != null && old.Length > 0 && CanUseNewExactEqualSearch(old[0].align))
            {
                RunNewBasicNextSearch(old, args, Form1.compEq);
                return;
            }

            standardByte_NextSearch(old, args, Form1.compEq);
        }

        void NotEqualTo_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            if (HasActiveSnapshot())
            {
                RunNewSnapshotNextSearch(args, Form1.compNEq);
                return;
            }

            if (activeScanUsesNewEngine && old != null && old.Length > 0 && CanUseNewExactEqualSearch(old[0].align))
            {
                RunNewBasicNextSearch(old, args, Form1.compNEq);
                return;
            }

            standardByte_NextSearch(old, args, Form1.compNEq);
        }

        void LessThan_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            standardByte_NextSearch(old, args, Form1.compLT);
        }

        void LessThanEqualTo_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            standardByte_NextSearch(old, args, Form1.compLTE);
        }

        void LessThanU_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            standardByte_NextSearch(old, args, Form1.compLTU);
        }

        void LessThanEqualToU_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            standardByte_NextSearch(old, args, Form1.compLTEU);
        }

        void GreaterThan_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            standardByte_NextSearch(old, args, Form1.compGT);
        }

        void GreaterThanEqualTo_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            standardByte_NextSearch(old, args, Form1.compGTE);
        }

        void GreaterThanU_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            standardByte_NextSearch(old, args, Form1.compGTU);
        }

        void GreaterThanEqualToU_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            standardByte_NextSearch(old, args, Form1.compGTEU);
        }

        void ValueBetween_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            if (HasActiveSnapshot())
            {
                RunNewSnapshotNextSearch(args, Form1.compVBet);
                return;
            }

            if (activeScanUsesNewEngine && old != null && old.Length > 0 && CanUseNewExactEqualSearch(old[0].align))
            {
                RunNewValueBetweenNextSearch(old, args);
                return;
            }

            BeginScanProgress("Next Scan", old == null ? 0 : old.Length, "candidates");
            int resCnt = 0;

            ncSearchType type = new ncSearchType();
            byte[] cmpArrayMin = null, cmpArrayMax = null;
            
            ClearItems();

            List<SearchListView.SearchListViewItem> itemsToAdd = new List<SearchListView.SearchListViewItem>();
            ulong curAddrIndex = 0;
            byte[] rec = new byte[0x10000];
            for (int cnt = 0; cnt < old.Length; cnt++)
            {
                int off = (int)(old[cnt].addr - curAddrIndex);
                if ((uint)off >= (uint)rec.Length)
                {
                    curAddrIndex = old[cnt].addr - (old[cnt].addr % (ulong)rec.Length);
                    Form1.apiGetMem(curAddrIndex, ref rec);
                    off = (int)(old[cnt].addr - curAddrIndex);
                }

                if (_shouldStopSearch)
                    break;

                if (type.ToByteArray == null)
                    type = SearchTypes[old[cnt].align];
                if (cmpArrayMin == null)
                    cmpArrayMin = type.ToByteArray((string)args[0]);
                if (cmpArrayMax == null)
                    cmpArrayMax = type.ToByteArray((string)args[1]);

                byte[] tempArr = new byte[type.ByteSize];
                Array.Copy(rec, off, tempArr, 0, type.ByteSize);
                tempArr = misc.notrevif(tempArr);
                if (misc.ArrayCompare(cmpArrayMin, tempArr, cmpArrayMax, Form1.compVBet))
                {
                    SearchListView.SearchListViewItem item = old[cnt];
                    item.oldVal = item.newVal;
                    item.newVal = tempArr;
                    itemsToAdd.Add(item);
                    resCnt++;
                }

                if ((cnt % rec.Length) == 0)
                {
                    AddResultRange(itemsToAdd);
                    itemsToAdd.Clear();
                    UpdateScanProgress(cnt + 1, resCnt);
                }
            }

            if (itemsToAdd.Count > 0)
            {
                AddResultRange(itemsToAdd);
                itemsToAdd.Clear();
            }

            Invoke((MethodInvoker)delegate
            {
                searchListView1.AddItemsFromList();
            });
            CompleteScanProgress("Next Scan complete | Results: " + resCnt.ToString("N0"));
        }

        void standardIncDec_NextSearch(SearchListView.SearchListViewItem[] old, string[] args, int cmpIntIndex)
        {
            BeginScanProgress("Next Scan", old == null ? 0 : old.Length, "candidates");
            int resCnt = 0;

            ncSearchType type = new ncSearchType();
            
            byte[] c = null;

            ClearItems();

            List<SearchListView.SearchListViewItem> itemsToAdd = new List<SearchListView.SearchListViewItem>();
            ulong curAddrIndex = 0;
            byte[] rec = new byte[0x10000];
            int off = 0;
            for (int cnt = 0; cnt < old.Length; cnt++)
            {
                off = (int)(old[cnt].addr - curAddrIndex);
                if ((uint)off >= (uint)rec.Length)
                {
                    curAddrIndex = old[cnt].addr - (old[cnt].addr % (ulong)rec.Length);
                    Form1.apiGetMem(curAddrIndex, ref rec);
                    off = (int)(old[cnt].addr - curAddrIndex);
                }

                if (_shouldStopSearch)
                    break;

                if (type.ToByteArray == null)
                    type = SearchTypes[old[cnt].align];
                if (args.Length > 0 && c == null)
                    c = type.ToByteArray((string)args[0]);

                byte[] tempArr = new byte[type.ByteSize];
                Array.Copy(rec, off, tempArr, 0, type.ByteSize);
                tempArr = misc.notrevif(tempArr);
                if (misc.ArrayCompare(old[cnt].newVal, tempArr, c, cmpIntIndex))
                {
                    SearchListView.SearchListViewItem item = old[cnt];
                    item.oldVal = item.newVal;
                    item.newVal = tempArr;
                    itemsToAdd.Add(item);
                    resCnt++;
                }


                if ((cnt % rec.Length) == 0)
                {
                    AddResultRange(itemsToAdd);
                    itemsToAdd.Clear();
                    UpdateScanProgress(cnt + 1, resCnt);
                }
            }

            if (itemsToAdd.Count > 0)
            {
                AddResultRange(itemsToAdd);
                itemsToAdd.Clear();
            }

            Invoke((MethodInvoker)delegate
            {
                searchListView1.AddItemsFromList();
            });
            CompleteScanProgress("Next Scan complete | Results: " + resCnt.ToString("N0"));
        }

        void Increased_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            if (HasActiveSnapshot())
            {
                RunNewSnapshotNextSearch(args, Form1.compGT);
                return;
            }

            if (activeScanUsesNewEngine && old != null && old.Length > 0 && CanUseNewExactEqualSearch(old[0].align))
            {
                RunNewDirectionNextSearch(old, true);
                return;
            }

            standardIncDec_NextSearch(old, new string[0], Form1.compGT);
        }

        void IncreasedBy_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            if (HasActiveSnapshot())
            {
                RunNewSnapshotNextSearch(args, Form1.compINC);
                return;
            }

            if (activeScanUsesNewEngine && old != null && old.Length > 0 && CanUseNewExactEqualSearch(old[0].align))
            {
                RunNewDeltaNextSearch(old, args, true);
                return;
            }

            standardIncDec_NextSearch(old, args, Form1.compINC);
        }

        void Decreased_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            if (HasActiveSnapshot())
            {
                RunNewSnapshotNextSearch(args, Form1.compLT);
                return;
            }

            if (activeScanUsesNewEngine && old != null && old.Length > 0 && CanUseNewExactEqualSearch(old[0].align))
            {
                RunNewDirectionNextSearch(old, false);
                return;
            }

            standardIncDec_NextSearch(old, new string[0], Form1.compLT);
        }

        void DecreasedBy_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            if (HasActiveSnapshot())
            {
                RunNewSnapshotNextSearch(args, Form1.compDEC);
                return;
            }

            if (activeScanUsesNewEngine && old != null && old.Length > 0 && CanUseNewExactEqualSearch(old[0].align))
            {
                RunNewDeltaNextSearch(old, args, false);
                return;
            }

            standardIncDec_NextSearch(old, args, Form1.compDEC);
        }

        void Changed_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            if (HasActiveSnapshot())
            {
                RunNewSnapshotNextSearch(args, Form1.compChg);
                return;
            }

            if (activeScanUsesNewEngine && old != null && old.Length > 0 && CanUseNewExactEqualSearch(old[0].align))
            {
                RunNewBasicNextSearch(old, new string[0], Form1.compChg);
                return;
            }

            standardIncDec_NextSearch(old, new string[0], Form1.compNEq);
        }

        void Unchanged_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            if (HasActiveSnapshot())
            {
                RunNewSnapshotNextSearch(args, Form1.compUChg);
                return;
            }

            if (activeScanUsesNewEngine && old != null && old.Length > 0 && CanUseNewExactEqualSearch(old[0].align))
            {
                RunNewBasicNextSearch(old, new string[0], Form1.compUChg);
                return;
            }

            standardIncDec_NextSearch(old, new string[0], Form1.compEq);
        }

        void Pointer_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            uint val = uint.Parse((string)args[0], System.Globalization.NumberStyles.HexNumber);

            BeginScanProgress("Next Scan", old == null ? 0 : old.Length, "candidates");
            int resCnt = 0;

            ncSearchType type = new ncSearchType();
            byte[] cmpArrayMin = null, cmpArrayMax = null;

            ClearItems();

            List<SearchListView.SearchListViewItem> itemsToAdd = new List<SearchListView.SearchListViewItem>();
            ulong curAddrIndex = 0;
            byte[] rec = new byte[0x10000];
            for (int cnt = 0; cnt < old.Length; cnt++)
            {
                int off = (int)(old[cnt].addr - curAddrIndex);
                if ((uint)off >= (uint)rec.Length)
                {
                    curAddrIndex = old[cnt].addr - (old[cnt].addr % (ulong)rec.Length);
                    Form1.apiGetMem(curAddrIndex, ref rec);
                    off = (int)(old[cnt].addr - curAddrIndex);
                }

                if (_shouldStopSearch)
                    break;

                if (type.ToByteArray == null)
                    type = SearchTypes[old[cnt].align];
                if (cmpArrayMin == null)
                    cmpArrayMin = (type.ToByteArray((val - 0x7FFF).ToString("X8")));
                if (cmpArrayMax == null)
                    cmpArrayMax = (type.ToByteArray((val + 0x7FFF).ToString("X8")));

                byte[] tempArr = new byte[type.ByteSize];
                Array.Copy(rec, off, tempArr, 0, type.ByteSize);
                tempArr = misc.notrevif(tempArr);
                if (misc.ArrayCompare(cmpArrayMin, tempArr, cmpArrayMax, Form1.compVBet) && (tempArr[3] % 4) == 0)
                {
                    SearchListView.SearchListViewItem item = old[cnt];

                    uint offset =  item.misc - (uint)misc.ByteArrayToLong(item.newVal, 0, 4);
                    uint newOff = val - (uint)misc.ByteArrayToLong(tempArr, 0, 4);
                    if (offset == newOff)
                    {
                        item.oldVal = item.newVal;
                        item.newVal = tempArr;
                        item.misc = val;
                        itemsToAdd.Add(item);
                        resCnt++;
                    }
                }

                if ((cnt % rec.Length) == 0)
                {
                    AddResultRange(itemsToAdd);
                    itemsToAdd.Clear();
                    UpdateScanProgress(cnt + 1, resCnt);
                }
            }

            if (itemsToAdd.Count > 0)
            {
                AddResultRange(itemsToAdd);
                itemsToAdd.Clear();
            }

            Invoke((MethodInvoker)delegate
            {
                searchListView1.AddItemsFromList();
            });
            CompleteScanProgress("Next Scan complete | Results: " + resCnt.ToString("N0"));
        }

        void Joker_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            BeginScanProgress("Next Scan", old == null ? 0 : old.Length, "candidates");
            int resCnt = 0;
            int updateCnt = 0;

            ncSearchType type = new ncSearchType();

            ClearItems();

            List<SearchListView.SearchListViewItem> itemsToAdd = new List<SearchListView.SearchListViewItem>();
            ulong curAddrIndex = 0;
            byte[] rec = new byte[0x10000];

            if (old.Length <= 0)
            {
                MessageBox.Show("No more results to work with. Try scanning a different range.", "Joker Finder", MessageBoxButtons.OK);
                CompleteScanProgress("Next Scan complete | Results: 0");
                return;
            }

            uint dis = old[0].misc + 1;
            if (dis >= Joker_ButtonChecks.Length)
                dis = 0;
            MessageBox.Show("Please press and hold the following button: " + Joker_ButtonChecks[dis], "Button to hold", MessageBoxButtons.OK);

            for (int cnt = 0; cnt < old.Length; cnt++)
            {
                int off = (int)(old[cnt].addr - curAddrIndex);
                if ((uint)off >= (uint)rec.Length)
                {
                    curAddrIndex = old[cnt].addr - (old[cnt].addr % (ulong)rec.Length);
                    Form1.apiGetMem(curAddrIndex, ref rec);
                    off = (int)(old[cnt].addr - curAddrIndex);
                    updateCnt++;
                }

                if (_shouldStopSearch)
                    break;

                if (type.ToByteArray == null)
                    type = SearchTypes[old[cnt].align];

                byte[] tempArr = new byte[type.ByteSize];
                Array.Copy(rec, off, tempArr, 0, type.ByteSize);
                tempArr = misc.notrevif(tempArr);

                bool isTrue = false;
                uint tempVal = BitConverter.ToUInt32(tempArr, 0);
                int cc = 0;
                if ((tempVal & (tempVal - 1)) == 0 && misc.ArrayCompare(tempArr, old[cnt].newVal, null, Form1.compNEq) && tempVal != 0)
                {
                    //Starting new check button region
                    if (((old[cnt].misc + 1) % 4) == 0)
                    {
                        isTrue = true;
                    }
                    else //same button region
                    {
                        isTrue = true;
                        for (cc = 0; cc < old[cnt].newVal.Length; cc++)
                        {
                            if (!((old[cnt].newVal[cc] == 0 && tempArr[cc] == 0) || (old[cnt].newVal[cc] != tempArr[cc])))
                            {
                                isTrue = false;
                                break;
                            }
                        }
                    }
                }

                if (isTrue)
                {
                    SearchListView.SearchListViewItem item = old[cnt];
                    item.oldVal = item.newVal;
                    item.newVal = tempArr;
                    item.misc = old[cnt].misc + 1;
                    if (item.misc >= Joker_ButtonChecks.Length)
                        item.misc = 0;
                    itemsToAdd.Add(item);
                    resCnt++;
                }

                if ((cnt % rec.Length) == 0 || updateCnt > 50)
                {
                    AddResultRange(itemsToAdd);
                    itemsToAdd.Clear();
                    UpdateScanProgress(cnt + 1, resCnt);
                    updateCnt = 0;
                }

            }

            if (itemsToAdd.Count > 0)
            {
                AddResultRange(itemsToAdd);
                itemsToAdd.Clear();
            }

            Invoke((MethodInvoker)delegate
            {
                searchListView1.AddItemsFromList();
            });
            CompleteScanProgress("Next Scan complete | Results: " + resCnt.ToString("N0"));
        }

        void BitDif_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            BeginScanProgress("Next Scan", old == null ? 0 : old.Length, "candidates");
            int resCnt = 0;

            ncSearchType type = new ncSearchType();

            ClearItems();

            List<SearchListView.SearchListViewItem> itemsToAdd = new List<SearchListView.SearchListViewItem>();
            ulong curAddrIndex = 0;
            byte[] rec = new byte[0x10000];

            int bitDifCount = 0;
            if (args.Length > 0)
            {
                bitDifCount = int.Parse((string)args[0], System.Globalization.NumberStyles.HexNumber);
            }

            for (int cnt = 0; cnt < old.Length; cnt++)
            {
                int off = (int)(old[cnt].addr - curAddrIndex);
                if ((uint)off >= (uint)rec.Length)
                {
                    curAddrIndex = old[cnt].addr - (old[cnt].addr % (ulong)rec.Length);
                    Form1.apiGetMem(curAddrIndex, ref rec);
                    off = (int)(old[cnt].addr - curAddrIndex);
                }

                if (_shouldStopSearch)
                    break;

                if (type.ToByteArray == null)
                    type = SearchTypes[old[cnt].align];

                byte[] tempArr = new byte[type.ByteSize];
                Array.Copy(rec, off, tempArr, 0, type.ByteSize);
                tempArr = misc.notrevif(tempArr);

                bool isTrue = false;
                int cc = 0;
                if (misc.ArrayCompare(tempArr, old[cnt].newVal, null, Form1.compNEq))
                {
                    int difCnt = 0;
                    byte[] res = new byte[tempArr.Length];
                    for (cc = 0; cc < res.Length; cc++)
                    {
                        res[cc] = (byte)(tempArr[cc] ^ old[cnt].newVal[cc]);
                        while (res[cc] != 0)
                        {
                            difCnt++;
                            res[cc] &= (byte)(res[cc] - 1);
                        }
                    }

                    if (difCnt == bitDifCount)
                        isTrue = true;
                }

                if (isTrue)
                {
                    SearchListView.SearchListViewItem item = old[cnt];
                    item.oldVal = item.newVal;
                    item.newVal = tempArr;
                    item.misc = old[cnt].misc + 1;
                    if (item.misc >= Joker_ButtonChecks.Length)
                        item.misc = 0;
                    itemsToAdd.Add(item);
                    resCnt++;
                }

                if ((cnt % rec.Length) == 0)
                {
                    AddResultRange(itemsToAdd);
                    itemsToAdd.Clear();
                    UpdateScanProgress(cnt + 1, resCnt);
                }

            }

            if (itemsToAdd.Count > 0)
            {
                AddResultRange(itemsToAdd);
                itemsToAdd.Clear();
            }

            Invoke((MethodInvoker)delegate
            {
                searchListView1.AddItemsFromList();
            });
            CompleteScanProgress("Next Scan complete | Results: " + resCnt.ToString("N0"));
        }

        #endregion
    }
}
