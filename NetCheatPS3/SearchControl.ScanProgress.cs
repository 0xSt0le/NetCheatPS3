using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace NetCheatPS3
{
    public partial class SearchControl
    {
        private const int ScanProgressMaximum = 10000;
        private const int ScanProgressMinimumDelta = 50;
        private const int ScanProgressMinimumUpdateMs = 100;

        private readonly object scanProgressSync = new object();
        private Stopwatch scanProgressStopwatch;
        private string scanProgressActionName = "";
        private string scanProgressUnitName = "";
        private long scanProgressTotalUnits = 1;
        private long scanProgressLastCompletedUnits = -1;
        private int scanProgressLastValue = -1;
        private long scanProgressLastResultCount = -1;
        private long scanProgressLastUiUpdateMs = -1;

        // Scan code reports completed/total work units here; it should not mix
        // arbitrary progress bar maximums or use failed block reads as progress semantics.
        public void BeginScanProgress(string actionName, long totalUnits, string unitName)
        {
            if (totalUnits <= 0)
                totalUnits = 1;

            lock (scanProgressSync)
            {
                scanProgressActionName = actionName ?? "Scan";
                scanProgressUnitName = unitName ?? "units";
                scanProgressTotalUnits = totalUnits;
                scanProgressLastCompletedUnits = -1;
                scanProgressLastValue = -1;
                scanProgressLastResultCount = -1;
                scanProgressLastUiUpdateMs = -1;
                scanProgressStopwatch = Stopwatch.StartNew();
            }

            UpdateScanProgressCore(0, 0, true);
        }

        public void UpdateScanProgress(long completedUnits, long resultCount)
        {
            UpdateScanProgressCore(completedUnits, resultCount, false);
        }

        public void UpdateScanProgressText(string text)
        {
            SetScanProgressUi(-1, text);
        }

        public void CompleteScanProgress(string finalText)
        {
            long totalUnits;
            lock (scanProgressSync)
            {
                totalUnits = scanProgressTotalUnits;
            }

            UpdateScanProgressCore(totalUnits, scanProgressLastResultCount < 0 ? 0 : scanProgressLastResultCount, true);
            SetScanProgressUi(ScanProgressMaximum, finalText);
        }

        public void ResetScanProgress(string text)
        {
            lock (scanProgressSync)
            {
                scanProgressActionName = "";
                scanProgressUnitName = "";
                scanProgressTotalUnits = 1;
                scanProgressLastCompletedUnits = -1;
                scanProgressLastValue = -1;
                scanProgressLastResultCount = -1;
                scanProgressLastUiUpdateMs = -1;
                scanProgressStopwatch = null;
            }

            SetScanProgressUi(0, text);
        }

        public void FailScanProgress(string text)
        {
            SetScanProgressUi(0, text);
        }

        public void SetStatusLabel(string str)
        {
            UpdateScanProgressText(str);
        }

        public void IncProgBar(int inc)
        {
            RunOnProgressUiThread(delegate
            {
                if (progBar != null)
                    progBar.Increment(inc);
            });
        }

        public void SetProgBar(int val)
        {
            RunOnProgressUiThread(delegate
            {
                if (progBar == null)
                    return;

                progBar.Value = ClampProgressValue(val, 0, progBar.Maximum);
                TaskbarProgress.SetValue(this.Handle, (double)progBar.Value, (double)progBar.Maximum);
            });
        }

        public void SetProgBarText(string val)
        {
            UpdateScanProgressText(val);
        }

        public void SetProgBarMax(int max)
        {
            RunOnProgressUiThread(delegate
            {
                if (progBar != null)
                    progBar.Maximum = max;
            });
        }

        private void UpdateScanProgressCore(long completedUnits, long resultCount, bool force)
        {
            string actionName;
            string unitName;
            long totalUnits;
            long elapsedMs;
            int value;
            bool shouldUpdate;

            lock (scanProgressSync)
            {
                if (completedUnits < 0)
                    completedUnits = 0;

                totalUnits = scanProgressTotalUnits <= 0 ? 1 : scanProgressTotalUnits;
                if (completedUnits > totalUnits)
                    completedUnits = totalUnits;

                value = (int)Math.Round((double)completedUnits * ScanProgressMaximum / (double)totalUnits);
                value = ClampProgressValue(value, 0, ScanProgressMaximum);

                elapsedMs = scanProgressStopwatch != null ? scanProgressStopwatch.ElapsedMilliseconds : 0;
                shouldUpdate = force ||
                    scanProgressLastValue < 0 ||
                    Math.Abs(value - scanProgressLastValue) >= ScanProgressMinimumDelta ||
                    elapsedMs - scanProgressLastUiUpdateMs >= ScanProgressMinimumUpdateMs;

                if (!shouldUpdate)
                    return;

                scanProgressLastCompletedUnits = completedUnits;
                scanProgressLastValue = value;
                scanProgressLastResultCount = resultCount;
                scanProgressLastUiUpdateMs = elapsedMs;
                actionName = scanProgressActionName;
                unitName = scanProgressUnitName;
            }

            SetScanProgressUi(value, FormatScanProgressText(actionName, value, completedUnits, totalUnits, unitName, resultCount));
        }

        private string FormatScanProgressText(string actionName, int value, long completedUnits, long totalUnits, string unitName, long resultCount)
        {
            double percent = (double)value * 100.0 / (double)ScanProgressMaximum;
            string text = actionName + ": " + percent.ToString("0.0") + "%";

            if (resultCount >= 0)
                text += " | Results: " + resultCount.ToString("N0");

            if (!String.IsNullOrEmpty(unitName))
                text += " | " + completedUnits.ToString("N0") + "/" + totalUnits.ToString("N0") + " " + unitName;

            return text;
        }

        private void SetScanProgressUi(int value, string text)
        {
            RunOnProgressUiThread(delegate
            {
                if (progBar == null)
                    return;

                if (progBar.Maximum != ScanProgressMaximum)
                    progBar.Maximum = ScanProgressMaximum;

                if (value >= 0)
                    progBar.Value = ClampProgressValue(value, 0, ScanProgressMaximum);

                progBar.printText = text ?? "";
            });
        }

        private void RunOnProgressUiThread(MethodInvoker action)
        {
            try
            {
                if (IsDisposed || !IsHandleCreated)
                    return;

                if (InvokeRequired)
                    BeginInvoke(action);
                else
                    action();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        private int ClampProgressValue(int value, int min, int max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }
}
