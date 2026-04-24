using System;
using System.Drawing;
using System.Windows.Forms;

namespace NetCheatPS3
{
    public partial class Form1
    {
        private ContextMenuStrip modernRangeContextMenu;
        private ToolStripMenuItem setScanToThisRangeMenuItem;
        private bool modernRangeUiInitialized = false;

        private void InitializeModernRangeUi()
        {
            if (rangeView == null)
                return;

            if (!modernRangeUiInitialized)
            {
                modernRangeContextMenu = new ContextMenuStrip();
                setScanToThisRangeMenuItem = new ToolStripMenuItem("Set Scan To This Range");
                setScanToThisRangeMenuItem.Click += setScanToThisRangeMenuItem_Click;
                modernRangeContextMenu.Items.Add(setScanToThisRangeMenuItem);

                rangeView.ContextMenuStrip = modernRangeContextMenu;
                rangeView.MouseDown += rangeView_ModernMouseDown;

                if (RangeTab != null)
                    RangeTab.Enter += delegate { BeginInvoke((MethodInvoker)delegate { StyleProbeAddressButtonLikeFindRanges(); }); };

                this.Shown += delegate { BeginInvoke((MethodInvoker)delegate { StyleProbeAddressButtonLikeFindRanges(); }); };

                modernRangeUiInitialized = true;
            }

            StyleProbeAddressButtonLikeFindRanges();
        }

        private Button FindProbeAddressButton()
        {
            if (RangeTab == null)
                return null;

            foreach (Control control in RangeTab.Controls)
            {
                Button button = control as Button;
                if (button == null)
                    continue;

                string text = button.Text == null ? "" : button.Text.Trim();
                if (String.Equals(text, "Probe Address", StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(button.Name, "probeAddress", StringComparison.OrdinalIgnoreCase) ||
                    text.IndexOf("Probe", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return button;
                }
            }

            return null;
        }

        private void StyleProbeAddressButtonLikeFindRanges()
        {
            if (findRanges == null)
                return;

            Button probeButton = FindProbeAddressButton();
            if (probeButton == null || Object.ReferenceEquals(probeButton, findRanges))
                return;

            probeButton.Text = "Probe Address";
            probeButton.FlatStyle = findRanges.FlatStyle;
            probeButton.Font = findRanges.Font;
            probeButton.Size = findRanges.Size;
            probeButton.BackColor = findRanges.BackColor;
            probeButton.ForeColor = findRanges.ForeColor;
            probeButton.UseVisualStyleBackColor = findRanges.UseVisualStyleBackColor;
            probeButton.Image = findRanges.Image;
            probeButton.ImageAlign = findRanges.ImageAlign;
            probeButton.TextAlign = findRanges.TextAlign;
            probeButton.TextImageRelation = findRanges.TextImageRelation;
            probeButton.Padding = findRanges.Padding;
            probeButton.Margin = findRanges.Margin;
            probeButton.AutoSize = findRanges.AutoSize;
            probeButton.AutoSizeMode = findRanges.AutoSizeMode;
            probeButton.Anchor = findRanges.Anchor;
            probeButton.TabStop = findRanges.TabStop;

            probeButton.FlatAppearance.BorderSize = findRanges.FlatAppearance.BorderSize;
            probeButton.FlatAppearance.BorderColor = findRanges.FlatAppearance.BorderColor;
            probeButton.FlatAppearance.MouseDownBackColor = findRanges.FlatAppearance.MouseDownBackColor;
            probeButton.FlatAppearance.MouseOverBackColor = findRanges.FlatAppearance.MouseOverBackColor;
        }

        private void rangeView_ModernMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            ListViewHitTestInfo hit = rangeView.HitTest(e.Location);
            if (hit != null && hit.Item != null)
            {
                rangeView.SelectedItems.Clear();
                hit.Item.Selected = true;
                hit.Item.Focused = true;
            }
        }

        private void setScanToThisRangeMenuItem_Click(object sender, EventArgs e)
        {
            if (rangeView.SelectedItems.Count <= 0)
                return;

            ListViewItem item = rangeView.SelectedItems[0];
            if (item.SubItems.Count < 2)
                return;

            try
            {
                ulong start;
                ulong stop;

                if (!TryParseRangeItem(item, out start, out stop))
                    return;

                SetSearchRangeAndShowSearchTab(start, stop);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to set scan range: " + ex.Message, "NetCheatPS3", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool TryParseRangeItem(ListViewItem item, out ulong start, out ulong stop)
        {
            start = 0;
            stop = 0;

            if (item == null || item.SubItems.Count < 2)
                return false;

            string startText = item.SubItems[0].Text.Trim();
            string stopText = item.SubItems[1].Text.Trim();

            if (startText.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                startText = startText.Substring(2);

            if (stopText.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                stopText = stopText.Substring(2);

            start = Convert.ToUInt64(startText, 16);
            stop = Convert.ToUInt64(stopText, 16);

            return stop > start;
        }

        private bool IsDefaultOrBroadPlaceholderRange(ulong start, ulong stop)
        {
            if (stop <= start)
                return true;

            if (start == 0 && stop >= 0xFFFFFF00UL)
                return true;

            return false;
        }

        public bool TryGetContainingMemoryRange(ulong address, out ulong start, out ulong stop)
        {
            start = 0;
            stop = 0;

            if (rangeView == null || rangeView.Items.Count <= 0)
                return false;

            bool found = false;
            ulong bestSize = UInt64.MaxValue;

            for (int i = 0; i < rangeView.Items.Count; i++)
            {
                ulong curStart;
                ulong curStop;

                if (!TryParseRangeItem(rangeView.Items[i], out curStart, out curStop))
                    continue;

                if (IsDefaultOrBroadPlaceholderRange(curStart, curStop))
                    continue;

                if (address >= curStart && address < curStop)
                {
                    ulong size = curStop - curStart;

                    if (!found || size < bestSize)
                    {
                        start = curStart;
                        stop = curStop;
                        bestSize = size;
                        found = true;
                    }
                }
            }

            return found;
        }

        public void SetSearchRangeAndShowSearchTab(ulong start, ulong stop)
        {
            searchControl1.SetScanRange(start, stop);
            TabCon.SelectedTab = SearchTab;
            statusLabel1.Text = "Scan range set to 0x" + start.ToString("X8") + "-0x" + stop.ToString("X8");
        }
    }
}