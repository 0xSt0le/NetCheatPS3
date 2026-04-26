using System;
using System.Windows.Forms;

namespace NetCheatPS3
{
    public partial class SearchControl
    {
        public void SetStatusLabel(string str)
        {
            Invoke((MethodInvoker)delegate
            {
                //statusLabel.Text = str;
                progBar.printText = str;
            });
        }

        public void IncProgBar(int inc)
        {
            Invoke((MethodInvoker)delegate
            {
                progBar.Increment(inc);
            });
        }

        public void SetProgBar(int val)
        {
            Invoke((MethodInvoker)delegate
            {
                progBar.Value = val;
                TaskbarProgress.SetValue(this.Handle, (double)val, (double)progBar.Maximum);
            });
        }

        public void SetProgBarText(string val)
        {
            Invoke((MethodInvoker)delegate
            {
                progBar.printText = val;
                //TaskbarProgress.SetValue(this.Handle, (double)val, (double)progBar.Maximum);
            });
        }

        public void SetProgBarMax(int max)
        {
            Invoke((MethodInvoker)delegate
            {
                progBar.Maximum = max;
            });
        }
    }
}
