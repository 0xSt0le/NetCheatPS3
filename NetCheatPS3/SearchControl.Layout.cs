using System;
using System.Drawing;

namespace NetCheatPS3
{
    public partial class SearchControl
    {
        private void SearchControl_Resize(object sender, EventArgs e)
        {
            int yOff = 5;
            int[] savedLoc = new int[3];

            foreach (SearchValue sv in SearchArgs)
            {
                sv.Location = new Point(5, yOff);
                sv.Width = Width - 10;

                yOff += sv.Height + 5;
            }

            label1.Location = new Point(5, yOff);
            searchNameBox.Location = new Point(label1.Location.X + label1.Width + 5, yOff);
            savedLoc[0] = searchNameBox.Location.X;
            label3.Location = new Point(searchNameBox.Location.X + searchNameBox.Width + 10, yOff);
            savedLoc[1] = label3.Location.X;
            startAddrTB.Location = new Point(label3.Location.X + label3.Width + 5, yOff);
            savedLoc[2] = startAddrTB.Location.X;
            yOff += startAddrTB.Height + 5;

            label2.Location = new Point(5, yOff);
            searchTypeBox.Location = new Point(savedLoc[0], yOff); //new Point(label2.Location.X + label2.Width + 5, yOff);
            label4.Location = new Point(savedLoc[1], yOff); //new Point(searchTypeBox.Location.X + searchTypeBox.Width + 10, yOff);
            stopAddrTB.Location = new Point(savedLoc[2], yOff); //new Point(label4.Location.X + label4.Width + 5, yOff);
            yOff += stopAddrTB.Height + 5;

            dumpMem.Location = new Point(5, yOff);
            saveScan.Location = new Point(dumpMem.Location.X + dumpMem.Width + 5, yOff);
            loadScan.Location = new Point(saveScan.Location.X + saveScan.Width + 5, yOff);
            searchPWS.Location = new Point(loadScan.Location.X + loadScan.Width + 5, yOff + 3);

            if (littleEndianCB != null)
            {
                littleEndianCB.Location = new Point(searchPWS.Location.X + searchPWS.Width + 12, yOff + 3);
                littleEndianCB.BackColor = BackColor;
                littleEndianCB.ForeColor = ForeColor;
            }

            if (noNegativeCB != null)
            {
                noNegativeCB.Location = new Point(littleEndianCB.Location.X + littleEndianCB.Width + 12, yOff + 3);
                noNegativeCB.BackColor = BackColor;
                noNegativeCB.ForeColor = ForeColor;
            }

            if (cleanFloatCB != null)
            {
                cleanFloatCB.Location = new Point(noNegativeCB.Location.X + noNegativeCB.Width + 12, yOff + 3);
                cleanFloatCB.BackColor = BackColor;
                cleanFloatCB.ForeColor = ForeColor;
            }

            if (noZeroCB != null)
            {
                noZeroCB.Location = new Point(cleanFloatCB.Location.X + cleanFloatCB.Width + 12, yOff + 3);
                noZeroCB.BackColor = BackColor;
                noZeroCB.ForeColor = ForeColor;
            }

            yOff += searchPWS.Height + 8;

            searchMemory.Location = new Point(5, yOff);
            nextSearchMem.Location = new Point(Width - nextSearchMem.Width - 5, yOff);
            refreshFromMem.Location = new Point(searchMemory.Location.X + searchMemory.Width + 5, yOff);
            refreshFromMem.Width = Width - 10 - (refreshFromMem.Location.X + nextSearchMem.Width);
            yOff += refreshFromMem.Height + 5;

            progBar.Location = new Point(5, yOff);
            progBar.Width = Width - 10;
            yOff += progBar.Height + 5;

            searchListView1.Location = new Point(5, yOff);
            searchListView1.Width = Width - 10;
            searchListView1.Height = Height - yOff;
        }
    }
}