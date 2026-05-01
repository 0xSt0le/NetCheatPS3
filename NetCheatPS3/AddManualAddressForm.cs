using System;
using System.Globalization;
using System.Windows.Forms;

namespace NetCheatPS3
{
    internal sealed class AddManualAddressForm : Form
    {
        private readonly TextBox addressTextBox;
        private readonly ComboBox typeComboBox;
        private readonly Button okButton;
        private readonly Button cancelButton;

        public Func<ulong, string, int, bool> TryAddAddress;

        public AddManualAddressForm()
        {
            Text = "Add address";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new System.Drawing.Size(300, 132);

            Label addressLabel = new Label();
            addressLabel.Text = "Address";
            addressLabel.Left = 12;
            addressLabel.Top = 16;
            addressLabel.AutoSize = true;

            addressTextBox = new TextBox();
            addressTextBox.Left = 82;
            addressTextBox.Top = 12;
            addressTextBox.Width = 196;

            Label typeLabel = new Label();
            typeLabel.Text = "Type";
            typeLabel.Left = 12;
            typeLabel.Top = 50;
            typeLabel.AutoSize = true;

            typeComboBox = new ComboBox();
            typeComboBox.Left = 82;
            typeComboBox.Top = 46;
            typeComboBox.Width = 196;
            typeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            typeComboBox.Items.Add(new ManualAddressType("1 Byte", 1));
            typeComboBox.Items.Add(new ManualAddressType("2 Bytes", 2));
            typeComboBox.Items.Add(new ManualAddressType("4 Bytes", 4));
            typeComboBox.Items.Add(new ManualAddressType("8 Bytes", 8));
            typeComboBox.Items.Add(new ManualAddressType("Float", 4));
            typeComboBox.Items.Add(new ManualAddressType("Double", 8));
            typeComboBox.SelectedIndex = 2;

            okButton = new Button();
            okButton.Text = "OK";
            okButton.Left = 122;
            okButton.Top = 92;
            okButton.Width = 75;
            okButton.Click += okButton_Click;

            cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.Left = 203;
            cancelButton.Top = 92;
            cancelButton.Width = 75;
            cancelButton.DialogResult = DialogResult.Cancel;

            AcceptButton = okButton;
            CancelButton = cancelButton;

            Controls.Add(addressLabel);
            Controls.Add(addressTextBox);
            Controls.Add(typeLabel);
            Controls.Add(typeComboBox);
            Controls.Add(okButton);
            Controls.Add(cancelButton);
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            ulong address;
            if (!TryParseAddress(addressTextBox.Text, out address))
            {
                MessageBox.Show(
                    this,
                    "Enter a valid 32-bit hexadecimal address.",
                    "Add address",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                addressTextBox.Focus();
                addressTextBox.SelectAll();
                return;
            }

            ManualAddressType selectedType = typeComboBox.SelectedItem as ManualAddressType;
            if (selectedType == null)
                return;

            if (TryAddAddress != null && !TryAddAddress(address, selectedType.Name, selectedType.ByteSize))
                return;

            DialogResult = DialogResult.OK;
            Close();
        }

        private static bool TryParseAddress(string text, out ulong address)
        {
            address = 0;
            if (String.IsNullOrWhiteSpace(text))
                return false;

            string normalized = RemoveWhitespace(text);
            if (normalized.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring(2);

            if (normalized.Length == 0 || normalized.Length > 8)
                return false;

            if (!UInt64.TryParse(normalized, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out address))
                return false;

            return address <= 0xFFFFFFFFUL;
        }

        private static string RemoveWhitespace(string text)
        {
            char[] buffer = new char[text.Length];
            int index = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (!Char.IsWhiteSpace(text[i]))
                {
                    buffer[index] = text[i];
                    index++;
                }
            }

            return new string(buffer, 0, index);
        }

        private sealed class ManualAddressType
        {
            public readonly string Name;
            public readonly int ByteSize;

            public ManualAddressType(string name, int byteSize)
            {
                Name = name;
                ByteSize = byteSize;
            }

            public override string ToString()
            {
                return Name;
            }
        }
    }
}
