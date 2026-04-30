using System;
using System.Drawing;
using System.Windows.Forms;

namespace NetCheatPS3
{
    public partial class ProgressBar : UserControl
    {
        public static bool progTaskBarError = false;

        private Color _progressColor = DefaultForeColor;
        public Color progressColor
        {
            get { return _progressColor; }
            set
            {
                _progressColor = value;
                InvalidateProgress();
            }
        }

        private string _printText = "";
        public string printText
        {
            get { return _printText; }
            set
            {
                _printText = value;
                InvalidateProgress();
            }
        }

        private int _value = 0;
        public int Value
        {
            get { return _value; }
            set
            {
                if (Form1.Instance != null)
                {
                    if (value == 0 && _value > 0)
                    {
                        if (Form1.Instance.ContainsFocus)
                        {
                            TaskbarProgress.SetState(Form1.Instance.Handle, TaskbarProgress.TaskbarStates.NoProgress);
                        }
                        else
                        {
                            TaskbarProgress.SetState(Form1.Instance.Handle, TaskbarProgress.TaskbarStates.Paused);
                            progTaskBarError = true;
                            TaskbarProgress.SetValue(Form1.Instance.Handle, 1, 1);
                        }
                    }
                    else
                    {
                        TaskbarProgress.SetState(Form1.Instance.Handle, TaskbarProgress.TaskbarStates.Indeterminate);
                        TaskbarProgress.SetValue(Form1.Instance.Handle, value, this.Maximum);
                    }
                }

                _value = value;
                InvalidateProgress();
            }
        }

        private int _maximum = 0;
        public int Maximum
        {
            get { return _maximum; }
            set
            {
                _maximum = value;
                InvalidateProgress();
            }
        }

        public void Increment(int inc)
        {
            if ((Value + inc) <= Maximum)
                Value += inc;
        }

        public ProgressBar()
        {
            InitializeComponent();
        }

        private void ProgressBar_Paint(object sender, PaintEventArgs e)
        {
            
        }

        private void ProgressBar_Resize(object sender, EventArgs e)
        {
            pictureBox1.Size = Size;
            pictureBox1.Location = new Point(0, 0);
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (_maximum <= 0)
                return;

            float ratio = Math.Max(0.0f, Math.Min(1.0f, (float)Value / (float)Maximum));
            Rectangle clientRect = pictureBox1.ClientRectangle;
            if (clientRect.Width <= 0 || clientRect.Height <= 0)
                return;

            using (Brush fillBrush = new SolidBrush(_progressColor))
            {
                Rectangle fillRect = new Rectangle(0, 0, (int)(ratio * clientRect.Width), clientRect.Height);
                e.Graphics.FillRectangle(fillBrush, fillRect);
            }

            string text = (ratio * 100f).ToString("0.00") + "%";
            if (!String.IsNullOrEmpty(printText))
                text += " - " + printText;

            TextFormatFlags flags =
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.SingleLine;

            TextRenderer.DrawText(e.Graphics, text, Font, new Rectangle(clientRect.X - 1, clientRect.Y, clientRect.Width, clientRect.Height), ForeColor, flags);
            TextRenderer.DrawText(e.Graphics, text, Font, new Rectangle(clientRect.X + 1, clientRect.Y, clientRect.Width, clientRect.Height), ForeColor, flags);
            TextRenderer.DrawText(e.Graphics, text, Font, new Rectangle(clientRect.X, clientRect.Y - 1, clientRect.Width, clientRect.Height), ForeColor, flags);
            TextRenderer.DrawText(e.Graphics, text, Font, new Rectangle(clientRect.X, clientRect.Y + 1, clientRect.Width, clientRect.Height), ForeColor, flags);
            TextRenderer.DrawText(e.Graphics, text, Font, clientRect, BackColor, flags);
        }

        private void InvalidateProgress()
        {
            if (pictureBox1 != null)
                pictureBox1.Invalidate();

            Invalidate();
        }

    }
}
