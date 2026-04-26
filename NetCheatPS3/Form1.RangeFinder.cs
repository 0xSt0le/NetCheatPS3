using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace NetCheatPS3
{
    public partial class Form1
    {
        private Button probeAddressButton = null;
        private ulong lastProbeAddress = 0;
        private int lastProbeSize = 0x10;
        private volatile bool findRangesCancel = false;
        private System.Threading.Thread findRangesThread = null;

        private const ulong RangeFinderBlockSize = 0x10000;
        private const ulong RangeFinderProbeStep = 0x100000;
        private const int RangeFinderUiStepProbes = 32;

        private sealed class RangeFinderBand
        {
            public ulong Start;
            public ulong End;
            public string Name;

            public RangeFinderBand(ulong start, ulong end, string name)
            {
                Start = start;
                End = end;
                Name = name;
            }
        }

        private sealed class RangeFinderRange
        {
            public ulong Start;
            public ulong End;

            public RangeFinderRange(ulong start, ulong end)
            {
                Start = start;
                End = end;
            }
        }

        private RangeFinderBand[] GetSmartRangeFinderBands(bool fullScan)
        {
            if (fullScan)
            {
                return new RangeFinderBand[]
                {
                    new RangeFinderBand(0x00000000, 0x100000000, "Full 32-bit process space")
                };
            }

            return new RangeFinderBand[]
            {
                // Native PS3 titles normally expose the useful process memory in low 32-bit virtual space.
                // This also includes the traditional 00010000-04000000 NetCheat style ranges.
                new RangeFinderBand(0x00000000, 0x10000000, "Primary native PS3 process space"),

                // PS2 Classics / emulator-backed targets often expose EE-style mirrors here.
                // This includes 0x20000000, where Jak PS2 values commonly show up.
                new RangeFinderBand(0x10000000, 0x34000000, "High process / PS2 Classics mirror space"),

                // PS2 EE scratchpad virtual region. Tiny, but cheap to test.
                new RangeFinderBand(0x70000000, 0x70010000, "PS2 scratchpad virtual space")
            };
        }

        private ulong AlignDown(ulong value, ulong alignment)
        {
            if (alignment == 0)
                return value;
            return value - (value % alignment);
        }

        private ulong AlignUp(ulong value, ulong alignment)
        {
            if (alignment == 0)
                return value;
            ulong mod = value % alignment;
            if (mod == 0)
                return value;
            return value + (alignment - mod);
        }

        private bool TryReadRangeBlock(ulong addr, byte[] block, ref long readOk, ref long readFailed)
        {
            bool validRegion = false;

            try
            {
                validRegion = apiGetMem(addr, ref block);
            }
            catch (Exception ex)
            {
                try { CrashLogger.Log("Form1.TryReadRangeBlock.apiGetMem @0x" + addr.ToString("X8"), ex); } catch { }
                validRegion = false;
            }

            if (validRegion)
                readOk++;
            else
                readFailed++;

            return validRegion;
        }

        private void AddOrMergeRange(List<RangeFinderRange> ranges, ulong start, ulong end)
        {
            if (end <= start)
                return;

            for (int i = 0; i < ranges.Count; i++)
            {
                RangeFinderRange r = ranges[i];

                if (end < r.Start || start > r.End)
                    continue;

                if (start < r.Start)
                    r.Start = start;
                if (end > r.End)
                    r.End = end;

                // Merge any following ranges that now overlap/abut.
                for (int j = ranges.Count - 1; j >= 0; j--)
                {
                    if (j == i)
                        continue;

                    RangeFinderRange other = ranges[j];
                    if (other.End < r.Start || other.Start > r.End)
                        continue;

                    if (other.Start < r.Start)
                        r.Start = other.Start;
                    if (other.End > r.End)
                        r.End = other.End;

                    ranges.RemoveAt(j);
                    if (j < i)
                        i--;
                }

                return;
            }

            ranges.Add(new RangeFinderRange(start, end));
        }

        private bool IsInsideKnownRange(List<RangeFinderRange> ranges, ulong addr)
        {
            for (int i = 0; i < ranges.Count; i++)
            {
                if (addr >= ranges[i].Start && addr < ranges[i].End)
                    return true;
            }

            return false;
        }

        private void SetFindRangesUiState(bool scanning, int progressValue, int progressMax, string statusText)
        {
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    SetFindRangesUiState(scanning, progressValue, progressMax, statusText);
                });
                return;
            }

            if (progressMax <= 0)
                progressMax = 1;

            findRanges.Text = scanning ? "Stop" : "Find Ranges";
            findRangeProgBar.Maximum = progressMax;

            if (progressValue < 0)
                progressValue = 0;
            if (progressValue > progressMax)
                progressValue = progressMax;

            findRangeProgBar.Value = progressValue;
            statusLabel1.Text = statusText;
        }

        private void FinishFindRangesUi(ListViewItem[] ranges, bool cancelled, long readOk, long readFailed, bool fullScan)
        {
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    FinishFindRangesUi(ranges, cancelled, readOk, readFailed, fullScan);
                });
                return;
            }

            rangeView.BeginUpdate();
            rangeView.Items.Clear();

            if (ranges != null && ranges.Length > 0)
                rangeView.Items.AddRange(ranges);

            rangeView.EndUpdate();

            UpdateMemArray();

            findRanges.Text = "Find Ranges";
            findRangeProgBar.Value = 0;

            string modeText = fullScan ? "Full adaptive" : "Smart adaptive";
            if (cancelled)
            {
                statusLabel1.Text = "Find Ranges stopped | " + modeText + " | OK: " + readOk.ToString("N0") + " | Failed: " + readFailed.ToString("N0");
            }
            else
            {
                statusLabel1.Text = "Find Ranges completed | " + modeText + " | Ranges: " +
                                    (ranges == null ? 0 : ranges.Length).ToString("N0") +
                                    " | OK: " + readOk.ToString("N0") +
                                    " | Failed: " + readFailed.ToString("N0");
            }

            if (!cancelled)
            {
                MessageBox.Show(
                    "Find Ranges completed.\r\n\r\n" +
                    "Mode: " + modeText + "\r\n" +
                    "Ranges found: " + (ranges == null ? 0 : ranges.Length).ToString("N0") + "\r\n" +
                    "Readable probes/blocks: " + readOk.ToString("N0") + "\r\n" +
                    "Failed probes/blocks: " + readFailed.ToString("N0") + "\r\n\r\n" +
                    "Smart mode scans likely PS3/PS2 process address bands first and avoids a blind 4 GB sweep.\r\n" +
                    "Shift-click Find Ranges to force a full adaptive 32-bit scan.",
                    "NetCheatPS3",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private void RefineReadableRangeAroundProbe(ulong probeAddr, ulong bandStart, ulong bandEnd, byte[] block, List<RangeFinderRange> foundRanges, ref long readOk, ref long readFailed)
        {
            ulong start = probeAddr;
            ulong end = probeAddr + RangeFinderBlockSize;

            // Walk backward block-by-block until the readable area starts.
            while (start >= bandStart + RangeFinderBlockSize)
            {
                ulong prev = start - RangeFinderBlockSize;
                if (!TryReadRangeBlock(prev, block, ref readOk, ref readFailed))
                    break;
                start = prev;
            }

            // Walk forward block-by-block until the readable area ends.
            ulong cur = probeAddr + RangeFinderBlockSize;
            while (cur < bandEnd)
            {
                if (IsInsideKnownRange(foundRanges, cur))
                {
                    cur += RangeFinderBlockSize;
                    continue;
                }

                if (!TryReadRangeBlock(cur, block, ref readOk, ref readFailed))
                    break;

                end = cur + RangeFinderBlockSize;
                cur += RangeFinderBlockSize;
            }

            if (end > bandEnd)
                end = bandEnd;

            AddOrMergeRange(foundRanges, start, end);
        }

        private void ScanRangeFinderBand(RangeFinderBand band, byte[] block, List<RangeFinderRange> foundRanges, ref long readOk, ref long readFailed, ref int progress, int progressMax)
        {
            ulong bandStart = AlignDown(band.Start, RangeFinderBlockSize);
            ulong bandEnd = AlignUp(band.End, RangeFinderBlockSize);
            if (bandEnd <= bandStart)
                return;

            // Two offsets catch ranges that sit between 1 MB probe boundaries without doing a true 64 KB deep sweep.
            ulong[] offsets = new ulong[] { 0, RangeFinderProbeStep / 2 };

            for (int pass = 0; pass < offsets.Length; pass++)
            {
                ulong addr = bandStart + offsets[pass];
                if (addr >= bandEnd)
                    continue;

                addr = AlignDown(addr, RangeFinderBlockSize);

                for (; addr < bandEnd; addr += RangeFinderProbeStep)
                {
                    if (findRangesCancel)
                        return;

                    if (IsInsideKnownRange(foundRanges, addr))
                    {
                        progress++;
                        continue;
                    }

                    bool valid = TryReadRangeBlock(addr, block, ref readOk, ref readFailed);

                    if (valid)
                    {
                        RefineReadableRangeAroundProbe(addr, bandStart, bandEnd, block, foundRanges, ref readOk, ref readFailed);
                    }

                    progress++;

                    if ((progress % RangeFinderUiStepProbes) == 0)
                    {
                        SetFindRangesUiState(
                            true,
                            progress,
                            progressMax,
                            "Smart range scan: " + band.Name + " @ 0x" + addr.ToString("X8") +
                            " | Ranges: " + foundRanges.Count.ToString("N0") +
                            " | OK: " + readOk.ToString("N0") +
                            " | Failed: " + readFailed.ToString("N0"));
                    }
                }
            }
        }

        private int EstimateRangeFinderProgressMax(RangeFinderBand[] bands)
        {
            long total = 0;

            for (int i = 0; i < bands.Length; i++)
            {
                ulong size = bands[i].End > bands[i].Start ? bands[i].End - bands[i].Start : 0;
                total += (long)((size + RangeFinderProbeStep - 1) / RangeFinderProbeStep) * 2;
            }

            if (total <= 0)
                total = 1;
            if (total > Int32.MaxValue)
                total = Int32.MaxValue;

            return (int)total;
        }

        private ListViewItem[] ConvertRangesToListViewItems(List<RangeFinderRange> ranges)
        {
            ranges.Sort(delegate(RangeFinderRange a, RangeFinderRange b)
            {
                if (a.Start < b.Start)
                    return -1;
                if (a.Start > b.Start)
                    return 1;
                return 0;
            });

            List<ListViewItem> items = new List<ListViewItem>(ranges.Count);

            for (int i = 0; i < ranges.Count; i++)
            {
                string[] rangeText = new string[2];
                rangeText[0] = ranges[i].Start.ToString("X8");
                rangeText[1] = ranges[i].End.ToString("X8");
                items.Add(new ListViewItem(rangeText));
            }

            return items.ToArray();
        }

        private void FindRangesWorker(object workerArg)
        {
            bool fullScan = workerArg is bool && (bool)workerArg;
            bool cancelled = false;
            long readOk = 0;
            long readFailed = 0;

            List<RangeFinderRange> foundRanges = new List<RangeFinderRange>(32);

            try
            {
                bool wasStopped = false;
                try
                {
                    wasStopped = isProcessStopped();
                }
                catch
                {
                    wasStopped = false;
                }

                if (!codes.ConnectAndAttach(wasStopped))
                {
                    SetFindRangesUiState(false, 0, 1, "Find Ranges failed: unable to connect/attach");
                    return;
                }

                RangeFinderBand[] bands = GetSmartRangeFinderBands(fullScan);
                int progressMax = EstimateRangeFinderProgressMax(bands);
                int progress = 0;

                byte[] block = new byte[RangeFinderBlockSize];

                SetFindRangesUiState(true, 0, progressMax, fullScan ? "Starting full adaptive range scan..." : "Starting smart adaptive range scan...");

                for (int i = 0; i < bands.Length; i++)
                {
                    if (findRangesCancel)
                    {
                        cancelled = true;
                        break;
                    }

                    ScanRangeFinderBand(bands[i], block, foundRanges, ref readOk, ref readFailed, ref progress, progressMax);
                }

                if (findRangesCancel)
                    cancelled = true;

                FinishFindRangesUi(ConvertRangesToListViewItems(foundRanges), cancelled, readOk, readFailed, fullScan);
            }
            catch (Exception ex)
            {
                try { CrashLogger.Log("Form1.FindRangesWorker", ex); } catch { }
                SetFindRangesUiState(false, 0, 1, "Find Ranges crashed. See NetCheatPS3_crash.log");
            }
            finally
            {
                findRangesCancel = false;
                findRangesThread = null;
            }
        }

        private void CreateProbeAddressButton()
        {
            if (probeAddressButton != null)
                return;

            probeAddressButton = new Button();
            probeAddressButton.Text = "Probe Address";
            probeAddressButton.UseVisualStyleBackColor = true;
            probeAddressButton.BackColor = ncBackColor;
            probeAddressButton.ForeColor = ncForeColor;
            probeAddressButton.Click += new EventHandler(ProbeAddress_Click);

            RangeTab.Controls.Add(probeAddressButton);
            probeAddressButton.BringToFront();

            try
            {
                Form1_Resize(this, EventArgs.Empty);
            }
            catch
            {
            }
        }

        private bool TryParseProbeHex(string text, out ulong value)
        {
            value = 0;

            if (text == null)
                return false;

            text = text.Trim();

            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                text = text.Substring(2);

            if (text.Length == 0)
                return false;

            return ulong.TryParse(
                text,
                System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture,
                out value);
        }

        private string FormatProbeHexLines(byte[] bytes, int maxBytes)
        {
            if (bytes == null)
                return "";

            int count = Math.Min(bytes.Length, maxBytes);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            for (int i = 0; i < count; i++)
            {
                if ((i % 16) == 0)
                {
                    if (i != 0)
                        sb.AppendLine();

                    sb.Append(i.ToString("X4"));
                    sb.Append(": ");
                }

                sb.Append(bytes[i].ToString("X2"));
                sb.Append(' ');
            }

            if (bytes.Length > maxBytes)
            {
                sb.AppendLine();
                sb.Append("... truncated in popup, full details copied to clipboard ...");
            }

            return sb.ToString();
        }

        private string FormatProbeHexCompact(byte[] bytes)
        {
            if (bytes == null)
                return "";

            System.Text.StringBuilder sb = new System.Text.StringBuilder(bytes.Length * 2);

            for (int i = 0; i < bytes.Length; i++)
                sb.Append(bytes[i].ToString("X2"));

            return sb.ToString();
        }

        private uint ReadProbeUInt32BE(byte[] bytes)
        {
            return ((uint)bytes[0] << 24) |
                   ((uint)bytes[1] << 16) |
                   ((uint)bytes[2] << 8) |
                   bytes[3];
        }

        private uint ReadProbeUInt32LE(byte[] bytes)
        {
            return ((uint)bytes[3] << 24) |
                   ((uint)bytes[2] << 16) |
                   ((uint)bytes[1] << 8) |
                   bytes[0];
        }

        private float UInt32BitsToFloat(uint value)
        {
            byte[] tmp = BitConverter.GetBytes(value);

            if (!BitConverter.IsLittleEndian)
                Array.Reverse(tmp);

            return BitConverter.ToSingle(tmp, 0);
        }

        private void ProbeAddress_Click(object sender, EventArgs e)
        {
            if (!EnsureConnectedAndAttachedForMainAction("Probe Address"))
                return;

            if (curAPI == null)
            {
                MessageBox.Show("No API selected!");
                return;
            }

            if (!connected)
            {
                MessageBox.Show("Not connected to the PS3!");
                return;
            }

            IBArg[] args = new IBArg[2];

            args[0].label = "Address";
            args[0].defStr = lastProbeAddress.ToString("X8");

            args[1].label = "Size";
            args[1].defStr = lastProbeSize.ToString("X");

            args = CallIBox(args);

            if (args == null)
                return;

            ulong addr;
            ulong size64;

            if (!TryParseProbeHex(args[0].retStr, out addr))
            {
                MessageBox.Show("Invalid address. Enter hex, for example 20F9FE50.");
                return;
            }

            if (!TryParseProbeHex(args[1].retStr, out size64))
            {
                MessageBox.Show("Invalid size. Enter hex, for example 10 for 16 bytes.");
                return;
            }

            if (size64 == 0)
            {
                MessageBox.Show("Size must be greater than zero.");
                return;
            }

            if (size64 > 0x1000)
            {
                MessageBox.Show("Probe size is limited to 0x1000 bytes.");
                return;
            }

            lastProbeAddress = addr;
            lastProbeSize = (int)size64;

            byte[] data = new byte[(int)size64];

            Stopwatch sw = new Stopwatch();
            sw.Start();

            bool ok = false;
            Exception caught = null;

            try
            {
                ok = apiGetMem(addr, ref data);
            }
            catch (Exception ex)
            {
                caught = ex;
                ok = false;
            }

            sw.Stop();

            if (!ok)
            {
                string failMsg =
                    "READ FAILED\r\n\r\n" +
                    "Address: 0x" + addr.ToString("X8") + "\r\n" +
                    "Size: 0x" + size64.ToString("X") + "\r\n" +
                    "Time: " + sw.ElapsedMilliseconds.ToString() + " ms";

                if (caught != null)
                    failMsg += "\r\n\r\nException:\r\n" + caught.Message;

                statusLabel1.Text = "Probe failed at 0x" + addr.ToString("X8");
                MessageBox.Show(failMsg, "Probe Address", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string rawCompact = FormatProbeHexCompact(data);

            System.Text.StringBuilder full = new System.Text.StringBuilder();
            full.AppendLine("READ OK");
            full.AppendLine();
            full.AppendLine("Address: 0x" + addr.ToString("X8"));
            full.AppendLine("Size: 0x" + size64.ToString("X") + " (" + size64.ToString() + " bytes)");
            full.AppendLine("Time: " + sw.ElapsedMilliseconds.ToString() + " ms");
            full.AppendLine();
            full.AppendLine("Raw bytes:");
            full.AppendLine(FormatProbeHexLines(data, 256));
            full.AppendLine();
            full.AppendLine("Raw compact:");
            full.AppendLine(rawCompact);

            if (data.Length >= 4)
            {
                uint u32be = ReadProbeUInt32BE(data);
                uint u32le = ReadProbeUInt32LE(data);

                full.AppendLine();
                full.AppendLine("First 4 bytes interpreted:");
                full.AppendLine("U32 BE : 0x" + u32be.ToString("X8") + " (" + u32be.ToString() + ")");
                full.AppendLine("U32 LE : 0x" + u32le.ToString("X8") + " (" + u32le.ToString() + ")");
                full.AppendLine("F32 BE : " + UInt32BitsToFloat(u32be).ToString("G9", System.Globalization.CultureInfo.InvariantCulture));
                full.AppendLine("F32 LE : " + UInt32BitsToFloat(u32le).ToString("G9", System.Globalization.CultureInfo.InvariantCulture));
            }

            try
            {
                Clipboard.SetText(full.ToString());
            }
            catch
            {
            }

            statusLabel1.Text = "Probe OK at 0x" + addr.ToString("X8") + " | " + size64.ToString() + " bytes | " + sw.ElapsedMilliseconds.ToString() + " ms";

            MessageBox.Show(
                full.ToString() + "\r\n\r\nFull probe details copied to clipboard.",
                "Probe Address",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void findRanges_Click(object sender, EventArgs e)
        {
            if (!EnsureConnectedAndAttachedForMainAction("Find Ranges"))
                return;

            if (curAPI == null)
            {
                MessageBox.Show("No API selected!");
                return;
            }

            if (findRangesThread != null && findRangesThread.IsAlive)
            {
                findRangesCancel = true;
                statusLabel1.Text = "Stopping Find Ranges...";
                return;
            }

            bool fullScan = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;

            findRangesCancel = false;
            rangeView.Items.Clear();
            UpdateMemArray();

            SetFindRangesUiState(true, 0, 1, fullScan ? "Starting full adaptive range scan..." : "Starting smart adaptive range scan...");

            findRangesThread = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(FindRangesWorker));
            findRangesThread.IsBackground = true;
            findRangesThread.Priority = System.Threading.ThreadPriority.BelowNormal;
            findRangesThread.Start(fullScan);
        }
    }
}
