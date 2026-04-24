using System;
using System.Text;
using System.Windows.Forms;
using NetCheatPS3.Scanner;

namespace NetCheatPS3
{
    public partial class SearchControl
    {
        private string lastScanKind = "";
        private long lastScanElapsedMs = 0;
        private ulong lastScanStart = 0;
        private ulong lastScanStop = 0;
        private long lastScanResultCount = 0;
        private DateTime lastScanFinishedAt = DateTime.MinValue;
        private MemoryReadStats lastScanReadStats = new MemoryReadStats();

        public bool ActiveScanLittleEndianForWrites
        {
            get { return activeScanLittleEndian; }
        }

        private bool HasEmptyVisibleSearchArgText()
        {
            if (SearchArgs == null || SearchArgs.Count == 0)
                return false;

            foreach (SearchValue searchValue in SearchArgs)
            {
                foreach (Control child in searchValue.Controls)
                {
                    TextBox textBox = child as TextBox;
                    if (textBox != null && textBox.Visible && textBox.Enabled)
                    {
                        if (textBox.Text == null || textBox.Text.Trim().Length == 0)
                            return true;
                    }
                }
            }

            return false;
        }

        private void ShowEmptySearchArgWarning()
        {
            MessageBox.Show(
                "Enter a value before starting the scan.",
                "NetCheatPS3",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private byte[] NormalizeRawMemoryForActiveScan(byte[] raw, int typeIndex)
        {
            if (raw == null)
                return raw;

            byte[] normalized = new byte[raw.Length];
            Buffer.BlockCopy(raw, 0, normalized, 0, raw.Length);

            if (activeScanLittleEndian && normalized.Length > 1)
                Array.Reverse(normalized);
            else
                normalized = misc.notrevif(normalized);

            return normalized;
        }

        private void CaptureLastScanStats(string kind, long elapsedMs, ulong start, ulong stop)
        {
            lastScanKind = kind == null ? "" : kind;
            lastScanElapsedMs = elapsedMs < 0 ? 0 : elapsedMs;
            lastScanStart = start;
            lastScanStop = stop;
            lastScanFinishedAt = DateTime.Now;
            lastScanReadStats = MemoryReader.LastCompletedStats;

            try
            {
                if (HasActiveSnapshot())
                    lastScanResultCount = activeSnapshotResultCount;
                else if (searchListView1 != null)
                    lastScanResultCount = searchListView1.TotalCount;
                else
                    lastScanResultCount = 0;
            }
            catch
            {
                lastScanResultCount = 0;
            }
        }

        private void CaptureLastScanStatsFromUi(string kind, long elapsedMs)
        {
            ulong start = 0;
            ulong stop = 0;

            try
            {
                start = Convert.ToUInt64(startAddrTB.Text, 16);
                stop = Convert.ToUInt64(stopAddrTB.Text, 16);
            }
            catch
            {
                start = 0;
                stop = 0;
            }

            CaptureLastScanStats(kind, elapsedMs, start, stop);
        }

        private void AppendLastScanSpeedDiagnostics(StringBuilder sb)
        {
            if (sb == null)
                return;

            double seconds = lastScanElapsedMs / 1000.0;
            MemoryReadStats stats = lastScanReadStats == null ? new MemoryReadStats() : lastScanReadStats;

            long bytesScanned = stats.BytesRead;
            if (bytesScanned <= 0 && lastScanStop > lastScanStart)
            {
                ulong span = lastScanStop - lastScanStart;
                bytesScanned = span > Int64.MaxValue ? Int64.MaxValue : (long)span;
            }

            double mbScanned = bytesScanned / (1024.0 * 1024.0);
            double mbPerSec = seconds > 0 ? mbScanned / seconds : 0.0;

            long blockLikeReads = stats.FullBlockSuccesses + stats.FallbackSegmentSuccesses;
            double blocksPerSec = seconds > 0 ? blockLikeReads / seconds : 0.0;

            sb.AppendLine("Last scan kind: " + (String.IsNullOrEmpty(lastScanKind) ? "n/a" : lastScanKind));
            sb.AppendLine("Last scan finished: " + (lastScanFinishedAt == DateTime.MinValue ? "n/a" : lastScanFinishedAt.ToString("yyyy-MM-dd HH:mm:ss")));
            sb.AppendLine("Last scan time: " + (lastScanElapsedMs <= 0 ? "n/a" : (seconds.ToString("0.000") + " sec")));
            sb.AppendLine("Bytes scanned/read: " + bytesScanned.ToString("N0"));
            sb.AppendLine("MB scanned/read: " + mbScanned.ToString("0.00"));
            sb.AppendLine("MB/s: " + mbPerSec.ToString("0.00"));
            sb.AppendLine("Blocks/sec: " + blocksPerSec.ToString("0.00"));
            sb.AppendLine("Results at finish: " + lastScanResultCount.ToString("N0"));
        }
    }
}