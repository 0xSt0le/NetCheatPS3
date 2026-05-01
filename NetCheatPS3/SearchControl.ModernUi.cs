using System;
using System.Drawing;
using System.Windows.Forms;

namespace NetCheatPS3
{
    public partial class SearchControl
    {
        private bool modernSearchUiInitialized = false;
        private ToolTip modernSearchToolTip;
        private Button addAddressButton;
        private Label exactBlockSizeLabel;
        private ComboBox exactBlockSizeBox;
        private CheckBox compareFirstScanCB;
        private CheckBox fuzzyValueCB;

        private void InitializeModernSearchUi()
        {
            if (modernSearchUiInitialized)
                return;

            modernSearchUiInitialized = true;

            Form1.ValHex = false;

            if (startAddrTB != null) startAddrTB.MaxLength = 8;
            if (stopAddrTB != null) stopAddrTB.MaxLength = 8;

            if (modernSearchToolTip == null)
                modernSearchToolTip = new ToolTip();

            EnsureAddAddressButton();
            EnsureExactBlockSizeSelector();            EnsureCompareFirstScanCheckbox();
            EnsureFuzzyValueCheckbox();
            EnsureSnapshotCleanupOnFormClose();

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
                cleanFloatCB.Text = "Simple Values Only";
                modernSearchToolTip.SetToolTip(
                    cleanFloatCB,
                    "Shows only practical float/double values and hides noisy tiny/huge/invalid values. Only affects new scanner results.");
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
                        SetScanChainOptionsLocked(true);
                    }
                    else if (searchMemory.Text == "Initial Scan")
                    {
                        SetScanChainOptionsLocked(false);
                    }

                    UpdateModernSearchLayout();
                });
            };

            SimplifySearchTypes();
            ResetSearchTypes();
            SetDefaultSearchTypeTo4Bytes();
            SimplifySearchComparisonModes();
            ResetSearchCompBox();
            ClearDefaultSearchArgText();
            UpdateCleanFloatVisibility();
            UpdateModernSearchLayout();
        }

        private void EnsureAddAddressButton()
        {
            if (addAddressButton != null)
                return;

            addAddressButton = new Button();
            addAddressButton.Name = "addAddressButton";
            addAddressButton.Text = "Add Address";
            addAddressButton.FlatStyle = FlatStyle.Flat;
            addAddressButton.Size = new Size(90, 23);
            addAddressButton.UseVisualStyleBackColor = true;
            addAddressButton.Click += delegate
            {
                if (Form1.Instance != null)
                    Form1.Instance.ShowAddManualAddressDialog();
            };

            Controls.Add(addAddressButton);
        }

        private void EnsureExactBlockSizeSelector()
        {
            if (exactBlockSizeBox != null)
                return;

            exactBlockSizeLabel = new Label();
            exactBlockSizeLabel.Name = "exactBlockSizeLabel";
            exactBlockSizeLabel.Text = "Block";
            exactBlockSizeLabel.AutoSize = true;
            exactBlockSizeLabel.TextAlign = ContentAlignment.MiddleLeft;

            exactBlockSizeBox = new ComboBox();
            exactBlockSizeBox.Name = "exactBlockSizeBox";
            exactBlockSizeBox.DropDownStyle = ComboBoxStyle.DropDownList;
            exactBlockSizeBox.Items.AddRange(new object[]
            {
                "64 KB",
                "128 KB",
                "256 KB",
                "512 KB",
                "1 MB",
                "2 MB",
                "4 MB"
            });
            exactBlockSizeBox.SelectedItem = "1 MB";
            exactBlockSizeBox.Width = 80;

            if (modernSearchToolTip != null)
            {
                modernSearchToolTip.SetToolTip(
                    exactBlockSizeBox,
                    "Exact-scan memory read size. Larger blocks reduce TMAPI calls but may fail on unstable ranges. Fallback splitting still protects failed reads.");
            }

            Controls.Add(exactBlockSizeLabel);
            Controls.Add(exactBlockSizeBox);
        }

        private void EnsureCompareFirstScanCheckbox()
        {
            if (compareFirstScanCB != null)
                return;

            compareFirstScanCB = new CheckBox();
            compareFirstScanCB.Name = "compareFirstScanCB";
            compareFirstScanCB.Text = "Compare to First Scan";
            compareFirstScanCB.AutoSize = true;
            compareFirstScanCB.Checked = false;
            compareFirstScanCB.Visible = false;

            if (modernSearchToolTip != null)
            {
                modernSearchToolTip.SetToolTip(
                    compareFirstScanCB,
                    "When checked, Next Scan compares current memory against the original first snapshot instead of the previous narrowed snapshot.");
            }

            Controls.Add(compareFirstScanCB);
        }

        private void EnsureFuzzyValueCheckbox()
        {
            if (fuzzyValueCB != null)
                return;

            fuzzyValueCB = new CheckBox();
            fuzzyValueCB.Name = "fuzzyValueCB";
            fuzzyValueCB.Text = "Fuzzy Value";
            fuzzyValueCB.AutoSize = true;
            fuzzyValueCB.Checked = false;
            fuzzyValueCB.Visible = false;

            if (modernSearchToolTip != null)
            {
                modernSearchToolTip.SetToolTip(
                    fuzzyValueCB,
                    "Float/double Exact Value helper. 70 = about 69.9-70.1, 70.0 = exact, 70.00 = about 69.99-70.01.");
            }

            Controls.Add(fuzzyValueCB);
        }
        private bool IsCompareToFirstScanChecked()
        {
            return compareFirstScanCB != null && compareFirstScanCB.Visible && compareFirstScanCB.Checked;
        }
        public int GetSelectedExactScanBlockSize()
        {
            try
            {
                if (exactBlockSizeBox == null || exactBlockSizeBox.SelectedItem == null)
                    return 0x100000;

                string text = exactBlockSizeBox.SelectedItem.ToString();

                if (String.Equals(text, "64 KB", StringComparison.OrdinalIgnoreCase))
                    return 0x10000;
                if (String.Equals(text, "128 KB", StringComparison.OrdinalIgnoreCase))
                    return 0x20000;
                if (String.Equals(text, "256 KB", StringComparison.OrdinalIgnoreCase))
                    return 0x40000;
                if (String.Equals(text, "512 KB", StringComparison.OrdinalIgnoreCase))
                    return 0x80000;
                if (String.Equals(text, "1 MB", StringComparison.OrdinalIgnoreCase))
                    return 0x100000;
                if (String.Equals(text, "2 MB", StringComparison.OrdinalIgnoreCase))
                    return 0x200000;
                if (String.Equals(text, "4 MB", StringComparison.OrdinalIgnoreCase))
                    return 0x400000;
            }
            catch
            {
            }

            return 0x100000;
        }

        private string GetSelectedExactScanBlockSizeText()
        {
            int size = GetSelectedExactScanBlockSize();

            if (size >= 0x100000 && (size % 0x100000) == 0)
                return "0x" + size.ToString("X") + " (" + (size / 0x100000).ToString("N0") + " MB)";

            return "0x" + size.ToString("X") + " (" + (size / 1024).ToString("N0") + " KB)";
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

            if (startAddrTB != null) startAddrTB.MaxLength = 8;
            if (stopAddrTB != null) stopAddrTB.MaxLength = 8;

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
            bool show = CurrentSearchTypeIsFloatOrDouble();

            if (cleanFloatCB != null)
                cleanFloatCB.Visible = show;

            if (fuzzyValueCB != null)
                fuzzyValueCB.Visible = show;
        }

        private void SetLittleEndianScanLocked(bool locked)
        {
            SetScanChainOptionsLocked(locked);
        }

        private void SetScanChainOptionsLocked(bool locked)
        {
            if (littleEndianCB != null)
                littleEndianCB.Enabled = !locked;

            if (exactBlockSizeBox != null)
                exactBlockSizeBox.Enabled = !locked;

            if (noNegativeCB != null)
                noNegativeCB.Enabled = !locked;

            if (noZeroCB != null)
                noZeroCB.Enabled = !locked;

            if (cleanFloatCB != null)
                cleanFloatCB.Enabled = !locked;

            if (fuzzyValueCB != null)
                fuzzyValueCB.Enabled = !locked;

            if (compareFirstScanCB != null)
                compareFirstScanCB.Enabled = !locked;
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

            EnsureAddAddressButton();
            EnsureExactBlockSizeSelector();
            EnsureCompareFirstScanCheckbox();
            EnsureFuzzyValueCheckbox();
            EnsureSnapshotCleanupOnFormClose();

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

            if (fuzzyValueCB != null)
            {
                fuzzyValueCB.Location = new Point(stopAddrTB.Right + 12, stopAddrTB.Top + 2);
                fuzzyValueCB.BackColor = BackColor;
                fuzzyValueCB.ForeColor = ForeColor;
                fuzzyValueCB.Visible = CurrentSearchTypeIsFloatOrDouble();
            }

            int optionsBottom = Math.Max(startAddrTB.Bottom, stopAddrTB.Bottom);

            if (exactBlockSizeLabel != null && exactBlockSizeBox != null)
            {
                int x = cleanFloatCB != null ? cleanFloatCB.Right + 16 : startAddrTB.Right + 16;
                exactBlockSizeLabel.Location = new Point(x, startAddrTB.Top + 4);
                exactBlockSizeLabel.BackColor = BackColor;
                exactBlockSizeLabel.ForeColor = ForeColor;

                exactBlockSizeBox.Location = new Point(exactBlockSizeLabel.Right + 5, startAddrTB.Top - 1);
                exactBlockSizeBox.BackColor = BackColor;
                exactBlockSizeBox.ForeColor = ForeColor;

                optionsBottom = Math.Max(optionsBottom, exactBlockSizeBox.Bottom);
            }

            if (compareFirstScanCB != null)
            {
                int x = exactBlockSizeLabel != null ? exactBlockSizeLabel.Left : startAddrTB.Right + 16;
                int y = exactBlockSizeBox != null ? exactBlockSizeBox.Bottom + 3 : startAddrTB.Bottom + 3;

                compareFirstScanCB.Location = new Point(x, y);
                compareFirstScanCB.BackColor = BackColor;
                compareFirstScanCB.ForeColor = ForeColor;
                compareFirstScanCB.Visible = !isInitialScan;

                if (isInitialScan)
                    compareFirstScanCB.Checked = false;

                compareFirstScanCB.Enabled = !isInitialScan;

                if (compareFirstScanCB.Visible)
                    optionsBottom = Math.Max(optionsBottom, compareFirstScanCB.Bottom);
            }

            int buttonY = Math.Max(filterY + searchPWS.Height + 8, optionsBottom + 8);

            searchMemory.Location = new Point(5, buttonY);
            nextSearchMem.Location = new Point(Width - nextSearchMem.Width - 5, buttonY);

            addAddressButton.BackColor = BackColor;
            addAddressButton.ForeColor = ForeColor;
            addAddressButton.Location = new Point(nextSearchMem.Left - addAddressButton.Width - 6, buttonY);

            refreshFromMem.Location = new Point(searchMemory.Right + 6, buttonY);
            refreshFromMem.Width = Math.Max(80, addAddressButton.Left - refreshFromMem.Left - 6);

            progBar.Location = new Point(5, buttonY + searchMemory.Height + 8);
            progBar.Width = Math.Max(50, Width - 10);

            searchListView1.Location = new Point(3, progBar.Bottom + 10);
            searchListView1.Size = new Size(Math.Max(50, Width - 6), Math.Max(50, Height - searchListView1.Top - 3));
        }

    }
}
