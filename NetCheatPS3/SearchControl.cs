using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

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


        #endregion

    }
}
