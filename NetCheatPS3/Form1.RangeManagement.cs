using System;
using System.Windows.Forms;

namespace NetCheatPS3
{
    public partial class Form1
    {
        public int rangeListIndex = -1;

        private void AddRange_Click(object sender, EventArgs e)
        {
            string[] rV = { "00000000", "00000000" };
            ListViewItem a = new ListViewItem(rV);
            rangeView.Items.Add(a);

            //Update range array
            UpdateMemArray();
        }

        private void RemoveRange_Click(object sender, EventArgs e)
        {
            if (rangeListIndex < 0)
                return;

            rangeView.Items.RemoveAt(rangeListIndex);

            //Update range array
            UpdateMemArray();

            if (rangeListIndex >= rangeView.Items.Count)
                rangeListIndex = (rangeView.Items.Count - 1);

            if (rangeListIndex < 0)
                return;

            rangeView.Items[rangeListIndex].Selected = true;
        }

        private void rangeView_SelectedIndexChanged(object sender, EventArgs e)
        {
            for (int x = 0; x < rangeView.Items.Count; x++)
            {
                if (rangeView.Items[x].Selected)
                {
                    rangeListIndex = x;
                    return;
                }
            }
        }

        private void rangeView_DoubleClick(object sender, EventArgs e)
        {
            IBArg[] a = new IBArg[2];

            a[0].defStr = rangeView.Items[rangeListIndex].SubItems[0].Text;
            a[0].label = "Start Address";

            a[1].defStr = rangeView.Items[rangeListIndex].SubItems[1].Text;
            a[1].label = "End Address";

            a = CallIBox(a);

            if (a == null)
                return;

            if (a[0].retStr.Length == 8)
                rangeView.Items[rangeListIndex].SubItems[0].Text = a[0].retStr;
            else
                rangeView.Items[rangeListIndex].SubItems[0].Text = a[0].defStr;

            if (a[1].retStr.Length == 8)
                rangeView.Items[rangeListIndex].SubItems[1].Text = a[1].retStr;
            else
                rangeView.Items[rangeListIndex].SubItems[1].Text = a[1].defStr;

            //Update range array
            UpdateMemArray();
        }

        private void ImportRange_Click(object sender, EventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog();
            fd.Filter = "NetCheat Memory Range files (*.ncm)|*.ncm|All files (*.*)|*.*";
            fd.RestoreDirectory = true;

            if (fd.ShowDialog() == DialogResult.OK)
            {
                //Check if file is already added to the recent range imports
                bool added = false;
                foreach (string rI in rangeImports)
                {
                    if (rI == fd.FileName)
                    {
                        added = true;
                        break;
                    }
                }

                if (!added)
                {
                    //File path
                    Array.Resize(ref rangeImports, rangeImports.Length + 1);
                    rangeImports[rangeImports.Length - 1] = fd.FileName;

                    //Order
                    Array.Resize(ref rangeOrder, rangeOrder.Length + 1);
                    for (int rO = 0; rO < rangeOrder.Length - 1; rO++)
                        rangeOrder[rO]++;
                    rangeOrder[rangeOrder.Length - 1] = 0;

                    //Add to recRangeBox
                    System.IO.FileInfo fi = new System.IO.FileInfo(fd.FileName);
                    ListViewItem lvi = new ListViewItem(new string[] { fi.Name });
                    lvi.Tag = rangeOrder.Length - 1;
                    lvi.ToolTipText = fi.FullName;
                    recRangeBox.Items.Insert(0, lvi);

                    SaveOptions();
                }


                ListView a = new ListView();
                a = fileio.OpenRangeFile(fd.FileName);

                if (a == null)
                    return;

                rangeView.Items.Clear();
                string[] str = new string[2];
                for (int x = 0; x < a.Items.Count; x++)
                {
                    str[0] = a.Items[x].SubItems[0].Text;
                    str[1] = a.Items[x].SubItems[1].Text;
                    ListViewItem strLV = new ListViewItem(str);
                    rangeView.Items.Add(strLV);
                }

                //Update range array
                UpdateMemArray();

                Text = "NetCheat PS3 " + versionNum + " by Dnawrkshp (" + new System.IO.FileInfo(fd.FileName).Name + ")";
            }
        }

        private void SaveRange_Click(object sender, EventArgs e)
        {
            SaveFileDialog fd = new SaveFileDialog();
            fd.Filter = "NetCheat Memory Range files (*.ncm)|*.ncm|All files (*.*)|*.*";
            fd.RestoreDirectory = true;

            if (fd.ShowDialog() == DialogResult.OK)
            {
                fileio.SaveRangeFile(fd.FileName, rangeView);

                //Check if file is already added to the recent range imports
                bool added = false;
                foreach (string rI in rangeImports)
                {
                    if (rI == fd.FileName)
                    {
                        added = true;
                        break;
                    }
                }

                if (!added)
                {
                    //File path
                    Array.Resize(ref rangeImports, rangeImports.Length + 1);
                    rangeImports[rangeImports.Length - 1] = fd.FileName;

                    //Order
                    Array.Resize(ref rangeOrder, rangeOrder.Length + 1);
                    for (int rO = 0; rO < rangeOrder.Length - 1; rO++)
                        rangeOrder[rO]++;
                    rangeOrder[rangeOrder.Length - 1] = 0;

                    //Add to recRangeBox
                    System.IO.FileInfo fi = new System.IO.FileInfo(fd.FileName);
                    ListViewItem lvi = new ListViewItem(new string[] { fi.Name });
                    lvi.Tag = rangeOrder.Length - 1;
                    lvi.ToolTipText = fi.FullName;
                    recRangeBox.Items.Insert(0, lvi);

                    SaveOptions();
                }
            }
        }

        /* Updates the memory array (range) to the array specified by the rangeView listview */
        public void UpdateMemArray()
        {
            misc.MemArray = new uint[rangeView.Items.Count * 2];

            for (int x = 0; x < rangeView.Items.Count; x++)
            {
                misc.MemArray[x * 2] = uint.Parse(rangeView.Items[x].SubItems[0].Text,
                    System.Globalization.NumberStyles.HexNumber);
                misc.MemArray[(x * 2) + 1] = uint.Parse(rangeView.Items[x].SubItems[1].Text,
                    System.Globalization.NumberStyles.HexNumber);
            }
        }

        private void RangeUp_Click(object sender, EventArgs e)
        {
            if (rangeListIndex <= 0)
                return;

            ListViewItem selected = rangeView.Items[rangeListIndex];

            rangeView.Items.RemoveAt(rangeListIndex);
            rangeView.Items.Insert(rangeListIndex - 1, selected);
        }

        private void RangeDown_Click(object sender, EventArgs e)
        {
            if (rangeListIndex < 0 || rangeListIndex >= rangeView.Items.Count)
                return;

            ListViewItem selected = rangeView.Items[rangeListIndex];

            rangeView.Items.RemoveAt(rangeListIndex);
            rangeView.Items.Insert(rangeListIndex + 1, selected);
        }

        private void recRangeBox_DoubleClick(object sender, EventArgs e)
        {
            
            if (recRangeBox.SelectedIndices.Count >= 0 && recRangeBox.SelectedIndices[0] >= 0)
            {
                int ind = recRangeBox.SelectedIndices[0];
                string path = recRangeBox.Items[ind].ToolTipText;

                if (System.IO.File.Exists(path))
                {
                    ListView a = new ListView();
                    a = fileio.OpenRangeFile(path);

                    if (a == null)
                        return;

                    rangeView.Items.Clear();
                    string[] str = new string[2];
                    for (int x = 0; x < a.Items.Count; x++)
                    {
                        str[0] = a.Items[x].SubItems[0].Text;
                        str[1] = a.Items[x].SubItems[1].Text;
                        ListViewItem strLV = new ListViewItem(str);
                        rangeView.Items.Add(strLV);
                    }

                    //Update range array
                    UpdateMemArray();

                    Text = "NetCheat PS3 " + versionNum + " by Dnawrkshp" + ((IntPtr.Size == 8) ? " (64 Bit)" : " (32 Bit)") + " (" + recRangeBox.Items[ind].Text + ")";

                    int roInd = int.Parse(recRangeBox.Items[ind].Tag.ToString());
                    if (ind != 0)
                    {
                        for (int xRO = 0; xRO < ind; xRO++)
                        {
                            int newInd = int.Parse(recRangeBox.Items[xRO].Tag.ToString());
                            rangeOrder[newInd]++;
                        }
                        rangeOrder[roInd] = 0;
                        SaveOptions();
                        UpdateRecRangeBox();
                    }
                }
                else
                {
                    if (MessageBox.Show(this, "Would you like to remove the reference to it?", "File Doesn't Exist!", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                    {
                        //Delete reference to file
                        int index = 0;

                        //Find index of file
                        for (index = 0; index < rangeOrder.Length; index++)
                        {
                            if (rangeImports[index] == path)
                                break;
                        }

                        int[] newRangeOrder = new int[rangeOrder.Length - 1];
                        string[] newRangeImports = new string[newRangeOrder.Length];

                        int y = 0;
                        for (int x = 0; x < rangeOrder.Length; x++)
                        {
                            if (x != index)
                            {
                                newRangeOrder[y] = rangeOrder[x];
                                newRangeImports[y] = rangeImports[x];
                                if (y >= rangeOrder[index])
                                    newRangeOrder[y]--;
                                y++;
                            }
                        }

                        rangeOrder = newRangeOrder;
                        rangeImports = newRangeImports;
                        SaveOptions();
                        recRangeBox.Items.RemoveAt(ind);
                    }
                }
                
            }
        }

        /* Sorts the recRangeBox according to the rangeOrder */
        void UpdateRecRangeBox()
        {
            recRangeBox.Items.Clear();
            foreach (int val in rangeOrder)
                recRangeBox.Items.Add("");

            for (int impRan = 0; impRan < rangeOrder.Length; impRan++)
            {
                string str = rangeImports[impRan];
                if (str != "")
                {
                    System.IO.FileInfo fi = new System.IO.FileInfo(str);
                    ListViewItem lvi = new ListViewItem(new string[] { fi.Name });
                    lvi.Text = fi.Name;
                    lvi.Tag = impRan.ToString();
                    lvi.ToolTipText = fi.FullName;
                    lvi.BackColor = ncBackColor;
                    lvi.ForeColor = ncForeColor;
                    recRangeBox.Items[rangeOrder[impRan]] = lvi;
                }
            }
        }
    }
}
