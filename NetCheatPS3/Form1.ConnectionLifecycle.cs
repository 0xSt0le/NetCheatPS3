using System;
using System.Drawing;
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
            statusLabel1.Text = statusMessage;
            UpdateConnectionUiState();
        }

        private void MarkDisconnected(string statusMessage)
        {
            attached = false;
            connected = false;
            ConstantLoop = 0;
            ClearAllCodeBackups(statusMessage);
            statusLabel1.Text = statusMessage;
            UpdateConnectionUiState();
        }

        private void UpdateConnectionUiState()
        {
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

            try
            {
                curAPI.Instance.isProcessStopped();
            }
            catch
            {
                MarkDetached("Process detached/lost. Cleared code backups.");
            }
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
