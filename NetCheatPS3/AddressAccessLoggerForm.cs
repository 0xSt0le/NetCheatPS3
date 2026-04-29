using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using NCAppInterface;

namespace NetCheatPS3
{
    public class AddressAccessLoggerForm : Form
    {
        private readonly IAddressAccessLoggerSession session;
        private readonly Dictionary<string, HitEntry> hits = new Dictionary<string, HitEntry>();
        private readonly ListView hitList = new ListView();
        private readonly Button extraInfoButton = new Button();
        private readonly Button stopButton = new Button();

        public AddressAccessLoggerForm(IAddressAccessLoggerApi loggerApi, ulong address, AddressAccessMode mode)
        {
            if (loggerApi == null)
                throw new ArgumentNullException("loggerApi");

            Text = (mode == AddressAccessMode.Write
                ? "The following opcodes write to "
                : "The following opcodes read from ") + address.ToString("X8");

            Width = 620;
            Height = 360;
            StartPosition = FormStartPosition.CenterParent;

            hitList.Dock = DockStyle.Fill;
            hitList.View = View.Details;
            hitList.FullRowSelect = true;
            hitList.MultiSelect = false;
            hitList.Columns.Add("Count", 80);
            hitList.Columns.Add("Instruction", 480);

            Panel buttonPanel = new Panel();
            buttonPanel.Dock = DockStyle.Bottom;
            buttonPanel.Height = 42;

            extraInfoButton.Text = "Extra Info";
            extraInfoButton.Width = 100;
            extraInfoButton.Left = 8;
            extraInfoButton.Top = 8;
            extraInfoButton.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            extraInfoButton.Click += extraInfoButton_Click;

            stopButton.Text = "Stop";
            stopButton.Width = 100;
            stopButton.Left = 116;
            stopButton.Top = 8;
            stopButton.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            stopButton.Click += stopButton_Click;

            buttonPanel.Controls.Add(extraInfoButton);
            buttonPanel.Controls.Add(stopButton);

            Controls.Add(hitList);
            Controls.Add(buttonPanel);

            session = loggerApi.StartAddressAccessLogger(address, mode, OnAddressAccessHit);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (session != null)
                session.Dispose();

            base.OnFormClosed(e);
        }

        private void OnAddressAccessHit(AddressAccessHit hit)
        {
            if (IsDisposed)
                return;

            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)delegate { OnAddressAccessHit(hit); });
                return;
            }

            if (hit == null)
                return;

            if (!String.IsNullOrEmpty(hit.Error))
            {
                AddErrorHit(hit);
                return;
            }

            string instruction = FormatInstruction(hit);
            string key = hit.ProgramCounter.ToString("X16") + ":" + BytesToHex(hit.InstructionBytes);

            HitEntry entry;
            if (!hits.TryGetValue(key, out entry))
            {
                entry = new HitEntry();
                entry.Hit = hit;
                entry.Item = new ListViewItem("0");
                entry.Item.SubItems.Add(instruction);
                entry.Item.Tag = entry;
                hits.Add(key, entry);
                hitList.Items.Add(entry.Item);
            }

            entry.Count++;
            entry.Hit = hit;
            entry.Item.Text = entry.Count.ToString("N0");
            entry.Item.SubItems[1].Text = instruction;
        }

        private void AddErrorHit(AddressAccessHit hit)
        {
            ListViewItem item = new ListViewItem("");
            item.SubItems.Add("Error - " + hit.Error);
            item.ForeColor = Color.DarkRed;
            hitList.Items.Add(item);
        }

        private void extraInfoButton_Click(object sender, EventArgs e)
        {
            if (hitList.SelectedItems.Count <= 0)
                return;

            HitEntry entry = hitList.SelectedItems[0].Tag as HitEntry;
            if (entry == null || entry.Hit == null)
                return;

            using (AddressAccessLoggerExtraInfoForm form = new AddressAccessLoggerExtraInfoForm(entry.Hit))
            {
                form.ShowDialog(this);
            }
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            if (session != null)
                session.Stop();

            stopButton.Enabled = false;
        }

        private static string FormatInstruction(AddressAccessHit hit)
        {
            return hit.ProgramCounter.ToString("X8") + " - " + BytesToHex(hit.InstructionBytes);
        }

        private static string BytesToHex(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return "";

            char[] chars = new char[bytes.Length * 2];
            const string hex = "0123456789ABCDEF";
            for (int index = 0; index < bytes.Length; index++)
            {
                chars[index * 2] = hex[bytes[index] >> 4];
                chars[index * 2 + 1] = hex[bytes[index] & 0xF];
            }

            return new string(chars);
        }

        private sealed class HitEntry
        {
            public int Count;
            public AddressAccessHit Hit;
            public ListViewItem Item;
        }
    }

    public class AddressAccessLoggerExtraInfoForm : Form
    {
        private readonly AddressAccessHit hit;
        private readonly TextBox textBox = new TextBox();

        public AddressAccessLoggerExtraInfoForm(AddressAccessHit hit)
        {
            this.hit = hit;

            Text = "Extra Info";
            Width = 620;
            Height = 480;
            StartPosition = FormStartPosition.CenterParent;

            textBox.Dock = DockStyle.Fill;
            textBox.Multiline = true;
            textBox.ReadOnly = true;
            textBox.ScrollBars = ScrollBars.Both;
            textBox.Font = new Font(FontFamily.GenericMonospace, 9.0f);
            textBox.Text = BuildText();

            Controls.Add(textBox);
        }

        private string BuildText()
        {
            List<string> lines = new List<string>();
            lines.Add("Watched address: " + hit.WatchedAddress.ToString("X8"));
            lines.Add("Mode: " + hit.Mode.ToString());
            lines.Add("Thread ID: " + hit.ThreadId.ToString("X16"));
            lines.Add("PC: " + hit.ProgramCounter.ToString("X16"));
            lines.Add("SP: " + hit.StackPointer.ToString("X16"));
            lines.Add("Raw DABR: " + hit.RawDabr.ToString("X16"));
            lines.Add("");
            lines.Add("Nearby instruction bytes:");
            lines.AddRange(ReadNearbyInstructionLines());
            lines.Add("");
            lines.Add("Full register capture not implemented yet.");

            return String.Join(Environment.NewLine, lines.ToArray());
        }

        private IEnumerable<string> ReadNearbyInstructionLines()
        {
            List<string> lines = new List<string>();
            if (hit.ProgramCounter == 0)
            {
                lines.Add("PC was not captured.");
                return lines;
            }

            ulong start = hit.ProgramCounter >= 16 ? hit.ProgramCounter - 16 : hit.ProgramCounter;
            start &= ~0x3UL;

            byte[] bytes = new byte[36];
            bool readOk = false;
            try
            {
                if (Form1.curAPI != null && Form1.curAPI.Instance != null)
                    readOk = Form1.curAPI.Instance.GetBytes(start, ref bytes);
            }
            catch
            {
                readOk = false;
            }

            if (!readOk)
            {
                lines.Add("Could not read nearby instruction bytes.");
                return lines;
            }

            for (int offset = 0; offset + 4 <= bytes.Length; offset += 4)
            {
                ulong address = start + (ulong)offset;
                string prefix = address == hit.ProgramCounter ? ">> " : "   ";
                lines.Add(prefix + address.ToString("X8") + " - " + BytesToHex(bytes, offset, 4));
            }

            return lines;
        }

        private static string BytesToHex(byte[] bytes, int offset, int count)
        {
            if (bytes == null || offset < 0 || count <= 0 || offset + count > bytes.Length)
                return "";

            char[] chars = new char[count * 2];
            const string hex = "0123456789ABCDEF";
            for (int index = 0; index < count; index++)
            {
                byte value = bytes[offset + index];
                chars[index * 2] = hex[value >> 4];
                chars[index * 2 + 1] = hex[value & 0xF];
            }

            return new string(chars);
        }
    }
}
