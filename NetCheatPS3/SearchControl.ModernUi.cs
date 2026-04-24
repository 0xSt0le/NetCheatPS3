using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using NetCheatPS3.Scanner;

namespace NetCheatPS3
{
    public partial class SearchControl
    {
        private bool modernSearchUiInitialized = false;
        private ToolTip modernSearchToolTip;
        private Button scanDiagnosticsButton;

        private void InitializeModernSearchUi()
        {
            if (modernSearchUiInitialized)
                return;

            modernSearchUiInitialized = true;

            Form1.ValHex = false;

            if (modernSearchToolTip == null)
                modernSearchToolTip = new ToolTip();

            RemoveLegacyScanButtons();
            EnsureScanDiagnosticsButton();

            if (searchNameBox != null)
                searchNameBox.DropDownStyle = ComboBoxStyle.DropDownList;

            if (searchTypeBox != null)
                searchTypeBox.DropDownStyle = ComboBoxStyle.DropDownList;

            if (littleEndianCB != null)
            {
                littleEndianCB.Checked = false;
                littleEndianCB.Enabled = true;
            }

            if (cleanFloatCB != null)
            {
                modernSearchToolTip.SetToolTip(
                    cleanFloatCB,
                    "Filters noisy float/double scan values: NaN/Infinity, tiny scientific-notation noise, and huge scientific-notation junk. Only affects new scanner results.");
            }

            this.Resize += delegate
            {
                if (modernSearchUiInitialized)
                    UpdateModernSearchLayout();
            };

            searchTypeBox.SelectedIndexChanged += delegate
            {
                UpdateCleanFloatVisibility();
                UpdateModernSearchLayout();
            };

            searchNameBox.SelectedIndexChanged += delegate
            {
                UpdateCleanFloatVisibility();
                UpdateModernSearchLayout();
            };

            searchMemory.Click += delegate
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    if (searchMemory.Text == "Stop")
                    {
                        SetLittleEndianScanLocked(true);
                    }
                    else if (searchMemory.Text == "Initial Scan")
                    {
                        SetLittleEndianScanLocked(false);
                    }

                    UpdateModernSearchLayout();
                });
            };

            SetDefaultSearchTypeTo4Bytes();
            ClearDefaultSearchArgText();
            UpdateCleanFloatVisibility();
            UpdateModernSearchLayout();
        }

        private void RemoveLegacyScanButtons()
        {
            RemoveLegacyScanButton(dumpMem);
            RemoveLegacyScanButton(saveScan);
            RemoveLegacyScanButton(loadScan);
        }

        private void RemoveLegacyScanButton(Button button)
        {
            if (button == null)
                return;

            button.Visible = false;
            button.Enabled = false;
            button.TabStop = false;

            if (Controls.Contains(button))
                Controls.Remove(button);
        }

        private void EnsureScanDiagnosticsButton()
        {
            if (scanDiagnosticsButton != null)
                return;

            scanDiagnosticsButton = new Button();
            scanDiagnosticsButton.Name = "scanDiagnosticsButton";
            scanDiagnosticsButton.Text = "Diagnostics";
            scanDiagnosticsButton.FlatStyle = FlatStyle.Flat;
            scanDiagnosticsButton.Size = new Size(90, 23);
            scanDiagnosticsButton.UseVisualStyleBackColor = true;
            scanDiagnosticsButton.Click += delegate { ShowScanDiagnostics(); };

            Controls.Add(scanDiagnosticsButton);
        }

        private void SetDefaultSearchTypeTo4Bytes()
        {
            if (searchTypeBox == null || searchTypeBox.Items.Count == 0)
                return;

            for (int i = 0; i < searchTypeBox.Items.Count; i++)
            {
                if (String.Equals(searchTypeBox.Items[i].ToString(), "4 bytes", StringComparison.OrdinalIgnoreCase))
                {
                    searchTypeBox.SelectedIndex = i;
                    return;
                }
            }
        }

        private void ClearDefaultSearchArgText()
        {
            Form1.ValHex = false;

            if (SearchArgs == null)
                return;

            foreach (SearchValue searchValue in SearchArgs)
            {
                foreach (Control child in searchValue.Controls)
                {
                    TextBox textBox = child as TextBox;
                    if (textBox != null)
                    {
                        textBox.Text = "";
                        continue;
                    }

                    CheckBox checkBox = child as CheckBox;
                    if (checkBox != null)
                    {
                        checkBox.Checked = false;
                    }
                }
            }
        }

        private bool CurrentSearchTypeIsFloatOrDouble()
        {
            if (searchTypeBox == null || searchTypeBox.SelectedItem == null)
                return false;

            string typeName = searchTypeBox.SelectedItem.ToString();
            return String.Equals(typeName, "Float", StringComparison.OrdinalIgnoreCase) ||
                   String.Equals(typeName, "Double", StringComparison.OrdinalIgnoreCase);
        }

        private void UpdateCleanFloatVisibility()
        {
            if (cleanFloatCB == null)
                return;

            cleanFloatCB.Visible = CurrentSearchTypeIsFloatOrDouble();
        }

        private void SetLittleEndianScanLocked(bool locked)
        {
            if (littleEndianCB != null)
                littleEndianCB.Enabled = !locked;
        }

        public void SetScanRange(ulong start, ulong stop)
        {
            startAddrTB.Text = start.ToString("X8");
            stopAddrTB.Text = stop.ToString("X8");
        }

        private void UpdateModernSearchLayout()
        {
            if (!modernSearchUiInitialized)
                return;

            RemoveLegacyScanButtons();
            EnsureScanDiagnosticsButton();

            int filterY = Math.Max(searchTypeBox.Bottom, stopAddrTB.Bottom) + 8;

            searchPWS.Location = new Point(5, filterY + 3);

            if (littleEndianCB != null)
            {
                littleEndianCB.Location = new Point(searchPWS.Right + 12, filterY + 3);
                littleEndianCB.BackColor = BackColor;
                littleEndianCB.ForeColor = ForeColor;
            }

            if (noNegativeCB != null)
            {
                int x = littleEndianCB != null ? littleEndianCB.Right + 12 : searchPWS.Right + 12;
                noNegativeCB.Location = new Point(x, filterY + 3);
                noNegativeCB.BackColor = BackColor;
                noNegativeCB.ForeColor = ForeColor;
            }

            if (noZeroCB != null)
            {
                int x = noNegativeCB != null ? noNegativeCB.Right + 12 : searchPWS.Right + 12;
                noZeroCB.Location = new Point(x, filterY + 3);
                noZeroCB.BackColor = BackColor;
                noZeroCB.ForeColor = ForeColor;
            }

            if (cleanFloatCB != null)
            {
                cleanFloatCB.Location = new Point(startAddrTB.Right + 12, startAddrTB.Top + 2);
                cleanFloatCB.BackColor = BackColor;
                cleanFloatCB.ForeColor = ForeColor;
                UpdateCleanFloatVisibility();
            }

            int buttonY = filterY + searchPWS.Height + 8;

            searchMemory.Location = new Point(5, buttonY);
            nextSearchMem.Location = new Point(Width - nextSearchMem.Width - 5, buttonY);

            scanDiagnosticsButton.BackColor = BackColor;
            scanDiagnosticsButton.ForeColor = ForeColor;
            scanDiagnosticsButton.Location = new Point(nextSearchMem.Left - scanDiagnosticsButton.Width - 6, buttonY);

            refreshFromMem.Location = new Point(searchMemory.Right + 6, buttonY);
            refreshFromMem.Width = Math.Max(80, scanDiagnosticsButton.Left - refreshFromMem.Left - 6);

            progBar.Location = new Point(5, buttonY + searchMemory.Height + 8);
            progBar.Width = Math.Max(50, Width - 10);

            searchListView1.Location = new Point(3, progBar.Bottom + 10);
            searchListView1.Size = new Size(Math.Max(50, Width - 6), Math.Max(50, Height - searchListView1.Top - 3));
        }

        private void ShowScanDiagnostics()
        {
            MemoryReadStats readStats = MemoryReader.LastCompletedStats;

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("Scan Diagnostics");
            sb.AppendLine();
            sb.AppendLine("API: " + Form1.apiName);
            sb.AppendLine("Connected: " + Form1.connected);
            sb.AppendLine("Attached: " + Form1.attached);
            sb.AppendLine();
            sb.AppendLine("Search mode: " + (searchNameBox.SelectedItem == null ? "" : searchNameBox.SelectedItem.ToString()));
            sb.AppendLine("Type: " + (searchTypeBox.SelectedItem == null ? "" : searchTypeBox.SelectedItem.ToString()));
            sb.AppendLine("Initial scan state: " + isInitialScan);
            sb.AppendLine("Pause When Scanning: " + (searchPWS.Visible && searchPWS.Checked));
            sb.AppendLine("Little Endian selected: " + (littleEndianCB != null && littleEndianCB.Checked));
            sb.AppendLine("Little Endian locked: " + (littleEndianCB != null && !littleEndianCB.Enabled));
            sb.AppendLine();
            sb.AppendLine("Start: 0x" + startAddrTB.Text);
            sb.AppendLine("Stop:  0x" + stopAddrTB.Text);
            sb.AppendLine();
            sb.AppendLine("Visible/list results: " + searchListView1.TotalCount.ToString("N0"));
            sb.AppendLine("Uses new scanner: " + activeScanUsesNewEngine);
            sb.AppendLine("Active scan little-endian: " + activeScanLittleEndian);
            sb.AppendLine("Exact scanner block size: 0x" + ExactScanner.DefaultBlockSize.ToString("X") + " (" + (ExactScanner.DefaultBlockSize / 1024).ToString("N0") + " KB)");
            sb.AppendLine();
            sb.AppendLine("Last scan speed:");
            AppendLastScanSpeedDiagnostics(sb);
            sb.AppendLine();
            sb.AppendLine("Memory reads:");
            sb.AppendLine("Last activity: " + MemoryReader.LastActivity);
            sb.AppendLine("Read attempts: " + readStats.ReadAttempts.ToString("N0"));
            sb.AppendLine("Read OK: " + readStats.ReadSuccesses.ToString("N0"));
            sb.AppendLine("Read failed: " + readStats.ReadFailures.ToString("N0"));
            sb.AppendLine("Bytes requested: " + readStats.BytesRequested.ToString("N0"));
            sb.AppendLine("Bytes read OK: " + readStats.BytesRead.ToString("N0"));
            sb.AppendLine("Full block OK: " + readStats.FullBlockSuccesses.ToString("N0"));
            sb.AppendLine("Full block failed: " + readStats.FullBlockFailures.ToString("N0"));
            sb.AppendLine("Fallback splits: " + readStats.FallbackSplits.ToString("N0"));
            sb.AppendLine("Fallback segment OK: " + readStats.FallbackSegmentSuccesses.ToString("N0"));
            sb.AppendLine("Fallback segment failed: " + readStats.FallbackSegmentFailures.ToString("N0"));
            sb.AppendLine("Partial blocks recovered: " + readStats.PartialBlocksRecovered.ToString("N0"));
            sb.AppendLine();
            sb.AppendLine("Snapshot active: " + HasActiveSnapshot());
            sb.AppendLine("Snapshot count: " + activeSnapshotResultCount.ToString("N0"));
            sb.AppendLine("Snapshot type index: " + activeSnapshotTypeIndex);
            sb.AppendLine("Snapshot byte size: " + activeSnapshotByteSize);
            sb.AppendLine("Snapshot path: " + (String.IsNullOrEmpty(activeSnapshotPath) ? "" : activeSnapshotPath));
            sb.AppendLine();
            sb.AppendLine("Filters:");
            sb.AppendLine("No Negative: " + (noNegativeCB != null && noNegativeCB.Checked));
            sb.AppendLine("No Zero: " + (noZeroCB != null && noZeroCB.Checked));
            sb.AppendLine("Clean Float: " + (cleanFloatCB != null && cleanFloatCB.Checked));

            MessageBox.Show(sb.ToString(), "NetCheatPS3 Scan Diagnostics", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}