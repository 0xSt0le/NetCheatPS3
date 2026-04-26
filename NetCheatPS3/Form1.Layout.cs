using System;
using System.Drawing;

namespace NetCheatPS3
{
    public partial class Form1
    {
        private void Form1_Resize(object sender, EventArgs e)
        {
            int ratio = 0;

            //Tab Control
            {
                TabCon.Size = new Size(this.Width - 40, this.Height - (501 - 393));
                int tHeight = TabCon.SelectedTab.Height, tWidth = TabCon.SelectedTab.Width;
                if (tHeight == 0 || tWidth == 0)
                    return;
                //Tab Page: Codes
                {
                    ratio = (tHeight - (367 - 298)) - cbList.Height;
                    cbList.Height = (tHeight - (367 - 298));
                    cbAdd.Top += ratio;
                    cbRemove.Top += ratio;
                    cbImport.Top += ratio;
                    cbSave.Top += ratio;
                    cbSaveAll.Top += ratio;
                    cbSaveAs.Top += ratio;

                    label5.Width = (tWidth - (453 - 225));
                    cbName.Width = (tWidth - (453 - 225));
                    cbState.Width = (tWidth - (453 - 225));
                    ratio = (tHeight - (367 - 225)) - cbCodes.Height;
                    cbCodes.Height = (tHeight - (367 - 225));
                    cbCodes.Width = (tWidth - (453 - 225));
                    cbWrite.Top += ratio;
                    cbWrite.Width = (tWidth - (453 - 225));
                    cbBManager.Top += ratio;
                    cbResetWrite.Top += ratio;
                    cbBackupWrite.Top += ratio;

                    ratio = (tWidth - cbBManager.Left - 18) / 3;
                    cbBManager.Width = ratio;
                    cbResetWrite.Width = ratio;
                    cbBackupWrite.Width = ratio;
                    cbResetWrite.Left = cbBManager.Left + cbBManager.Width + 5;
                    cbBackupWrite.Left = cbResetWrite.Left + cbResetWrite.Width + 5;
                }
                //Tab Page: Search
                {
                    searchControl1.Size = new Size(tWidth - (461 - 444), tHeight - (393 - 383));
                }
                //Tab Page: Range
                {
                    label1.Width = tWidth - 20;
                    label2.Width = tWidth - 20;
                    rangeView.Height = tHeight - 40;

                    ratio = tWidth - 235 - 8;
                    label3.Width = ratio;
                    recRangeBox.Width = ratio;
                    if (probeAddressButton != null)
                    {
                        int probeWidth = 110;
                        findRangeProgBar.Width = ratio - findRanges.Width - probeWidth - 12;
                        if (findRangeProgBar.Width < 50)
                            findRangeProgBar.Width = 50;

                        findRanges.Left = findRangeProgBar.Left + findRangeProgBar.Width + 6;

                        probeAddressButton.Width = probeWidth;
                        probeAddressButton.Height = findRanges.Height;
                        probeAddressButton.Top = findRanges.Top;
                        probeAddressButton.Left = findRanges.Left + findRanges.Width + 6;
                    }
                    else
                    {
                        findRangeProgBar.Width = ratio - 91 - 6;
                        findRanges.Left = findRangeProgBar.Left + findRangeProgBar.Width + 6;
                    }

                    ratio -= 30;
                    ratio /= 2;

                    RangeUp.Width = ratio;
                    RangeDown.Width = ratio;
                    RangeDown.Left = RangeUp.Left + ratio + 30;
                    ImportRange.Width = ratio;
                    SaveRange.Width = ratio;
                    SaveRange.Left = ImportRange.Left + ratio + 30;
                    AddRange.Width = ratio;
                    RemoveRange.Width = ratio;
                    RemoveRange.Left = AddRange.Left + ratio + 30;

                    ratio = tHeight - 200 - recRangeBox.Height;
                    recRangeBox.Height = tHeight - 200;
                    RangeUp.Top += ratio;
                    RangeDown.Top += ratio;
                    ImportRange.Top += ratio;
                    SaveRange.Top += ratio;
                    AddRange.Top += ratio;
                    RemoveRange.Top += ratio;
                }
                //Tab Page: Plugins
                {
                    pluginList.Height = tHeight - 40;
                    int pWidth = tWidth - (461 - 174);
                    if (pWidth > 250)
                        pWidth = 250;
                    pluginList.Width = pWidth;

                    descPlugName.Left = pluginList.Left + pluginList.Width + 6;
                    descPlugAuth.Left = descPlugName.Left + 22;
                    descPlugVer.Left = descPlugAuth.Left;
                    descPlugDesc.Left = descPlugName.Left + 3;
                    plugIcon.Left = descPlugName.Left;

                    int plugIconHeight = tHeight - (393 - 210);
                    plugIcon.Height = (int)(210f * ((float)plugIconHeight / 210f));
                    plugIcon.Width = (int)(266f * ((float)plugIconHeight / 210f));

                    descPlugDesc.Width = tWidth - descPlugDesc.Left;
                }
                //Tab Page: APIs
                {
                    apiList.Height = tHeight - 40;
                    int pWidth = tWidth - (461 - 174);
                    if (pWidth > 250)
                        pWidth = 250;
                    apiList.Width = pWidth;

                    descAPIName.Left = apiList.Left + apiList.Width + 6;
                    descAPIAuth.Left = descAPIName.Left + 22;
                    descAPIVer.Left = descAPIAuth.Left;
                    descAPIDesc.Left = descAPIName.Left + 3;
                    apiIcon.Left = descAPIName.Left;

                    int APIIconHeight = tHeight - (393 - 210);
                    apiIcon.Height = (int)(210f * ((float)APIIconHeight / 210f));
                    apiIcon.Width = (int)(266f * ((float)APIIconHeight / 210f));

                    descAPIDesc.Width = tWidth - descAPIDesc.Left;
                }
            }

            this.HScroll = false;
            this.VScroll = false;
            this.AutoScroll = false;
        }
    }
}
