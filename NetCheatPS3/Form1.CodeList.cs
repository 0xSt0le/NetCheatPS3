using System;
using System.Linq;
using System.Windows.Forms;

namespace NetCheatPS3
{
    public partial class Form1
    {
        private void cbList_SelectedIndexChanged(object sender, EventArgs e)
        {
            cbListIndex = FindLI();
            if (cbListIndex < 0)
                cbListIndex = 0;

            UpdateCB(cbListIndex);
            cbCodes.BackColor = BackColor;
            cbCodes.ForeColor = ForeColor;
            cbCodes.SelectionStart = 0;
            cbCodes.SelectionLength = cbCodes.Text.Length;
            cbCodes.SelectionColor = ForeColor;
            cbCodes.SelectionLength = 0;
        }

        /* Finds the listindex of the listview cbList */
        public int FindLI()
        {
            int x = 0;

            //Potentially let the list update the selected Code
            Application.DoEvents();

            //Finds the list index (selected Code)
            for (x = 0; x <= cbList.Items.Count - 1; x++)
            {
                if (cbList.Items[x].Selected == true)
                    return x;
            }

            return -1;
        }

        /* Updates the code name, state, and codes */
        private void UpdateCB(int Index)
        {
            if (Index < 0 || Index >= MaxCodes)
            {
                cbName.Text = "";
                cbCodes.Text = "";
                cbState.Checked = false;
                return;
            }

            //Update the textboxes to the new code
            if (Index >= Codes.Count)
                return;
            cbName.Text = Codes[Index].name;
            cbCodes.Text = Codes[Index].codes;
            cbState.Checked = Codes[Index].state;
        }

        /* Adds a new code to the list */
        private void cbAdd_Click(object sender, EventArgs e)
        {
            cbList.Items.Add("NEW CODE");
            CodesCount = cbList.Items.Count - 1;
            cbList.Items[CodesCount].ForeColor = ncForeColor;
            cbList.Items[CodesCount].BackColor = ncBackColor;

            CodeDB cdb = new CodeDB();
            cdb.name = "NEW CODE";
            cdb.state = false;
            cdb.codes = "";
            //Codes[CodesCount].name = "NEW CODE";
            //Codes[CodesCount].state = false;
            //Codes[CodesCount].codes = "";
            Codes.Add(cdb);
        }

        /* Removes a code from the list */
        private void cbRemove_Click(object sender, EventArgs e)
        {
            int ind = cbListIndex;
            if (ind < 0)
                return;

            cbList.Items[ind].Remove();

            if (ind < Codes.Count)
                Codes.RemoveAt(ind);

            if (cbListIndex >= (cbList.Items.Count - 1))
                cbListIndex = cbList.Items.Count - 1;

            UpdateCB(cbListIndex);
        }

        /* Imports a ncl file into the the list */
        private void cbImport_Click(object sender, EventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog();
            fd.Filter = "NetCheat List files (*.ncl)|*.ncl|All files (*.*)|*.*";
            fd.RestoreDirectory = true;

            if (fd.ShowDialog() == DialogResult.OK)
            {
                CodeDB[] ret = fileio.OpenFile(fd.FileName);

                if (ret == null)
                    return;

                int cnt = 0;
                if (cbListIndex >= 0)
                    cbList.Items[cbListIndex].Selected = false;
                for (int x = 0; x < ret.Length; x++)
                {

                    if (ret[x].name == null)
                        break;

                    ret[x].filename = fd.FileName;
                    if (ret[x].state)
                        cbList.Items.Add("+ " + ret[x].name);
                    else
                        cbList.Items.Add(ret[x].name);

                    cnt = cbList.Items.Count - 1;
                    cbList.Items[cnt].ForeColor = ncForeColor;
                    cbList.Items[cnt].BackColor = ncBackColor;
                    Codes.Add(ret[x]);
                }

                CodesCount = cnt;
                cbList.Items[cnt].Selected = true;
            }
        }

        /* Save the selected code as a ncl file */
        private void cbSaveAs_Click(object sender, EventArgs e)
        {
            SaveFileDialog fd = new SaveFileDialog();
            fd.Filter = "NetCheat List files (*.ncl)|*.ncl|All files (*.*)|*.*";
            fd.RestoreDirectory = true;

            if (fd.ShowDialog() == DialogResult.OK)
            {
                fileio.SaveFile(fd.FileName, Codes[cbListIndex]);

                CodeDB c = Codes[cbListIndex];
                c.filename = fd.FileName;
                //Codes[cbListIndex].filename = fd.FileName;
                Codes[cbListIndex] = c;
            }
        }

        /* Saves all the codes as an ncl file */
        private void cbSaveAll_Click(object sender, EventArgs e)
        {
            SaveFileDialog fd = new SaveFileDialog();
            fd.Filter = "NetCheat List files (*.ncl)|*.ncl|All files (*.*)|*.*";
            fd.RestoreDirectory = true;

            if (fd.ShowDialog() == DialogResult.OK)
            {
                fileio.SaveFileAll(fd.FileName);
            }
        }

        /* Quickly saves the code as a ncl */
        private void cbSave_Click(object sender, EventArgs e)
        {
            fileio.SaveFile(Codes[cbListIndex].filename, Codes[cbListIndex]);
        }

        /* Writes the selected code to the PS3 */
        private void cbWrite_Click(object sender, EventArgs e)
        {
            if (Codes[cbListIndex].backUp == null || Codes[cbListIndex].backUp.Length == 0)
            {
                CodeDB c = Codes[cbListIndex];
                c.backUp = codes.CreateBackupPS3(Codes[cbListIndex]);
                //Codes[cbListIndex].backUp = codes.CreateBackupPS3(Codes[cbListIndex]);
                Codes[cbListIndex] = c;
            }
            codes.WriteToPS32(Codes[cbListIndex]);
        }

        /* Toggles whether the code is constant writing or not */
        private void cbState_CheckedChanged(object sender, EventArgs e)
        {
            int ind = cbListIndex;
            if (ind < 0)
                return;

            if (cbState.Checked == true)
                cbList.Items[ind].Text = "+ " + Codes[ind].name;
            else
                cbList.Items[ind].Text = Codes[ind].name;

            CodeDB c = Codes[ind];
            c.state = cbState.Checked;
            Codes[ind] = c;

            ConstantLoop = 1;
        }

        /* Updates the name in cbList */
        private void cbName_TextChanged(object sender, EventArgs e)
        {
            int ind = cbListIndex;
            if (ind < 0)
                return;

            CodeDB c = Codes[ind];
            c.name = cbName.Text;
            //Codes[ind].name = cbName.Text;
            Codes[ind] = c;
            if (Codes[ind].state == true)
            {
                cbList.Items[ind].Text = "+ " + cbName.Text;
            }
            else
            {
                cbList.Items[ind].Text = cbName.Text;
            }
        }

        /* Toggles constant write */
        private void cbList_DoubleClick(object sender, EventArgs e)
        {
            int ind = cbListIndex;
            if (ind < 0)
                return;

            if (Codes[ind].state == true)
            {
                cbList.Items[ind].Text = Codes[ind].name;

                CodeDB c = Codes[ind];
                c.state = false;
                Codes[ind] = c;
            }
            else
            {
                cbList.Items[ind].Text = "+ " + Codes[ind].name;
                CodeDB c = Codes[ind];
                c.state = true;
                Codes[ind] = c;
                ConstantLoop = 1;
            }
            Application.DoEvents();
            UpdateCB(ind);
        }

        private void cbCodes_TextChanged(object sender, EventArgs e)
        {
            int ind = cbListIndex;
            if (ind < 0)
                return;

            

            CodesCount = cbList.Items.Count - 1;
            CodeDB c = Codes[ind];
            c.codes = cbCodes.Text.Replace("{", "").Replace("}", "").Replace("#", "");
            Codes[ind] = c;
            codes.UpdateCData(Codes[ind].codes, ind);
        }

        private void cbCodes_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.A && e.Shift == false && e.Control)
            {
                cbCodes.SelectionStart = 0;
                cbCodes.SelectionLength = cbCodes.Text.Length;
            }
        }

        private void cbCodes_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                bool hasValidCode = false;
                bool startsAsCond = false;
                ncCode[] cds = null;

                try
                {
                    cds = codes.ParseCodeStringFull(cbCodes.SelectedText);

                    for (int x = 0; x < cds.Length; x++)
                    {
                        if (codes.isCodeValid(cds[x]))
                        {
                            hasValidCode = true;

                            if (x == 0 && (cds[x].codeType.ToString().ToUpper() == "D" || cds[x].codeType.ToString().ToUpper() == "E"))
                            {
                                if (cbCodes.SelectedText.StartsWith(cds[x].codeType.ToString() + cds[x].codeArg0.ToString("X") + " " + cds[x].codeArg1.ToString("X8") + " " + misc.ByteAToStringHex(cds[x].codeArg2, "")))
                                    startsAsCond = true;
                            }

                            break;
                        }
                    }
                }
                catch { }

                editConditionalToolStripMenuItem.Visible = startsAsCond;

                if (hasValidCode)
                {
                    bwStripMenuItem1.Visible = true;
                    twStripMenuItem1.Visible = true;
                    fwStripMenuItem1.Visible = true;
                    codesToolMenuStrip.Show(sender as Control, e.Location);
                }
                else
                {
                    bwStripMenuItem1.Visible = false;
                    twStripMenuItem1.Visible = false;
                    fwStripMenuItem1.Visible = false;
                    codesToolMenuStrip.Show(sender as Control, e.Location);
                }
            }
        }

        private void cbCodes_KeyUp(object sender, KeyEventArgs e)
        {
            
        }

        private void cbResetWrite_Click(object sender, EventArgs e)
        {
            //foreach (ncCode nCode in Codes[cbListIndex].backUp)
            for (int x = Codes[cbListIndex].backUp.Length - 1; x >= 0; x--)
            {
                if (Codes[cbListIndex].backUp[x].codeType == '0' || Codes[cbListIndex].backUp[x].codeType == '1')
                    apiSetMem(Codes[cbListIndex].backUp[x].codeArg1, Codes[cbListIndex].backUp[x].codeArg2);
            }
            if (Codes[cbListIndex].backUp.Length == 0)
                MessageBox.Show("Please write before you reset.\nKeep in mind constant writing doesn't save a backup and editing the text box erases the backup.");
        }

        private void cbBackupWrite_Click(object sender, EventArgs e)
        {
            CodeDB c = Codes[cbListIndex];
            c.backUp = codes.CreateBackupPS3(Codes[cbListIndex]);
            Codes[cbListIndex] = c;
        }


        private void cbBManager_Click(object sender, EventArgs e)
        {
            if (FRManager == null || FRManager.IsDisposed)
            {
                FRManager = new FindReplaceManager();
                FRManager.ForeColor = ncForeColor;
                FRManager.BackColor = ncBackColor;
                HandlePluginControls(FRManager.Controls);
                CreateProbeAddressButton();
            }

            FRManager.Show();
            FRManager.Focus();
        }

        private void bwStripMenuItem1_Click(object sender, EventArgs e)
        {
            string text = "";
            string[] lines = cbCodes.SelectedText.Split(new char[] { '\r', '\n' });
            //ncCode[] cds = codes.ParseCodeStringFull(cbCodes.SelectedText);
            char[] supported = new char[] { '1', '2' };

            for (int x = 0; x < lines.Length; x++)
            {
                ncCode c = codes.ParseCodeStringFull(lines[x])[0];
                if (codes.isCodeValid(c) && supported.Contains(c.codeType))
                {
                    text += "0 " + c.codeArg1.ToString("X8") + " " + misc.ByteAToStringHex(c.codeArg2, "") + "\r\n";
                }
                else
                {
                    text += lines[x] + "\r\n";
                }

            }

            if (text.EndsWith("\r\n"))
                text = text.Remove(text.Length - 2, 2);

            cbCodes.SelectedText = text;
        }

        private void twStripMenuItem1_Click(object sender, EventArgs e)
        {
            string text = "";
            string[] lines = cbCodes.SelectedText.Split(new char[] { '\r', '\n' });
            //ncCode[] cds = codes.ParseCodeStringFull(cbCodes.SelectedText);
            char[] supported = new char[] { '0' };

            for (int x = 0; x < lines.Length; x++)
            {
                ncCode c = codes.ParseCodeStringFull(lines[x])[0];
                if (codes.isCodeValid(c) && supported.Contains(c.codeType))
                {
                    text += "1 " + c.codeArg1.ToString("X8") + " " + misc.ByteAToString(c.codeArg2, "").Replace("\0", "") + "\r\n";
                }
                else
                {
                    text += lines[x] + "\r\n";
                }

            }

            if (text.EndsWith("\r\n"))
                text = text.Remove(text.Length - 2, 2);

            cbCodes.SelectedText = text;
        }

        private void fwStripMenuItem1_Click(object sender, EventArgs e)
        {
            string text = "";
            string[] lines = cbCodes.SelectedText.Split(new char[] { '\r', '\n' });
            //ncCode[] cds = codes.ParseCodeStringFull(cbCodes.SelectedText);
            char[] supported = new char[] { '0' };

            for (int x = 0; x < lines.Length; x++)
            {
                ncCode c = codes.ParseCodeStringFull(lines[x])[0];
                if (codes.isCodeValid(c) && supported.Contains(c.codeType) && c.codeArg2.Length <= 4)
                {
                    byte[] newBA = c.codeArg2;
                    if (c.codeArg2.Length < 4)
                    {
                        newBA = new byte[4];
                        int mod = (int)c.codeArg1 % 4;
                        Array.Copy(c.codeArg2, 0, newBA, mod, c.codeArg2.Length);
                    }

                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(newBA);
                    text += "2 " + c.codeArg1.ToString("X8") + " " + BitConverter.ToSingle(newBA, 0) + "\r\n";
                }
                else
                {
                    text += lines[x] + "\r\n";
                }

            }

            if (text.EndsWith("\r\n"))
                text = text.Remove(text.Length - 2, 2);

            cbCodes.SelectedText = text;
        }

        private void createCondStripMenuItem1_Click(object sender, EventArgs e)
        {
            ConditionalEditor ce = new ConditionalEditor();
            ce.rtb = cbCodes;
            ce.isEditing = false;
            ce.code = cbCodes.SelectedText;
            ce.ShowDialog();

        }

        private void editConditionalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConditionalEditor ce = new ConditionalEditor();
            ce.rtb = cbCodes;
            ce.isEditing = true;
            ce.code = cbCodes.SelectedText;
            ce.ShowDialog();
        }
    }
}
