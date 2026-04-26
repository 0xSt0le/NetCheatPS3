using System;
using System.Drawing;
using System.Speech.Recognition;
using System.Windows.Forms;

namespace NetCheatPS3
{
    public partial class Form1
    {
        private void Form1_Focused(object sender, EventArgs e)
        {
            if (Form1.Instance.ContainsFocus)
            {
                if (ProgressBar.progTaskBarError || (searchControl1.searchMemory.Text != "Stop" && searchControl1.nextSearchMem.Text != "Stop"))
                {
                    TaskbarProgress.SetState(this.Handle, TaskbarProgress.TaskbarStates.NoProgress);
                    ProgressBar.progTaskBarError = false;
                }
            }
        }

        /* Everything else because I have no organization skills... */
        private void Form1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            isClosing = true;
            ConstantLoop = 2;
            codes.ExitConstWriter = true;
            this.statusLabel1.Text = "Disconnected";

            //Close all plugins
            foreach (PluginForm a in pluginForm)
            {
                try
                {
                    a.Dispose();
                    a.Close();
                }
                catch { }
            }

            if (refPlugin.Text == "Close Plugins")
                Global.Plugins.ClosePlugins();
        }

        public void HandleFocusControls(Control.ControlCollection focCtrl)
        {
            foreach (Control ctrl in focCtrl)
            {
                if (ctrl.Controls != null && ctrl.Controls.Count > 0)
                    HandleFocusControls(ctrl.Controls);

                ctrl.GotFocus += new EventHandler(Form1_Focused);
            }
        }

        private void optButton_Click(object sender, EventArgs e)
        {
            OptionForm oForm = new OptionForm();
            oForm.Show();
        }

        private void TabCon_KeyUp(object sender, KeyEventArgs e)
        {
            if (ProcessKeyBinds(e.KeyData))
                e.SuppressKeyPress = true;
        }

        private bool ProcessKeyBinds(Keys data)
        {
            int match = -1;

            for (int x = 0; x < keyBinds.Length; x++)
                if (keyBinds[x].Equals(data))
                    match = x;
            
            if (match < 0)
                return false;

            switch (match)
            {
                case 0: //Connect And Attach
                    //Connect
                    connectButton_Click(null, null);
                    //Attach if connected
                    if (connected)
                        attachProcessButton_Click(null, null);
                    break;
                case 1: //Disconnect
                    ps3Disc_Click(null, null);
                    break;
                case 6: //Toggle Constant Write
                    if (cbListIndex >= 0 && cbListIndex < cbList.Items.Count)
                    {
                        cbList_DoubleClick(null, null);
                    }
                    break;
                case 7: //Write
                    if (cbListIndex >= 0 && cbListIndex < cbList.Items.Count)
                    {
                        cbWrite_Click(null, null);
                    }
                    break;
            }
            return true;
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Oemtilde)
            {
                try
                {
                    sRecognize.RecognizeAsyncStop();
                    isRecognizing = false;
                }
                catch { }
            }
            if (ProcessKeyBinds(e.KeyData))
                e.SuppressKeyPress = true;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Oemtilde && !isRecognizing)
            {
                try
                {
                    sRecognize.RecognizeAsync(RecognizeMode.Multiple);
                    //sRecognize.Recognize();
                    isRecognizing = true;
                }
                catch { }
            }
        }

        private void shutdownPS3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            curAPI.Instance.Shutdown();
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            optButton_Click(null, null);
        }

        string ParseValFromStr(string val)
        {
            val = val.ToLower();
            val = val.Replace(" ", "");
            val = val.Replace("zero", "0");
            val = val.Replace("one", "1");
            val = val.Replace("two", "2");
            val = val.Replace("three", "3");
            val = val.Replace("four", "4");
            val = val.Replace("five", "5");
            val = val.Replace("six", "6");
            val = val.Replace("seven", "7");
            val = val.Replace("eight", "8");
            val = val.Replace("nine", "9");
            val = val.Replace("see", "C");
            val = val.Replace("be", "B");
            return val;
        }

        private void startGameButt_Click(object sender, EventArgs e)
        {
            if (!curAPI.Instance.ContinueProcess())
                MessageBox.Show("Feature not supported with this API!");
        }

        private void pauseGameButt_Click(object sender, EventArgs e)
        {
            if (!curAPI.Instance.PauseProcess())
                MessageBox.Show("Feature not supported with this API!");
        }
        private void pauseGameButt_BackColorChanged(object sender, EventArgs e)
        {
            if (pauseGameButt.BackColor != Color.White)
                pauseGameButt.BackColor = Color.White;
        }

        private void pauseGameButt_ForeColorChanged(object sender, EventArgs e)
        {
            if (pauseGameButt.ForeColor != Color.FromArgb(0, 130, 210))
                pauseGameButt.ForeColor = Color.FromArgb(0, 130, 210);
        }

        private void startGameButt_BackColorChanged(object sender, EventArgs e)
        {
            if (startGameButt.BackColor != Color.White)
                startGameButt.BackColor = Color.White;
        }

        private void startGameButt_ForeColorChanged(object sender, EventArgs e)
        {
            if (startGameButt.ForeColor != Color.FromArgb(0, 130, 210))
                startGameButt.ForeColor = Color.FromArgb(0, 130, 210);
        }

        private void endianStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentEndian == Endian.Big)
                CurrentEndian = Endian.Little;
            else
                CurrentEndian = Endian.Big;
        }
    }
}
