using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NetCheatPS3
{
    public partial class SearchListView
    {
        private bool modernResultUiInitialized = false;
        private ToolStripMenuItem copyAddressToolStripMenuItem;
        private ToolStripMenuItem copyNetCheatCodeToolStripMenuItem;
        private ToolStripMenuItem setScanToThisRegionToolStripMenuItem;
        private ToolStripMenuItem editWriteValueToolStripMenuItem;

        private void InitializeModernResultUi()
        {
            if (modernResultUiInitialized)
                return;

            modernResultUiInitialized = true;

            cms = contextMenuStrip1;

            EnsureModernResultContextMenu();

            // The original built-in double-click handler creates a readonly textbox too.
            // Remove it so only the modern editor owns textbox placement/write behavior.
            printBox.DoubleClick -= new EventHandler(printBox_DoubleClick);
            printBox.DoubleClick += printBox_ModernDoubleClick;

            printBox.MouseDown += printBox_ModernMouseDown;
        }

        private void EnsureModernResultContextMenu()
        {
            if (contextMenuStrip1 == null)
                return;

            if (copyToolStripMenuItem != null && contextMenuStrip1.Items.Contains(copyToolStripMenuItem))
                contextMenuStrip1.Items.Remove(copyToolStripMenuItem);

            if (contextMenuStrip1.Items.Find("copyAddressToolStripMenuItem", false).Length > 0)
            {
                ToolStripItem[] oldRangeItems = contextMenuStrip1.Items.Find("setRangeAroundAddressToolStripMenuItem", false);
                foreach (ToolStripItem item in oldRangeItems)
                    item.Text = "Set Scan Range To This Region";

                ToolStripItem[] newRangeItems = contextMenuStrip1.Items.Find("setScanToThisRegionToolStripMenuItem", false);
                foreach (ToolStripItem item in newRangeItems)
                    item.Text = "Set Scan Range To This Region";

                return;
            }

            copyAddressToolStripMenuItem = new ToolStripMenuItem("Copy Address");
            copyAddressToolStripMenuItem.Name = "copyAddressToolStripMenuItem";
            copyAddressToolStripMenuItem.Click += copyAddressToolStripMenuItem_Click;

            copyNetCheatCodeToolStripMenuItem = new ToolStripMenuItem("Copy NetCheat Code");
            copyNetCheatCodeToolStripMenuItem.Name = "copyNetCheatCodeToolStripMenuItem";
            copyNetCheatCodeToolStripMenuItem.Click += copyNetCheatCodeToolStripMenuItem_Click;

            setScanToThisRegionToolStripMenuItem = new ToolStripMenuItem("Set Scan Range To This Region");
            setScanToThisRegionToolStripMenuItem.Name = "setScanToThisRegionToolStripMenuItem";
            setScanToThisRegionToolStripMenuItem.Click += setScanToThisRegionToolStripMenuItem_Click;

            editWriteValueToolStripMenuItem = new ToolStripMenuItem("Edit / Write Value");
            editWriteValueToolStripMenuItem.Name = "editWriteValueToolStripMenuItem";
            editWriteValueToolStripMenuItem.Click += editWriteValueToolStripMenuItem_Click;

            contextMenuStrip1.Items.Insert(0, copyAddressToolStripMenuItem);
            contextMenuStrip1.Items.Insert(1, copyNetCheatCodeToolStripMenuItem);
            contextMenuStrip1.Items.Insert(2, setScanToThisRegionToolStripMenuItem);
            contextMenuStrip1.Items.Insert(3, editWriteValueToolStripMenuItem);
            contextMenuStrip1.Items.Insert(4, new ToolStripSeparator());
        }

        private int GetVisibleStartIndex()
        {
            return vertSBar.Visible ? vertSBar.Value : 0;
        }

        private int GetIndexAtPoint(Point point)
        {
            int index = (int)(point.Y / ItemHeight) + GetVisibleStartIndex();
            if (index < 0 || index >= TotalCount)
                return -1;

            return index;
        }

        private int GetColumnAtPoint(Point point)
        {
            if (addrLabel.Width <= 0)
                return -1;

            int column = (int)(point.X / addrLabel.Width);
            if (column < 0 || column > 3)
                return -1;

            return column;
        }

        private bool TryGetPrimarySelectedItem(out int index, out SearchListViewItem item)
        {
            index = -1;
            item = new SearchListViewItem();

            if (SelectedIndices == null || SelectedIndices.Count <= 0)
                return false;

            index = SelectedIndices[0];
            if (index < 0 || index >= TotalCount)
                return false;

            item = GetItemAtIndex(index);
            return true;
        }


        private bool EnsureConnectedAndAttachedForResultAction(string actionName)
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
        private void printBox_ModernMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            int index = GetIndexAtPoint(e.Location);
            if (index >= 0)
                SetSelectedIndex(index);
        }

        private void printBox_ModernDoubleClick(object sender, EventArgs e)
        {
            Point pos = printBox.PointToClient(Cursor.Position);
            int itemIndex = GetIndexAtPoint(pos);
            int column = GetColumnAtPoint(pos);

            if (itemIndex < 0)
                return;

            if (column != 1 && column != 2)
                return;

            if (!EnsureConnectedAndAttachedForResultAction("Edit / Write Value"))
                return;

            BeginEditValueAt(itemIndex, column);
        }

        private void editWriteValueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int index;
            SearchListViewItem item;
            if (!TryGetPrimarySelectedItem(out index, out item))
                return;

            if (!EnsureConnectedAndAttachedForResultAction("Edit / Write Value"))
                return;

            BeginEditValueAt(index, 2);
        }

        private void BeginEditValueAt(int itemIndex, int column)
        {
            if (itemIndex < 0 || itemIndex >= TotalCount)
                return;

            int visibleStart = GetVisibleStartIndex();
            int visibleRow = itemIndex - visibleStart;

            if (visibleRow < 0 || visibleRow > MaxItemsPerPage)
                return;

            if (tempTB != null)
            {
                printBox.Controls.Remove(tempTB);
                tempTB.Dispose();
                tempTB = null;
            }

            SearchListViewItem listItem = GetItemAtIndex(itemIndex);
            string[] parsed = ParseListViewItem(listItem, itemIndex);

            tempTB = new TextBox();
            tempTB.Width = addrLabel.Width - 10;
            tempTB.TextAlign = HorizontalAlignment.Center;
            tempTB.ReadOnly = false;
            tempTB.BorderStyle = BorderStyle.FixedSingle;
            tempTB.Text = parsed[column];
            tempTB.Tag = new object[] { itemIndex, column };
            tempTB.Location = new Point((column * addrLabel.Width) + addrLabel.Width / 2 - tempTB.Width / 2, visibleRow * (int)ItemHeight - 2);

            tempTB.KeyDown += editValueTextBox_KeyDown;

            printBox.Controls.Add(tempTB);
            tempTB.BringToFront();
            tempTB.Select();
            tempTB.SelectionStart = 0;
            tempTB.SelectionLength = tempTB.Text.Length;
            tempTB.Focus();
        }

        private void editValueTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox editBox = sender as TextBox;
            if (editBox == null)
                return;

            if (e.KeyCode == Keys.Escape)
            {
                CancelActiveValueEdit();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            if (e.KeyCode != Keys.Enter)
                return;

            object[] tag = editBox.Tag as object[];
            if (tag == null || tag.Length < 2)
                return;

            int itemIndex = (int)tag[0];
            int column = (int)tag[1];

            WriteEditedValue(itemIndex, column, editBox.Text);

            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        private void CancelActiveValueEdit()
        {
            if (tempTB != null)
            {
                printBox.Controls.Remove(tempTB);
                tempTB.Dispose();
                tempTB = null;
                printBox.Refresh();
            }
        }

        private bool IsActiveScanLittleEndian()
        {
            if (parentControl != null)
                return parentControl.ActiveScanLittleEndianForWrites;

            if (SearchControl.Instance != null)
                return SearchControl.Instance.ActiveScanLittleEndianForWrites;

            return false;
        }

        private byte[] NormalizeRawMemoryForDisplay(byte[] raw)
        {
            if (raw == null)
                return raw;

            byte[] normalized = new byte[raw.Length];
            Buffer.BlockCopy(raw, 0, normalized, 0, raw.Length);

            if (IsActiveScanLittleEndian() && normalized.Length > 1)
                Array.Reverse(normalized);
            else
                normalized = misc.notrevif(normalized);

            return normalized;
        }

        private byte[] BuildEditedValueBytes(SearchListViewItem item, int column, string text, out byte[] displayBytes)
        {
            displayBytes = null;

            if (item.align < 0 || item.align >= SearchControl.SearchTypes.Count)
                throw new InvalidOperationException("Unsupported result type.");

            SearchControl.ncSearchType type = SearchControl.SearchTypes[item.align];
            int byteSize = type.ByteSize;

            if (byteSize <= 0)
                throw new InvalidOperationException("Editing this result type is not supported.");

            byte[] valueBytes;

            if (column == 1)
            {
                valueBytes = ParseHexValueBytes(text, byteSize);
            }
            else
            {
                bool oldHex = Form1.ValHex;
                try
                {
                    Form1.ValHex = false;
                    valueBytes = type.ToByteArray(text);
                }
                finally
                {
                    Form1.ValHex = oldHex;
                }

                if (valueBytes == null || valueBytes.Length < byteSize)
                    throw new InvalidOperationException("Could not parse value.");
            }

            displayBytes = new byte[byteSize];
            Buffer.BlockCopy(valueBytes, 0, displayBytes, 0, byteSize);

            byte[] rawBytes = new byte[byteSize];
            Buffer.BlockCopy(displayBytes, 0, rawBytes, 0, byteSize);

            if (IsActiveScanLittleEndian() && byteSize > 1)
                Array.Reverse(rawBytes);

            return rawBytes;
        }

        private byte[] ParseHexValueBytes(string text, int byteSize)
        {
            if (text == null)
                text = "";

            string cleaned = text.Trim();
            cleaned = cleaned.Replace(" ", "");
            cleaned = cleaned.Replace("_", "");
            cleaned = cleaned.Replace("-", "");
            cleaned = cleaned.Replace(",", "");

            if (cleaned.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                cleaned = cleaned.Substring(2);

            if (cleaned.Length == 0)
                throw new InvalidOperationException("Value is empty.");

            if ((cleaned.Length % 2) != 0)
                cleaned = "0" + cleaned;

            int wantedChars = byteSize * 2;
            if (cleaned.Length > wantedChars)
                throw new InvalidOperationException("Hex value is too large for this type.");

            cleaned = cleaned.PadLeft(wantedChars, '0');

            byte[] result = new byte[byteSize];
            for (int i = 0; i < byteSize; i++)
                result[i] = byte.Parse(cleaned.Substring(i * 2, 2), NumberStyles.HexNumber);

            return result;
        }

        private void WriteEditedValue(int itemIndex, int column, string text)
        {
            if (itemIndex < 0 || itemIndex >= TotalCount)
                return;

            if (!EnsureConnectedAndAttachedForResultAction("Write Value"))
                return;

            try
            {
                SearchListViewItem item = GetItemAtIndex(itemIndex);

                byte[] displayBytes;
                byte[] rawBytes = BuildEditedValueBytes(item, column, text, out displayBytes);

                Form1.apiSetMem(item.addr, rawBytes);

                byte[] verifyRaw = new byte[rawBytes.Length];
                if (Form1.apiGetMem(item.addr, ref verifyRaw))
                {
                    item.oldVal = item.newVal;
                    item.newVal = NormalizeRawMemoryForDisplay(verifyRaw);
                }
                else
                {
                    item.oldVal = item.newVal;
                    item.newVal = displayBytes;
                }

                item.refresh = false;
                SetItemAtIndex(item, itemIndex);

                if (tempTB != null)
                {
                    printBox.Controls.Remove(tempTB);
                    tempTB.Dispose();
                    tempTB = null;
                }

                printBox.Refresh();

                if (parentControl != null)
                    parentControl.SetProgBarText("Wrote " + rawBytes.Length.ToString("N0") + " byte(s) to 0x" + item.addr.ToString("X8"));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not write value: " + ex.Message, "NetCheatPS3", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void copyAddressToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int index;
            SearchListViewItem item;
            if (!TryGetPrimarySelectedItem(out index, out item))
                return;

            Clipboard.SetText(item.addr.ToString("X8"));
        }

        private void copyNetCheatCodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int index;
            SearchListViewItem item;
            if (!TryGetPrimarySelectedItem(out index, out item))
                return;

            SearchControl.ncSearchType type = SearchControl.SearchTypes[item.align];

            string code = type.ItemToString(item);
            if (code == null)
                code = "";

            Clipboard.SetText(code.Replace("\0", ""));
        }

        private void setScanToThisRegionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int index;
            SearchListViewItem item;
            if (!TryGetPrimarySelectedItem(out index, out item))
                return;

            ulong start;
            ulong stop;

            if (Form1.Instance != null && Form1.Instance.TryGetContainingMemoryRange(item.addr, out start, out stop))
            {
                Form1.Instance.SetSearchRangeAndShowSearchTab(start, stop);
                return;
            }

            MessageBox.Show(
                "No real discovered/imported range contains 0x" + item.addr.ToString("X8") + ".\r\n\r\n" +
                "Run Find Ranges first, then use Set Scan Range To This Region.",
                "NetCheatPS3",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}