using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace NetCheatPS3
{
    public partial class Form1
    {
        private void connectButton_Click(object sender, EventArgs e)
        {
            if (curAPI == null)
                return;

            if (connected)
            {
                DisconnectFromTarget("Disconnected. Cleared code backups.");
                return;
            }

            statusLabel1.Text = "Connecting...";
            try
            {
                if (curAPI.Instance.Connect())
                {
                    connected = true;
                    attached = false;
                    ConstantLoop = 0;
                    statusLabel1.Text = "Connected";
                    UpdateConnectionUiState();
                }
                else
                {
                    statusLabel1.Text = "Failed to connect";
                    connected = false;
                    attached = false;
                    UpdateConnectionUiState();
                }
            }
            catch
            {
                statusLabel1.Text = "Failed to connect";
                connected = false;
                attached = false;
                UpdateConnectionUiState();
            }
        }

        private void attachProcessButton_Click(object sender, EventArgs e)
        {
            if (!connected)
            {
                UpdateConnectionUiState();
                return;
            }

            if (attached)
            {
                MarkDetached("Detached from process. Cleared code backups.");
                return;
            }

            try
            {
                if (curAPI.Instance.Attach())
                {
                    ConstantLoop = 1;
                    attached = true;
                    ClearAllCodeBackups("Process attached.");
                    statusLabel1.Text = "Process Attached";
                    UpdateConnectionUiState();
                }
                else
                {
                    statusLabel1.Text = "Error attaching process";
                    attached = false;
                    UpdateConnectionUiState();
                }
            }
            catch (Exception)
            {
                statusLabel1.Text = "Error attaching process";
                attached = false;
                UpdateConnectionUiState();
            }
        }

        private void DisconnectFromTarget(string statusMessage)
        {
            try
            {
                if (curAPI != null && curAPI.Instance != null)
                    curAPI.Instance.Disconnect();
            }
            catch (Exception)
            {
            }

            MarkDisconnected(statusMessage);
        }

        private void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            connectButton_Click(null, null);
        }

        private void attachToolStripMenuItem_Click(object sender, EventArgs e)
        {
            attachProcessButton_Click(null, null);
        }

        private void disconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DisconnectFromTarget("Disconnected. Cleared code backups.");
        }

        private void MarkDetached(string statusMessage)
        {
            attached = false;
            ConstantLoop = 0;
            ClearAllCodeBackups(statusMessage);
            SetConnectionStatusAndUpdateUi(statusMessage);
        }

        private void MarkDisconnected(string statusMessage)
        {
            attached = false;
            connected = false;
            ConstantLoop = 0;
            ClearAllCodeBackups(statusMessage);
            SetConnectionStatusAndUpdateUi(statusMessage);
        }

        public static void MarkAttachedMemoryUnavailable()
        {
            if (Instance != null)
                Instance.MarkDetached("Process detached/lost. Cleared code backups.");
        }

        public static void ValidateAttachedMemoryStateAfterAccessFailure()
        {
            if (Instance != null && connected && attached)
                Instance.TryValidateAttachedMemoryAccess("memory access", true);
        }

        private void SetConnectionStatusAndUpdateUi(string statusMessage)
        {
            if (IsHandleCreated && InvokeRequired)
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    SetConnectionStatusAndUpdateUi(statusMessage);
                });
                return;
            }

            if (statusLabel1 != null)
                statusLabel1.Text = statusMessage;

            UpdateConnectionUiState();
        }

        private void UpdateConnectionUiState()
        {
            if (IsHandleCreated && InvokeRequired)
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    UpdateConnectionUiState();
                });
                return;
            }

            if (connectButton != null)
            {
                connectButton.Enabled = curAPI != null;
                connectButton.Text = connected ? "Disconnect" : "Connect";
            }

            if (attachProcessButton != null)
            {
                attachProcessButton.Enabled = connected;
                attachProcessButton.Text = attached ? "Detach" : "Attach";
            }

            if (connectToolStripMenuItem != null)
                connectToolStripMenuItem.Text = connected ? "Disconnect" : "Connect";

            if (attachToolStripMenuItem != null)
            {
                attachToolStripMenuItem.Enabled = connected;
                attachToolStripMenuItem.Text = attached ? "Detach" : "Attach";
            }

            if (disconnectToolStripMenuItem != null)
                disconnectToolStripMenuItem.Enabled = connected;

            if (toolStripDropDownButton1 != null)
            {
                if (attached)
                    toolStripDropDownButton1.BackColor = Color.DarkGreen;
                else if (connected)
                    toolStripDropDownButton1.BackColor = Color.DarkGoldenrod;
                else
                    toolStripDropDownButton1.BackColor = Color.Maroon;
            }
        }

        private void SynchronizeConnectionStateBeforeAction()
        {
            if (!connected)
            {
                if (attached)
                    MarkDisconnected("Disconnected. Cleared code backups.");

                UpdateConnectionUiState();
                return;
            }

            if (curAPI == null || curAPI.Instance == null)
            {
                MarkDisconnected("Disconnected. Cleared code backups.");
                return;
            }

            if (!attached)
            {
                UpdateConnectionUiState();
                return;
            }

            TryValidateAttachedMemoryAccess("connection state check", true);
        }

        public bool TryValidateAttachedMemoryAccess(string actionName, bool silent)
        {
            if (!connected || !attached)
                return false;

            if (curAPI == null || curAPI.Instance == null)
            {
                MarkDisconnected("Disconnected. Cleared code backups.");
                return false;
            }

            ulong probeAddress;
            if (!TryGetAttachedMemoryProbeAddress(out probeAddress))
                probeAddress = 0x00010000;

            try
            {
                byte[] probe = new byte[1];
                if (curAPI.Instance.GetBytes(probeAddress, ref probe))
                    return true;
            }
            catch
            {
            }

            MarkDetached("Process detached/lost. Cleared code backups.");
            if (!silent && !InvokeRequired)
            {
                string message = "The process or target appears unavailable. NetCheatPS3 detached locally and cleared code backups.";
                MessageBox.Show(message, "NetCheatPS3", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            return false;
        }

        private bool TryGetAttachedMemoryProbeAddress(out ulong address)
        {
            address = 0;

            if (TryGetSelectedCodeWritableAddress(out address))
                return true;

            if (InvokeRequired)
            {
                address = 0x00010000;
                return true;
            }

            if (TryGetSearchResultAddress(out address))
                return true;

            if (TryGetFirstScanRangeStart(out address))
                return true;

            address = 0x00010000;
            return true;
        }

        private bool TryGetSelectedCodeWritableAddress(out ulong address)
        {
            address = 0;

            if (cbListIndex < 0 || cbListIndex >= Codes.Count || Codes[cbListIndex].CData == null)
                return false;

            foreach (ncCode code in Codes[cbListIndex].CData)
            {
                if ((code.codeType == '0' || code.codeType == '1' || code.codeType == '2') && code.codeArg1 != 0)
                {
                    address = code.codeArg1;
                    return true;
                }
            }

            return false;
        }

        private bool TryGetSearchResultAddress(out ulong address)
        {
            address = 0;

            try
            {
                if (searchControl1 == null || searchControl1.searchListView1 == null || searchControl1.searchListView1.TotalCount <= 0)
                    return false;

                int index = 0;
                if (searchControl1.searchListView1.SelectedIndices != null && searchControl1.searchListView1.SelectedIndices.Count > 0)
                    index = searchControl1.searchListView1.SelectedIndices[0];

                if (index < 0 || index >= searchControl1.searchListView1.TotalCount)
                    index = 0;

                SearchListView.SearchListViewItem item = searchControl1.searchListView1.GetItemAtIndex(index);
                if (item.addr == 0)
                    return false;

                address = item.addr;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool TryGetFirstScanRangeStart(out ulong address)
        {
            address = 0;

            try
            {
                if (rangeView == null || rangeView.Items.Count <= 0)
                    return false;

                for (int index = 0; index < rangeView.Items.Count; index++)
                {
                    if (rangeView.Items[index].SubItems.Count <= 0)
                        continue;

                    string text = rangeView.Items[index].SubItems[0].Text;
                    ulong parsed;
                    if (ulong.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out parsed) && parsed != 0)
                    {
                        address = parsed;
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private void ClearAllCodeBackups(string reason)
        {
            for (int index = 0; index < Codes.Count; index++)
            {
                CodeDB code = Codes[index];
                if (code.backUp != null)
                {
                    code.backUp = null;
                    Codes[index] = code;
                }
            }
        }

        public static void PauseProcess()
        {
            curAPI.Instance.PauseProcess();
        }

        public static void ContinueProcess()
        {
            curAPI.Instance.ContinueProcess();
        }

        public bool isProcessStopped()
        {
            if (curAPI == null)
                return true;
            return curAPI.Instance.isProcessStopped();
        }
    }
}
