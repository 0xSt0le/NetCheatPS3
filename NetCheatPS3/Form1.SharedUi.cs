using System.Windows.Forms;

namespace NetCheatPS3
{
    public partial class Form1
    {
        public IBArg[] CallIBox(IBArg[] a)
        {
            InputBox ib = new InputBox();

            ib.Arg = a;
            ib.fmHeight = this.Height;
            ib.fmWidth = this.Width;
            ib.fmLeft = this.Left;
            ib.fmTop = this.Top;
            ib.TopMost = true;
            ib.fmForeColor = ForeColor;
            ib.fmBackColor = BackColor;
            ib.Show();

            while (ib.ret == 0)
            {
                a = ib.Arg;
                Application.DoEvents();
            }
            a = ib.Arg;

            if (ib.ret == 1)
                return a;
            else if (ib.ret == 2)
                return null;

            return null;
        }

        public static void HandlePluginControls(Control.ControlCollection plgCtrl)
        {
            foreach (Control ctrl in plgCtrl)
            {
                if (ctrl.Controls != null && ctrl.Controls.Count > 0)
                {
                    HandlePluginControls(ctrl.Controls);
                }
                if (ctrl is ListView)
                {
                    foreach (ListViewItem ctrlLVI in (ctrl as ListView).Items)
                    {
                        ctrlLVI.BackColor = ncBackColor;
                        ctrlLVI.ForeColor = ncForeColor;
                    }
                }

                ctrl.BackColor = ncBackColor;
                ctrl.ForeColor = ncForeColor;
            }
        }
    }
}
