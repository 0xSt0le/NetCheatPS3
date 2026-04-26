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

            this.statusLabel1.Text = "Connecting...";
            try
            {
                if (curAPI.Instance.Connect())
                {
                    connected = true;
                    this.statusLabel1.Text = "Connected";


                    connectButton.Enabled = false;
                    attachProcessButton.Enabled = true;
                    toolStripDropDownButton1.BackColor = Color.DarkGoldenrod;
                }
                else
                {
                    this.statusLabel1.Text = "Failed to connect";
                    connected = false;
                }
            }
            catch
            {
                this.statusLabel1.Text = "Failed to connect";
                connected = false;
            }
        }

        private void attachProcessButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (curAPI.Instance.Attach())
                {
                    this.statusLabel1.Text = "Process Attached";

                    ConstantLoop = 1;
                    attachProcessButton.Enabled = false;
                    toolStripDropDownButton1.BackColor = Color.DarkGreen;
                    attached = true;
                }
                else
                {
                    this.statusLabel1.Text = "Error attaching process";
                    toolStripDropDownButton1.BackColor = Color.DarkGoldenrod;
                }
                
            }
            catch (Exception)
            {
                this.statusLabel1.Text = "Error attaching process";
                toolStripDropDownButton1.BackColor = Color.DarkGoldenrod;
            }
        }

        private void ps3Disc_Click(object sender, EventArgs e)
        {
            if (curAPI == null)
                return;

            try
            {
                curAPI.Instance.Disconnect();
                ConstantLoop = 2;
                this.statusLabel1.Text = "Disconnected";
                attached = false;
                connected = false;

                attachProcessButton.Enabled = false;
                connectButton.Enabled = true;
                toolStripDropDownButton1.BackColor = Color.Maroon;
            }
            catch (Exception)
            {

            }
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
            ps3Disc_Click(null, null);
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