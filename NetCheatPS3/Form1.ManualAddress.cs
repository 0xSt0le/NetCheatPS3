using System;
using System.Windows.Forms;

namespace NetCheatPS3
{
    public partial class Form1
    {
        internal void ShowAddManualAddressDialog()
        {
            using (AddManualAddressForm form = new AddManualAddressForm())
            {
                form.TryAddAddress = TryAddManualAddress;
                form.ShowDialog(this);
            }
        }

        private bool TryAddManualAddress(ulong address, string typeName, int byteSize)
        {
            if (!connected || !attached || curAPI == null || curAPI.Instance == null)
            {
                string message = "Connect and attach before adding a manual address.";
                SetMainStatusSafe(message);
                MessageBox.Show(this, message, "Add address", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            byte[] value = new byte[byteSize];
            if (!apiGetMem(address, ref value))
            {
                string message = "Could not read the current value at 0x" + address.ToString("X8") + ".";
                SetMainStatusSafe(message);
                MessageBox.Show(this, message, "Add address", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            string addressText = address.ToString("X8");
            string codeText = "0 " + addressText + " " + BytesToHex(value);
            string name = typeName + " @ " + addressText;

            CodeDB code = new CodeDB();
            code.name = name;
            code.state = false;
            code.codes = codeText;
            code.backUp = null;

            if (cbListIndex >= 0 && cbListIndex < cbList.Items.Count)
                cbList.Items[cbListIndex].Selected = false;

            cbList.Items.Add(name);
            int index = cbList.Items.Count - 1;
            Codes.Add(code);
            CodesCount = index;
            cbListIndex = index;

            cbList.Items[index].ForeColor = ncForeColor;
            cbList.Items[index].BackColor = ncBackColor;
            codes.UpdateCData(codeText, index);
            cbList.Items[index].Selected = true;
            cbList.Items[index].Focused = true;
            cbList.Items[index].EnsureVisible();
            UpdateCB(index);

            SetMainStatusSafe("Added manual address 0x" + addressText + " as " + typeName + ".");
            return true;
        }

        private static string BytesToHex(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return String.Empty;

            char[] chars = new char[bytes.Length * 2];
            const string hex = "0123456789ABCDEF";
            for (int index = 0; index < bytes.Length; index++)
            {
                chars[index * 2] = hex[bytes[index] >> 4];
                chars[index * 2 + 1] = hex[bytes[index] & 0xF];
            }

            return new string(chars);
        }
    }
}
