using System.Windows.Forms;

using System;

namespace NetCheatPS3
{
    public partial class SearchControl
    {
        private bool EnsureConnectedAndAttachedForScanAction(string actionName)
        {
            string message = Form1.GetConnectionAttachErrorMessage(actionName);

            if (message == null)
                return true;

            Form1.SetMainStatusSafe(message);

            MessageBox.Show(
                message,
                "NetCheatPS3",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return false;
        }

        private bool AbortScanThreadIfDisconnectedOrUnattached(string actionName)
        {
            string message = Form1.GetConnectionAttachErrorMessage(actionName);

            if (message == null)
                return false;

            Form1.SetMainStatusSafe(message);

            try
            {
                Invoke((MethodInvoker)delegate
                {
                    if (actionName == "Initial Scan")
                    {
                        searchMemory.Text = "Initial Scan";
                        isInitialScan = true;
                    }

                    if (actionName == "Next Scan")
                        nextSearchMem.Text = "Next Scan";

                    if (progBar != null)
                        progBar.printText = message;
                });
            }
            catch
            {
            }

            _shouldStopSearch = false;
            searchThread = null;
            return true;
        }

        private bool ValidateScanMemoryBeforeStart(string actionName, ulong address, int byteSize)
        {
            if (Form1.Instance == null)
                return false;

            int readLength = Math.Max(4, byteSize);
            string probeError;
            if (Form1.Instance.TryReadProbeBytes(address, readLength, out probeError))
                return true;

            string livenessError;
            if (Form1.Instance.TryValidateTargetStillAlive(address, out livenessError))
            {
                string message = "Scan range is not readable. Check start/stop range or process attach.";
                Form1.SetMainStatusSafe(message);
                if (progBar != null)
                    progBar.printText = message;
                return false;
            }

            Form1.Instance.TryValidateReadableMemoryForAction(actionName, address, readLength, true, true);
            return false;
        }

        private void SetSearchActionRunningStatus(string actionName)
        {
            string message = actionName + " running...";
            Form1.SetMainStatusSafe(message);

            try
            {
                if (progBar != null)
                    progBar.printText = message;
            }
            catch
            {
            }
        }
    }
}
