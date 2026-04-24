using System;
using System.Windows.Forms;

namespace NetCheatPS3
{
    public partial class SearchControl
    {
        public bool ActiveScanLittleEndianForWrites
        {
            get { return activeScanLittleEndian; }
        }

        private bool HasEmptyVisibleSearchArgText()
        {
            if (SearchArgs == null || SearchArgs.Count == 0)
                return false;

            foreach (SearchValue searchValue in SearchArgs)
            {
                foreach (Control child in searchValue.Controls)
                {
                    TextBox textBox = child as TextBox;
                    if (textBox != null && textBox.Visible && textBox.Enabled)
                    {
                        if (textBox.Text == null || textBox.Text.Trim().Length == 0)
                            return true;
                    }
                }
            }

            return false;
        }

        private void ShowEmptySearchArgWarning()
        {
            MessageBox.Show(
                "Enter a value before starting the scan.",
                "NetCheatPS3",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private byte[] NormalizeRawMemoryForActiveScan(byte[] raw, int typeIndex)
        {
            if (raw == null)
                return raw;

            byte[] normalized = new byte[raw.Length];
            Buffer.BlockCopy(raw, 0, normalized, 0, raw.Length);

            if (activeScanLittleEndian && normalized.Length > 1)
                Array.Reverse(normalized);
            else
                normalized = misc.notrevif(normalized);

            return normalized;
        }
    }
}