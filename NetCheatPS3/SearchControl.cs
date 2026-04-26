using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using NetCheatPS3.Scanner;

namespace NetCheatPS3
{
    public partial class SearchControl : UserControl
    {

        #region Declarations

        public static SearchControl Instance = null;

        public SearchListView searchListView1 = new SearchListView();

        bool forceTBUpdate = false;

        private bool _isPWSVisible = true;
        public bool isPWSVisible
        {
            get { return _isPWSVisible; }
            set
            {
                _isPWSVisible = value;
                searchPWS.Visible = _isPWSVisible;
            }
        }

        private volatile bool _shouldStopSearch;
        private CheckBox littleEndianCB;
        private CheckBox noNegativeCB;
        private CheckBox cleanFloatCB;
        private CheckBox noZeroCB;
        private bool activeScanUsesNewEngine = false;
        private bool activeScanLittleEndian = false;
        private string activeSnapshotPath = null;
        private int activeSnapshotTypeIndex = -1;
        private int activeSnapshotByteSize = 0;
        private long activeSnapshotResultCount = 0;

        private const int MaxVisibleSnapshotResults = 10000;
        private bool activeFilterNoNegative = true;
        private bool activeFilterCleanFloat = true;
        private bool activeFilterNoZero = true;

        private const double CleanFloatMinAbs = 0.0001;
        private const double CleanFloatMaxAbs = 10000000.0;

        int lastTypeIndex = -1;
        int lastSearchIndex = -1;

        private bool _isInitialScan = true;
        public bool isInitialScan
        {
            get { return _isInitialScan; }
            set
            {

                _isInitialScan = value;

                if (IsHandleCreated)
                {
                    Invoke((MethodInvoker)delegate
                    {
                        if (_isInitialScan)
                        {
                            nextSearchMem.Enabled = false;
                            startAddrTB.Enabled = true;
                            stopAddrTB.Enabled = true;
                            searchTypeBox.Enabled = true;

                            searchListView1.isUsingListA = true;
                            searchListView1.listB.Clear();
                            searchListView1.ClearItems();
                        }
                        else
                        {
                            nextSearchMem.Enabled = true;
                            startAddrTB.Enabled = false;
                            stopAddrTB.Enabled = false;
                            searchTypeBox.Enabled = false;
                        }
                        ResetSearchCompBox();
                    });
                }
            }
        }

        public delegate void InitialSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args);
        public delegate void NextSearch(SearchListView.SearchListViewItem[] old, string[] args);
        public struct ncSearcher
        {
            public InitialSearch InitialSearch;         //Function that searches initially through the memory
            public NextSearch NextSearch;               //Function that search through already searched results
            public SearchType Type;                     //Whether this can only be Initial, Next, or Both
            public string Name;                         //Name of the search
            public string[] Args;                       //Names of arguments passed when searching
            public string[] Exceptions;                 //Types that are incompatible (array of names)
            public string[] TypeColumnOverride;         //If length of 4, overrides types column names
            public ParseItemToListString ItemToLString; //If not null, overrides types' convert item to string[] for search results box
            public ToSListViewItem ItemToString;        //If not null, overrides types' convert item to string (code)
        }

        public delegate string ByteAToString(byte[] val);
        public delegate byte[] StringToByteA(string val);
        public delegate string ToSListViewItem(SearchListView.SearchListViewItem item); 
        public delegate SearchListView.SearchListViewItem SearchToItem(ulong addr, byte[] newVal, byte[] oldVal, int typeIndex, uint misc = 0);
        public delegate string CheckboxConvert(string val, bool state);
        public delegate string[] ParseItemToListString(SearchListView.SearchListViewItem item);
        public delegate void TypeInitialize(string arg, int typeIndex);
        public delegate bool isValueValid(string[] args, out string error);
        public struct ncSearchType
        {
            public string Name;                         //Name of the search type
            public int ByteSize;                        //Value to increment the search by every loop
            public string[] ListColumnNames;            //Names of each column in the search results list view
            public ByteAToString BAToString;            //Converts the byte array into a results string
            public StringToByteA ToByteArray;           //Converts the comparison value into a byte array
            public ToSListViewItem ItemToString;        //Converts the item into a string
            public SearchToItem ToItem;                 //Converts search results into a SearchListViewItem
            public CheckboxConvert CheckboxConvert;     //Converts the value from x to y when the value's CBox is checked
            public ParseItemToListString ItemToLString; //Converts the item into a string array used by the list view
            public string CheckboxName;                 //Name of the checkbox
            public string DefaultValue;                 //Default value
            public bool ignoreAlignment;                //Whether to increase the search counter by 1 or by ByteSize
            public TypeInitialize Initialize;           //Called when the search begins, good for size declarations (X bytes)
            public isValueValid areArgsValid;           //Returns true if the args are valid, false otherwise and stores error in error string
        }

        public enum SearchType : int
        {
            InitialSearchOnly = 0,
            NextSearchOnly = 1,
            Both = 2
        }

        public static List<ncSearcher> SearchComparisons = new List<ncSearcher>();
        public static List<ncSearchType> SearchTypes = new List<ncSearchType>();
        List<SearchValue> SearchArgs = new List<SearchValue>();

        string[] Joker_ButtonChecks = new string[] { "X", "Square", "Circle", "Triangle", "L1", "R1", "L2", "R2", "Up", "Right", "Down", "Left" };

        #endregion

        #region Search Args and Types Setting

        private void RemoveSearchArgs()
        {
            if (SearchArgs == null)
                SearchArgs = new List<SearchValue>();

            foreach (SearchValue a in SearchArgs)
                Controls.Remove(a);
            SearchArgs.Clear();
        }

        private void ResetSearchTypes()
        {
            SimplifySearchTypes();

            searchTypeBox.Items.Clear();

            foreach (ncSearchType nc in SearchTypes)
                searchTypeBox.Items.Add(nc.Name);
        }

        private void RemoveSearchTypes(string[] types)
        {
            if (types == null)
                return;

            foreach (string str in types)
                RemoveSearchType(str);
        }

        private void RemoveSearchType(string name)
        {
            if (searchTypeBox.Items.Contains(name))
                searchTypeBox.Items.Remove(name);
        }

        #endregion




        #region GUI Events

        public SearchControl()
        {
            InitializeComponent();

            
            this.Load += delegate { InitializeModernSearchUi(); };Instance = this;

            littleEndianCB = new CheckBox();
            littleEndianCB.AutoSize = true;
            littleEndianCB.Text = "Little Endian";
            littleEndianCB.Checked = false;
            littleEndianCB.BackColor = BackColor;
            littleEndianCB.ForeColor = ForeColor;
            Controls.Add(littleEndianCB);

            noNegativeCB = new CheckBox();
            noNegativeCB.AutoSize = true;
            noNegativeCB.Text = "No Negative";
            noNegativeCB.Checked = true;
            noNegativeCB.BackColor = BackColor;
            noNegativeCB.ForeColor = ForeColor;
            Controls.Add(noNegativeCB);

            cleanFloatCB = new CheckBox();
            cleanFloatCB.AutoSize = true;
            cleanFloatCB.Text = "Clean Float";
            cleanFloatCB.Checked = true;
            cleanFloatCB.BackColor = BackColor;
            cleanFloatCB.ForeColor = ForeColor;
            Controls.Add(cleanFloatCB);

            noZeroCB = new CheckBox();
            noZeroCB.AutoSize = true;
            noZeroCB.Text = "No Zero";
            noZeroCB.Checked = true;
            noZeroCB.BackColor = BackColor;
            noZeroCB.ForeColor = ForeColor;
            Controls.Add(noZeroCB);

            searchListView1.Location = new Point(3, 211);
            searchListView1.Size = new Size(484, 180);
            searchListView1.SetCMenuStrip(searchListView1.contextMenuStrip1);
            searchListView1.SetParseItem(searchListView1.ParseItem);
            searchListView1.parentControl = this;
            this.Controls.Add(searchListView1);
        }

        private void SearchControl_Load(object sender, EventArgs e)
        {
            if (IntPtr.Size == 8)
            {
                startAddrTB.Text = "0000000000010000";
                stopAddrTB.Text = "0000000000020000";
            }

            LoadSearch();
            SimplifySearchComparisonModes();
        }

        private void searchNameBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            
            
            if (suppressSearchModeEvents ||
                searchNameBox == null ||
                searchNameBox.SelectedIndex < 0 ||
                searchNameBox.SelectedIndex >= searchNameBox.Items.Count ||
                searchNameBox.SelectedItem == null ||
                searchTypeBox == null ||
                searchTypeBox.SelectedItem == null)
                return;
if (suppressSearchModeEvents || searchNameBox == null || searchNameBox.SelectedIndex < 0 || searchNameBox.SelectedIndex >= searchNameBox.Items.Count)
                return;
if (lastSearchIndex == searchNameBox.SelectedIndex && !forceTBUpdate)
                return;

            string[] searchArgsOldValue = new string[SearchArgs.Count];
            bool[] searchArgsOldCheck = new bool[SearchArgs.Count];
            for (int sAOV = 0; sAOV < searchArgsOldValue.Length; sAOV++)
            {
                searchArgsOldValue[sAOV] = SearchArgs[sAOV].getValue();
                searchArgsOldCheck[sAOV] = SearchArgs[sAOV].GetState();
            }

            RemoveSearchArgs();
            ncSearcher searcher = SearchComparisons.Where(ns => ns.Name == searchNameBox.Items[searchNameBox.SelectedIndex].ToString()).FirstOrDefault();
            ncSearchType type = SearchTypes.Where(st => st.Name == searchTypeBox.SelectedItem.ToString()).FirstOrDefault(); //SearchTypes[searchTypeBox.SelectedIndex];
            int yOff = 5;

            ResetSearchTypes();
            RemoveSearchTypes(searcher.Exceptions);
            if (searcher.Exceptions.Contains(type.Name))
                lastTypeIndex = 0;
            else
                lastTypeIndex = searchTypeBox.Items.IndexOf(type.Name);
            if (lastTypeIndex >= searchTypeBox.Items.Count)
                lastTypeIndex = 0;
            searchTypeBox.SelectedIndex = lastTypeIndex;

            if (searcher.Args != null)
            {
                int cnt = 0;
                foreach (string str in searcher.Args)
                {
                    SearchValue a = new SearchValue();

                    string def = type.DefaultValue;
                    bool state = true;
                    if (cnt < searchArgsOldValue.Length && searchArgsOldValue[cnt] != null)
                    {
                        def = searchArgsOldValue[cnt];
                        state = searchArgsOldCheck[cnt];
                    }

                    a.SetSValue(str, def, type.CheckboxName, true, state, type.CheckboxConvert);
                    a.Location = new Point(5, yOff);
                    a.Width = Width - 5;
                    a.Back = BackColor;
                    a.Fore = ForeColor;

                    SearchArgs.Add(a);
                    Controls.Add(a);
                    yOff += a.Height + 5;
                    cnt++;
                }
            }

            if (searcher.TypeColumnOverride != null && searcher.TypeColumnOverride.Length == 4)
                searchListView1.SetColumnNames(searcher.TypeColumnOverride);
            else
                searchListView1.SetColumnNames(type.ListColumnNames);
            lastSearchIndex = searchNameBox.SelectedIndex;
            SearchControl_Resize(null, null);
        }

        private void searchTypeBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (searchTypeBox.SelectedIndex < 0 || searchNameBox.SelectedIndex < 0 || lastTypeIndex == searchTypeBox.SelectedIndex)
                return;

            RemoveSearchArgs();
            ncSearcher searcher = SearchComparisons.Where(ns => ns.Name == searchNameBox.Items[searchNameBox.SelectedIndex].ToString()).FirstOrDefault();
            //ncSearchType type = SearchTypes[searchTypeBox.SelectedIndex];
            ncSearchType type = SearchTypes.Where(ns => ns.Name == searchTypeBox.Items[searchTypeBox.SelectedIndex].ToString()).FirstOrDefault();
            int yOff = 5;

            if (searcher.TypeColumnOverride != null && searcher.TypeColumnOverride.Length == 4)
                searchListView1.SetColumnNames(searcher.TypeColumnOverride);
            else
                searchListView1.SetColumnNames(type.ListColumnNames);

            if (searcher.Args != null)
            {
                foreach (string str in searcher.Args)
                {
                    SearchValue a = new SearchValue();
                    a.SetSValue(str, type.DefaultValue, type.CheckboxName, true, true, type.CheckboxConvert);
                    a.Location = new Point(5, yOff);
                    a.Width = Width - 5;
                    a.Back = BackColor;
                    a.Fore = ForeColor;

                    SearchArgs.Add(a);
                    Controls.Add(a);
                    yOff += a.Height + 5;
                }
            }

            lastTypeIndex = searchTypeBox.SelectedIndex;
            SearchControl_Resize(null, null);
        }

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

        #endregion

    }
}
