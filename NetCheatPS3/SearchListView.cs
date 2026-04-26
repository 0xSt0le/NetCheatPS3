using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace NetCheatPS3
{
    public partial class SearchListView : UserControl
    {
        ContextMenuStrip cms = null;

        public delegate string[] ParseItemFunction(SearchListViewItem item, int ind);
        public ParseItemFunction ParseListViewItem;
        public bool multiSelect = true;
        public SearchControl parentControl = null;

        public SearchListView()
        {
            InitializeComponent();
            InitializeModernResultUi();
            this.MouseWheel += new MouseEventHandler(this_MouseWheel);
        }

        public void SetCMenuStrip(ContextMenuStrip c)
        {
            cms = c;
        }

        public void SetParseItem(ParseItemFunction p)
        {
            ParseListViewItem = p;
        }

        public List<SearchListViewItem> listA = new List<SearchListViewItem>();
        public List<SearchListViewItem> listB = new List<SearchListViewItem>();

        public bool isUsingListA = true;
        public List<SearchListViewItem> a
        {
            get
            {
                if (isUsingListA)
                    return listA;
                else
                    return listB;
            }
            set
            {
                if (isUsingListA)
                    listA = value;
                else
                    listB = value;
            }
        }

        private List<SearchListViewItem> b = new List<SearchListViewItem>();

        #region Public Variables

        [Serializable]
        public struct SearchListViewItem
        {
            public byte[] newVal;
            public byte[] oldVal;
            public uint addr;
            public int align;
            public bool refresh;
            public uint misc;
        };

        private float _ItemHeight = 16f;
        [Description("Height of each drawn item"), Category("Appearance"), DefaultValue(16f), Browsable(true)]
        public float ItemHeight
        {
            get { return _ItemHeight; }
            set { _ItemHeight = value; }
        }

        private Brush _TextBrush = Brushes.Black;
        [Description("Color of the drawn text"), Category("Appearance"), Browsable(true)]
        public Brush TextBrush
        {
            get { return _TextBrush; }
            set { _TextBrush = value; }
        }

        private Font _ItemFont;
        [Description("Font used when drawing the items"), Category("Appearance"), Browsable(true)]
        public Font ItemFont
        {
            get { return _ItemFont; }
            set { _ItemFont = value; }
        }

        private Brush _SelectionColor = Brushes.LightBlue;
        [Description("Color of the selected item"), Category("Appearance"), Browsable(true)]
        public Brush SelectionColor
        {
            get { return _SelectionColor; }
            set { _SelectionColor = value; }
        }

        private List<int> _SelectedIndices = new List<int>();
        public List<int> SelectedIndices
        {
            get { return _SelectedIndices; }
            set
            {
                _SelectedIndices = value;

                if (tempTB != null)
                {
                    printBox.Controls.Remove(tempTB);
                    tempTB = null;
                }

                printBox.Refresh();
            }
        }

        public int MaxItemsPerPage
        {
            get
            {
                return (int)(printBox.Height / ItemHeight);
            }
        }

        public int TotalCount
        {
            get
            {
                return a.Count + b.Count;
            }
        }

        public int MaxItemSize = 0x1000;

        #endregion

        #region Private Variables

        private bool isShiftDown = false;
        private bool isCtrlDown = false;

        private int vertCaretMult = 1;

        private bool isVertSBarVisible
        {
            get { return vertSBar.Visible; }
            set
            {
                vertSBar.Visible = value;
                SearchListView_Resize(null, null);
            }
        }

        #endregion

        #region Private Functions

        private void this_MouseWheel(object sender, MouseEventArgs e)
        {
            int newVal = vertSBar.Value + ((e.Delta > 0) ? -1 : 1);
            if (newVal < vertSBar.Maximum && newVal > 0)
                vertSBar.Value = newVal;
        }

        private void SearchListView_Load(object sender, EventArgs e)
        {
            if (TextBrush == null)
                TextBrush = Brushes.Black;
            if (ItemFont == null)
                ItemFont = Font;

            addrLabel.PreviewKeyDown += new PreviewKeyDownEventHandler(printBox_PreviewKeyDown);
            hexValLabel.PreviewKeyDown += new PreviewKeyDownEventHandler(printBox_PreviewKeyDown);
            decValLabel.PreviewKeyDown += new PreviewKeyDownEventHandler(printBox_PreviewKeyDown);
            alignLabel.PreviewKeyDown += new PreviewKeyDownEventHandler(printBox_PreviewKeyDown);
            vertSBar.PreviewKeyDown += new PreviewKeyDownEventHandler(printBox_PreviewKeyDown);
        }

        private void printBox_Paint(object sender, PaintEventArgs e)
        {
            int start = 0;
            if (isVertSBarVisible)
            {
                if (vertSBar.Value < 0)
                    vertSBar.Value = 0;
                start = vertSBar.Value / vertCaretMult;
            }


            int x = 0;
            for (int y = start; y < TotalCount; y++)
            {
                float yOff = (float)x * ItemHeight;
                if (yOff > printBox.Height)
                    break;

                if (SelectedIndices.Contains(y))
                {
                    Rectangle rect = new Rectangle(0, (int)yOff, printBox.Width, (int)ItemHeight);
                    e.Graphics.FillRectangle(SelectionColor, rect);
                }

                string[] vals = ParseListViewItem(GetItemAtIndex(y), y);

                TextBrush = new SolidBrush(ForeColor);
                RectangleF r = new RectangleF(0f, yOff, addrLabel.Width, ItemHeight);
                DrawStringCentered(vals[0], TextBrush, r, e);

                r.X = addrLabel.Width;
                DrawStringCentered(vals[1], TextBrush, r, e);
                r.X = addrLabel.Width * 2;
                DrawStringCentered(vals[2], TextBrush, r, e);
                r.X = addrLabel.Width * 3;
                DrawStringCentered(vals[3], TextBrush, r, e);

                x++;
            }
        }

        public string[] ParseItem(SearchListViewItem item, int ind)
        {
            SearchControl.ncSearchType type = SearchControl.SearchTypes[item.align];
            SearchControl.ncSearcher searcher = new SearchControl.ncSearcher();

            try
            {
                if (SearchControl.Instance != null &&
                    SearchControl.Instance.searchNameBox != null &&
                    SearchControl.Instance.searchNameBox.SelectedIndex >= 0 &&
                    SearchControl.Instance.searchNameBox.SelectedIndex < SearchControl.Instance.searchNameBox.Items.Count)
                {
                    string selectedSearchName = SearchControl.Instance.searchNameBox.Items[SearchControl.Instance.searchNameBox.SelectedIndex].ToString();
                    searcher = SearchControl.SearchComparisons.Where(sc => sc.Name == selectedSearchName).FirstOrDefault();
                }
            }
            catch
            {
                searcher = new SearchControl.ncSearcher();
            }
            if (item.refresh)
            {
                byte[] newVal = new byte[type.ByteSize];
                Form1.apiGetMem(item.addr, ref newVal);
                newVal = NormalizeRawMemoryForDisplay(newVal);
                item.oldVal = item.newVal;
                item.newVal = newVal;
                item.refresh = false;
                SetItemAtIndex(item, ind);
            }

            if (searcher.ItemToLString != null)
                return searcher.ItemToLString(item);
            else
                return type.ItemToLString(item);
        }

        int charInsertIndex = 0;
        private void DrawStringCentered(string str, Brush b, RectangleF r, PaintEventArgs e)
        {
            SizeF size = e.Graphics.MeasureString(str, ItemFont);
            if (size.Width > r.Width)
            {
                try
                {
                    if (charInsertIndex <= 0)
                    {
                        charInsertIndex = (int)(r.Width / ItemFont.Size) - 2;
                    }
                    str = str.Insert(charInsertIndex, "...");


                    size.Width = r.Width;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            RectangleF rect = new RectangleF(
                (r.X + (r.Width / 2f)) - (size.Width / 2f),
                (r.Y + (r.Height / 2f)) - (size.Height / 2f),
                size.Width,
                size.Height);

            e.Graphics.DrawString(str, ItemFont, b, rect);
        }

        private void SearchListView_Resize(object sender, EventArgs e)
        {
            int labWidth = Width / 4;
            if (isVertSBarVisible)
                labWidth = (Width - 20) / 4;

            addrLabel.Width = labWidth;
            hexValLabel.Width = labWidth;
            decValLabel.Width = labWidth;
            alignLabel.Width = labWidth;

            addrLabel.Location = new Point(0, 0);
            hexValLabel.Location = new Point(labWidth, 0);
            decValLabel.Location = new Point(labWidth * 2, 0);
            alignLabel.Location = new Point(labWidth * 3, 0);

            vertSBar.Location = new Point(Width - 20, 0);
            vertSBar.Height = Height;
            Controls.SetChildIndex(vertSBar, 0);

            printBox.Location = new Point(0, addrLabel.Height);
            printBox.Width = labWidth * 4;
            printBox.Height = Height - addrLabel.Height;
        }

        private void vertSBar_ValueChanged(object sender, EventArgs e)
        {
            printBox.Refresh();

            if (tempTB != null)
            {
                printBox.Controls.Remove(tempTB);
                tempTB = null;
            }
        }

        private void printBox_Click(object sender, EventArgs e)
        {
            if (isMouse2Down)
                return;

            Point pos = printBox.PointToClient(Cursor.Position);
            int index = (int)(pos.Y / ItemHeight) + (vertSBar.Visible ? vertSBar.Value : 0);

            isCtrlDown = (Control.ModifierKeys & Keys.Control) != 0;
            isShiftDown = (Control.ModifierKeys & Keys.Shift) != 0;

            if (index >= TotalCount)
            {
                if (!isCtrlDown && !isShiftDown)
                {
                    ClearSelectedIndices();
                }
            }
            else
            {
                if (isCtrlDown && multiSelect)
                {
                    AddSelectedIndex(index);
                }
                else if (isShiftDown && multiSelect)
                {
                    int oldIndex = SelectedIndices[SelectedIndices.Count - 1];
                    AddSelectedIndexRange(oldIndex, index);
                }
                else
                {
                    if (index < TotalCount)
                        SetSelectedIndex(index);
                }
            }

            printBox.Focus();
        }

        TextBox tempTB;
        bool didPressCtrlC = false;
        private void printBox_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            printBox.Enabled = false;
            int max = MaxItemsPerPage;

            if (SelectedIndices.Count == 0)
                return;


            if (e.KeyCode == Keys.Up)
            {
                if (SelectedIndices[0] > 0)
                    SetSelectedIndex(SelectedIndices[0] - 1);
                if (vertSBar.Value > SelectedIndices[0] || (vertSBar.Value + max) < SelectedIndices[0])
                    vertSBar.Value = SelectedIndices[0];
            }
            else if (e.KeyCode == Keys.Down)
            {
                if (SelectedIndices[0] < (TotalCount - 1) && SelectedIndices[0] >= 0)
                    SetSelectedIndex(SelectedIndices[0] + 1);
                if (vertSBar.Value > SelectedIndices[0] || (vertSBar.Value + max - 1) < SelectedIndices[0])
                {
                    int newValue = SelectedIndices[0] - max + 1;
                    if (newValue <= vertSBar.Maximum)
                        vertSBar.Value = newValue;
                }
            }
            else if (e.KeyCode == Keys.C && e.Control)
            {
                if (!didPressCtrlC)
                {
                    didPressCtrlC = true;
                    CopySelection();
                }
            }
            else if (e.KeyCode == Keys.Delete)
            {
                if (SelectedIndices != null && SelectedIndices.Count > 0)
                    deleteToolStripMenuItem_Click(null, null);
            }
            else
            {
                didPressCtrlC = false;
            }

            isShiftDown = e.Shift;
            isCtrlDown = e.Control;

            printBox.Enabled = true;
            printBox.Focus();
            e.IsInputKey = true;
        }

        void CopySelection()
        {
            SearchControl.ncSearcher searcher = new SearchControl.ncSearcher();

            try
            {
                if (SearchControl.Instance != null &&
                    SearchControl.Instance.searchNameBox != null &&
                    SearchControl.Instance.searchNameBox.SelectedIndex >= 0 &&
                    SearchControl.Instance.searchNameBox.SelectedIndex < SearchControl.Instance.searchNameBox.Items.Count)
                {
                    string selectedSearchName = SearchControl.Instance.searchNameBox.Items[SearchControl.Instance.searchNameBox.SelectedIndex].ToString();
                    searcher = SearchControl.SearchComparisons.Where(sc => sc.Name == selectedSearchName).FirstOrDefault();
                }
            }
            catch
            {
                searcher = new SearchControl.ncSearcher();
            }
            string[] codes = new string[SelectedIndices.Count];
            int codeIndex = 0;
            foreach (int x in SelectedIndices)
            {
                SearchListViewItem item = GetItemAtIndex(x);
                if (item.newVal != null)
                {
                    SearchControl.ncSearchType type = SearchControl.SearchTypes[item.align];

                    if (searcher.ItemToString != null)
                        codes[codeIndex] = searcher.ItemToString(item) + Environment.NewLine;
                    else
                        codes[codeIndex] = type.ItemToString(item) + Environment.NewLine;
                    codeIndex++;
                }
            }

            Clipboard.SetDataObject(new DataObject(DataFormats.Text, (String.Join("", codes, 0, codeIndex)).Replace("\0", "")));
        }

        static bool isMouse2Down = false;
        private void printBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
                isMouse2Down = true;
            else
                isMouse2Down = false;
        }

        private void printBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (isMouse2Down && cms != null)
            {
                cms.ForeColor = ForeColor;
                cms.BackColor = BackColor;
                cms.Show(printBox, e.Location);
            }

            isMouse2Down = false;
        }

        private void printBox_MouseClick(object sender, MouseEventArgs e)
        {
            isShiftDown = false;
            isCtrlDown = false;
        }

        #endregion

        #region Public Functions

        public void BeginUpdate()
        {
        }

        public void EndUpdate()
        {
        }

        public void SetColumnNames(string[] names)
        {
            if (names == null || names.Length != 4)
                return;

            addrLabel.Text = names[0];
            hexValLabel.Text = names[1];
            decValLabel.Text = names[2];
            alignLabel.Text = names[3];
        }

        public void RemoveItem(SearchListViewItem item)
        {
            a.Remove(item);
            printBox.Refresh();
        }

        public void RemoveItemAt(int index)
        {
            a.RemoveAt(index);

            printBox.Refresh();
        }

        public void ClearItems()
        {
            if (tempTB != null)
            {
                printBox.Controls.Remove(tempTB);
                tempTB = null;
            }

            b.Clear();
            a.Clear();
            SelectedIndices.Clear();
            isVertSBarVisible = false;
            printBox.Refresh();
        }

        public SearchListViewItem[] CloneItems()
        {
            return GetItemsArray();
        }

        public void AddItemsFromList()
        {
            AddItemRange(b);
            b.Clear();
        }

        public void AddItem(SearchListViewItem item)
        {
            CalculateSBarMax();
            b.Add(item);
            if (b.Count >= MaxItemSize)
                AddItemsFromList();
        }

        public void CalculateSBarMax()
        {
            int totalSize = a.Count;
            int max = (totalSize) - MaxItemsPerPage;
            if (max > 0)
            {
                vertSBar.Maximum = max * vertCaretMult;
                vertSBar.LargeChange = vertCaretMult;
                isVertSBarVisible = true;
                vertSBar.Refresh();
                vertSBar.Update();
            }
            else
            {
                isVertSBarVisible = false;
            }
        }

        public void AddItemRange(List<SearchListViewItem> items)
        {
            a.AddRange(items);
            CalculateSBarMax();
            printBox.Refresh();
        }

        private void addi(SearchListViewItem item)
        {
            a.Add(item);
        }

        public void AddItemRange(SearchListViewItem[] items)
        {
            foreach (SearchListViewItem i in items)
            {
                addi(i);
            }
            CalculateSBarMax();
            printBox.Refresh();
        }

        public void SetItemAtIndex(SearchListViewItem item, int ind)
        {
            if (ind < 0 || ind >= (a.Count + b.Count))
                return;

            if (ind >= a.Count)
            {
                b[ind - a.Count] = item;
                return;
            }

            a[ind] = item;
        }

        public SearchListViewItem GetItemAtIndex(int ind)
        {
            if (ind < 0 || ind >= (a.Count + b.Count))
                return new SearchListViewItem();

            if (ind >= a.Count)
            {
                return b[ind - a.Count];
            }

            return a[ind];
        }

        public SearchListViewItem[] GetItemsArray()
        {
            if (b.Count > 0)
                AddItemsFromList();

            return a.ToArray();
        }

        public void SetSelectedIndex(int ind)
        {
            List<int> a = new List<int>();
            a.Add(ind);
            SelectedIndices = a;
        }

        public void AddSelectedIndex(int ind)
        {
            List<int> a = SelectedIndices;
            if (a.Contains(ind))
                a.Remove(ind);
            else
                a.Add(ind);
            SelectedIndices = a;
        }

        public void AddSelectedIndexRange(int start, int stop)
        {
            List<int> a = SelectedIndices;

            if (start < stop)
                for (int x = start; x <= stop; x++)
                {
                    a.Add(x);
                }
            else
                for (int x = start; x >= stop; x--)
                {
                    a.Add(x);
                }
            SelectedIndices = a;
        }

        public void ClearSelectedIndices()
        {
            List<int> a = new List<int>();
            SelectedIndices = a;
        }

        #endregion

        #region Context Menu Strip 

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (SelectedIndices == null || SelectedIndices.Count <= 0)
                return;

            int s = SelectedIndices[0];

            List<SearchListViewItem> items = new List<SearchListViewItem>();
            foreach (int x in SelectedIndices)
            {
                items.Add(GetItemAtIndex(x));
            }

            foreach (SearchListViewItem item in items)
                RemoveItem(item);

            vertSBar.Maximum = a.Count - MaxItemsPerPage;
            if (vertSBar.Maximum <= 0)
            {
                vertSBar.Maximum = 0;
                isVertSBarVisible = false;
            }
            else if (vertSBar.Value >= vertSBar.Maximum)
                vertSBar.Value = vertSBar.Maximum - 1;

            parentControl.SetProgBarText("Results: " + a.Count.ToString("N0"));

            SelectedIndices.Clear();

            if (s >= TotalCount)
                s = TotalCount - 1;

            SelectedIndices.Add(s);
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectedIndices.Clear();
            for (int x = 0; x < TotalCount; x++)
                SelectedIndices.Add(x);
            printBox.Refresh();
        }

        private void refreshFromPS3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!EnsureConnectedAndAttachedForResultAction("Refresh From PS3"))
                return;

            SearchControl.Instance.RefreshResults(1);
        }

        public void UpdateItemAtIndex(int ind)
        {
        }

        #endregion

        private void vertSBar_VisibleChanged(object sender, EventArgs e)
        {
            if (!vertSBar.Visible)
                vertSBar.Value = 0;
        }

    }
}
