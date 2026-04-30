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

        private const ulong RangeFinderProbeStep = 0x100000;
        private const ulong RangeFinderDeepProbeStep = 0x10000;
        private const int RangeFinderProbeSize = 0x10;
        private const ulong RangeFinderEdgeRefineStep = 0x10000;
        private const ulong RangeFinderAddressSpaceEnd = 0x100000000;
        private const ulong RangeFinderLargeRangeThreshold = 0x1000000;
        private const ulong RangeFinderLargeRangeSampleStep = 0x400000;
        private const int RangeFinderUiStepProbes = 32;

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

        private sealed class RangeFinderStats
        {
            public long ProbeOk;
            public long ProbeFailed;
            public long EdgeRefineProbes;
            public long SeedsFound;
            public long RangesMerged;
            public long MainProbesCompleted;
            public readonly Stopwatch Elapsed = new Stopwatch();

            public long TotalProbes
            {
                get { return ProbeOk + ProbeFailed; }
            }
        }

        private sealed class RangeFinderProbeCache
        {
            private readonly Form1 owner;
            private readonly Dictionary<ulong, bool> probes = new Dictionary<ulong, bool>();
            private readonly byte[] probeBuffer = new byte[RangeFinderProbeSize];
            private readonly RangeFinderStats stats;

            public RangeFinderProbeCache(Form1 owner, RangeFinderStats stats)
            {
                this.owner = owner;
                this.stats = stats;
            }

            public bool Probe(ulong addr)
            {
                addr = owner.AlignDown(addr, RangeFinderEdgeRefineStep);
                if (addr >= RangeFinderAddressSpaceEnd)
                    return false;

                bool cached;
                if (probes.TryGetValue(addr, out cached))
                    return cached;

                bool ok = false;
                byte[] read = probeBuffer;

                try
                {
                    ok = apiGetMem(addr, ref read);
                }
                catch (Exception ex)
                {
                    try { CrashLogger.Log("Form1.RangeFinderProbe @0x" + addr.ToString("X8"), ex); } catch { }
                    ok = false;
                }

                probes[addr] = ok;

                if (ok)
                    stats.ProbeOk++;
                else
                    stats.ProbeFailed++;

                return ok;
            }
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

        private string GetRangeFinderModeText(bool deepScan)
        {
            return deepScan ? "Deep adaptive 64 KB map" : "Adaptive 1 MB map";
        }

        private string FormatRangeFinderStatus(string prefix, RangeFinderStats stats, List<RangeFinderRange> ranges, ulong addr)
        {
            return prefix + " @ 0x" + addr.ToString("X8") +
                " | Seeds: " + stats.SeedsFound.ToString("N0") +
                " | Ranges: " + ranges.Count.ToString("N0") +
                " | OK: " + stats.ProbeOk.ToString("N0") +
                " | Failed: " + stats.ProbeFailed.ToString("N0");
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

        private void FinishFindRangesUi(ListViewItem[] ranges, bool cancelled, RangeFinderStats stats, bool deepScan)
        {
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    FinishFindRangesUi(ranges, cancelled, stats, deepScan);
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

            string modeText = GetRangeFinderModeText(deepScan);
            double seconds = stats.Elapsed.Elapsed.TotalSeconds;
            if (seconds <= 0.0)
                seconds = 0.001;
            double probesPerSecond = stats.TotalProbes / seconds;

            if (cancelled)
            {
                statusLabel1.Text = "Find Ranges stopped | Mode: " + modeText +
                                    " | OK: " + stats.ProbeOk.ToString("N0") +
                                    " | Failed: " + stats.ProbeFailed.ToString("N0");
            }
            else
            {
                statusLabel1.Text = "Find Ranges completed | Mode: " + modeText + " | Ranges: " +
                                    (ranges == null ? 0 : ranges.Length).ToString("N0") +
                                    " | OK: " + stats.ProbeOk.ToString("N0") +
                                    " | Failed: " + stats.ProbeFailed.ToString("N0") +
                                    " | " + probesPerSecond.ToString("N1") + " probes/sec";
            }

            if (!cancelled)
            {
                MessageBox.Show(
                    "Find Ranges completed.\r\n\r\n" +
                    "Mode: " + modeText + "\r\n" +
                    "Ranges found: " + (ranges == null ? 0 : ranges.Length).ToString("N0") + "\r\n" +
                    "Probe step: 0x" + (deepScan ? RangeFinderDeepProbeStep : RangeFinderProbeStep).ToString("X") + "\r\n" +
                    "Probe size: 0x" + RangeFinderProbeSize.ToString("X") + "\r\n" +
                    "Edge refine probes: " + stats.EdgeRefineProbes.ToString("N0") + "\r\n" +
                    "Seeds found: " + stats.SeedsFound.ToString("N0") + "\r\n" +
                    "Ranges merged: " + stats.RangesMerged.ToString("N0") + "\r\n" +
                    "Probe OK: " + stats.ProbeOk.ToString("N0") + "\r\n" +
                    "Probe failed: " + stats.ProbeFailed.ToString("N0") + "\r\n" +
                    "Elapsed: " + stats.Elapsed.Elapsed.TotalSeconds.ToString("0.000") + " sec\r\n" +
                    "Probes/sec: " + probesPerSecond.ToString("N1"),
                    "NetCheatPS3",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private ulong RefineRangeStart(ulong failedBoundary, ulong readableBoundary, RangeFinderProbeCache probes, RangeFinderStats stats)
        {
            if (readableBoundary == 0)
                return 0;

            ulong start = failedBoundary >= RangeFinderAddressSpaceEnd
                ? 0
                : failedBoundary + RangeFinderEdgeRefineStep;

            if (start > readableBoundary)
                start = readableBoundary;

            for (ulong addr = start; addr <= readableBoundary; addr += RangeFinderEdgeRefineStep)
            {
                if (findRangesCancel)
                    return readableBoundary;

                stats.EdgeRefineProbes++;
                if (probes.Probe(addr))
                    return addr;

                if (addr > RangeFinderAddressSpaceEnd - RangeFinderEdgeRefineStep)
                    break;
            }

            return readableBoundary;
        }

        private ulong RefineRangeEnd(ulong readableBoundary, ulong failedBoundary, RangeFinderProbeCache probes, RangeFinderStats stats)
        {
            ulong endLimit = failedBoundary > RangeFinderAddressSpaceEnd ? RangeFinderAddressSpaceEnd : failedBoundary;
            ulong lastReadable = readableBoundary;

            if (readableBoundary > RangeFinderAddressSpaceEnd - RangeFinderEdgeRefineStep)
                return RangeFinderAddressSpaceEnd;

            for (ulong addr = readableBoundary + RangeFinderEdgeRefineStep; addr < endLimit; addr += RangeFinderEdgeRefineStep)
            {
                if (findRangesCancel)
                    break;

                stats.EdgeRefineProbes++;
                if (!probes.Probe(addr))
                    break;

                lastReadable = addr;

                if (addr > RangeFinderAddressSpaceEnd - RangeFinderEdgeRefineStep)
                    break;
            }

            ulong end = lastReadable + RangeFinderEdgeRefineStep;
            return end > RangeFinderAddressSpaceEnd ? RangeFinderAddressSpaceEnd : end;
        }

        private void DiscoverRangeFromSeed(ulong seed, RangeFinderProbeCache probes, RangeFinderStats stats, List<RangeFinderRange> foundRanges)
        {
            ulong roughStart = seed;
            ulong failedBeforeStart = RangeFinderAddressSpaceEnd;

            while (roughStart >= RangeFinderProbeStep)
            {
                if (findRangesCancel)
                    return;

                ulong prev = roughStart - RangeFinderProbeStep;
                if (!probes.Probe(prev))
                {
                    failedBeforeStart = prev;
                    break;
                }

                roughStart = prev;
            }

            ulong roughEndReadable = seed;
            ulong failedAfterEnd = RangeFinderAddressSpaceEnd;

            while (roughEndReadable <= RangeFinderAddressSpaceEnd - RangeFinderProbeStep)
            {
                if (findRangesCancel)
                    return;

                ulong next = roughEndReadable + RangeFinderProbeStep;
                if (!probes.Probe(next))
                {
                    failedAfterEnd = next;
                    break;
                }

                roughEndReadable = next;
            }

            ulong refinedStart = RefineRangeStart(failedBeforeStart, roughStart, probes, stats);
            ulong refinedEnd = RefineRangeEnd(roughEndReadable, failedAfterEnd, probes, stats);
            int before = foundRanges.Count;
            AddOrMergeRange(foundRanges, refinedStart, refinedEnd);
            if (foundRanges.Count <= before)
                stats.RangesMerged++;
        }

        private List<RangeFinderRange> VerifyAndSplitLargeRanges(List<RangeFinderRange> ranges, RangeFinderProbeCache probes)
        {
            List<RangeFinderRange> verified = new List<RangeFinderRange>(ranges.Count);

            for (int i = 0; i < ranges.Count; i++)
            {
                RangeFinderRange range = ranges[i];
                if ((range.End - range.Start) <= RangeFinderLargeRangeThreshold)
                {
                    verified.Add(range);
                    continue;
                }

                ulong segmentStart = range.Start;
                int consecutiveFailures = 0;

                for (ulong addr = AlignUp(range.Start + RangeFinderLargeRangeSampleStep, RangeFinderLargeRangeSampleStep);
                    addr < range.End;
                    addr += RangeFinderLargeRangeSampleStep)
                {
                    if (findRangesCancel)
                        break;

                    bool ok = probes.Probe(addr);
                    if (ok)
                    {
                        consecutiveFailures = 0;
                        continue;
                    }

                    consecutiveFailures++;
                    if (consecutiveFailures < 2)
                        continue;

                    ulong splitStart = addr - RangeFinderLargeRangeSampleStep;
                    if (splitStart > segmentStart)
                        verified.Add(new RangeFinderRange(segmentStart, splitStart));

                    segmentStart = addr + RangeFinderLargeRangeSampleStep;
                    consecutiveFailures = 0;
                }

                if (segmentStart < range.End)
                    verified.Add(new RangeFinderRange(segmentStart, range.End));
            }

            return verified;
        }

        private void RunAdaptiveRangeMap(bool deepScan, RangeFinderProbeCache probes, RangeFinderStats stats, List<RangeFinderRange> foundRanges)
        {
            ulong step = deepScan ? RangeFinderDeepProbeStep : RangeFinderProbeStep;
            int progressMax = (int)(RangeFinderAddressSpaceEnd / step);
            int progress = 0;
            string modeText = GetRangeFinderModeText(deepScan);

            SetFindRangesUiState(true, 0, progressMax, "Starting " + modeText + "...");

            for (ulong addr = 0; addr < RangeFinderAddressSpaceEnd; addr += step)
            {
                if (findRangesCancel)
                    return;

                if (!IsInsideKnownRange(foundRanges, addr))
                {
                    if (probes.Probe(addr))
                    {
                        stats.SeedsFound++;
                        DiscoverRangeFromSeed(addr, probes, stats, foundRanges);
                    }
                }

                progress++;
                stats.MainProbesCompleted = progress;

                if ((progress % RangeFinderUiStepProbes) == 0 || progress == progressMax)
                {
                    SetFindRangesUiState(
                        true,
                        progress,
                        progressMax,
                        FormatRangeFinderStatus(modeText, stats, foundRanges, addr));
                }

                if (addr > RangeFinderAddressSpaceEnd - step)
                    break;
            }
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
            bool deepScan = workerArg is bool && (bool)workerArg;
            bool cancelled = false;
            RangeFinderStats stats = new RangeFinderStats();

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

                stats.Elapsed.Start();
                RangeFinderProbeCache probes = new RangeFinderProbeCache(this, stats);
                RunAdaptiveRangeMap(deepScan, probes, stats, foundRanges);

                if (findRangesCancel)
                    cancelled = true;

                if (!cancelled)
                    foundRanges = VerifyAndSplitLargeRanges(foundRanges, probes);

                stats.Elapsed.Stop();

                FinishFindRangesUi(ConvertRangesToListViewItems(foundRanges), cancelled, stats, deepScan);
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

            bool deepScan = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;

            findRangesCancel = false;
            rangeView.Items.Clear();
            UpdateMemArray();

            SetFindRangesUiState(true, 0, 1, deepScan ? "Starting deep adaptive 64 KB map..." : "Starting adaptive 1 MB map...");
            if (deepScan)
                MessageBox.Show(
                    "Deep adaptive scans the full 32-bit address space in 64 KB steps using tiny probe reads. This is slower than normal adaptive mapping.",
                    "Find Ranges",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

            findRangesThread = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(FindRangesWorker));
            findRangesThread.IsBackground = true;
            findRangesThread.Priority = System.Threading.ThreadPriority.BelowNormal;
            findRangesThread.Start(deepScan);
        }
    }
}
