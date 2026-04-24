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
            searchTypeBox.Items.Clear();
            foreach (ncSearchType nc in SearchTypes)
                searchTypeBox.Items.Add(nc.Name);
        }

        private void RemoveSearchTypes(string[] types)
        {
            foreach (string str in types)
                RemoveSearchType(str);
        }

        private void RemoveSearchType(string name)
        {
            if (searchTypeBox.Items.Contains(name))
                searchTypeBox.Items.Remove(name);
        }

        #endregion

        #region Search Thread Calls

        public void SetStatusLabel(string str)
        {
            Invoke((MethodInvoker)delegate
            {
                //statusLabel.Text = str;
                progBar.printText = str;
            });
        }

        public void ClearItems()
        {
            Invoke((MethodInvoker)delegate
            {
                searchListView1.ClearItems();
            });
        }

        public void AddResultRange(List<SearchListView.SearchListViewItem> items)
        {
            Invoke((MethodInvoker)delegate
            {
                searchListView1.AddItemRange(items);
            });
        }

        public void AddResult(SearchListView.SearchListViewItem item)
        {
            Invoke((MethodInvoker)delegate
            {
                searchListView1.AddItem(item);
            });
        }

        public void IncProgBar(int inc)
        {
            Invoke((MethodInvoker)delegate
            {
                progBar.Increment(inc);
            });
        }

        public void SetProgBar(int val)
        {
            Invoke((MethodInvoker)delegate
            {
                progBar.Value = val;
                TaskbarProgress.SetValue(this.Handle, (double)val, (double)progBar.Maximum);
            });
        }

        public void SetProgBarText(string val)
        {
            Invoke((MethodInvoker)delegate
            {
                progBar.printText = val;
                //TaskbarProgress.SetValue(this.Handle, (double)val, (double)progBar.Maximum);
            });
        }

        public void SetProgBarMax(int max)
        {
            Invoke((MethodInvoker)delegate
            {
                progBar.Maximum = max;
            });
        }

        public int GetRealDif(ulong start, ulong stop, ulong div)
        {
            int ret = 0;
            Invoke((MethodInvoker)delegate
            {
                ret = misc.ParseRealDif(start, stop, div);
            });
            return ret;
        }

        #endregion

        #region GUI Buttons

        public void ThreadInitSearch(object[] args)
        {
            bool pausedForScan = false;
            bool stopped = false;

            try
            {
                if (!codes.ConnectAndAttach(searchMemoryStopProc))
                {
                    Invoke((MethodInvoker)delegate
                    {
                        searchMemory.Text = "Initial Scan";
                        Form1.Instance.statusLabel1.Text = "Unable to connect or attach to the PS3";
                        isInitialScan = true;
                    });
                    return;
                }

                pausedForScan = (bool)args[5];
                if (pausedForScan)
                    Form1.PauseProcess();

                SetProgBar(0);
                isInitialScan = true;

                SearchControl.ncSearcher searcher = (SearchControl.ncSearcher)args[0];
                ulong start = (ulong)args[1];
                ulong stop = (ulong)args[2];
                int index = (int)args[3];
                string[] passArgs = (string[])args[4];

                System.Diagnostics.Stopwatch stopw = new System.Diagnostics.Stopwatch();
                stopw.Start();
                searcher.InitialSearch(start, stop, index, passArgs);
                stopw.Stop();
                CaptureLastScanStats("Initial Scan", stopw.ElapsedMilliseconds, start, stop);

                stopped = _shouldStopSearch;

                SetProgBar(0);

                if (pausedForScan)
                    Form1.ContinueProcess();

                Invoke((MethodInvoker)delegate
                {
                    if (stopped)
                    {
                        searchMemory.Text = "Initial Scan";
                        Form1.Instance.statusLabel1.Text = "Scan stopped";
                        isInitialScan = true;
                    }
                    else
                    {
                        searchMemory.Text = "New Scan";
                        Form1.Instance.statusLabel1.Text = "Scan took " + ((float)stopw.ElapsedMilliseconds / 1000f).ToString("0.00") + " seconds";
                        isInitialScan = false;
                    }
                });
            }
            catch (Exception ex)
            {
                CrashLogger.Log("SearchControl.ThreadInitSearch", ex);

                Invoke((MethodInvoker)delegate
                {
                    searchMemory.Text = "Initial Scan";
                    nextSearchMem.Text = "Next Scan";
                    Form1.Instance.statusLabel1.Text = "Initial scan crashed. See NetCheatPS3_crash.log";
                    isInitialScan = true;
                });
            }
            finally
            {
                try
                {
                    if (pausedForScan && Form1.connected && Form1.attached && Form1.Instance.isProcessStopped())
                        Form1.ContinueProcess();
                }
                catch (Exception ex)
                {
                    CrashLogger.Log("SearchControl.ThreadInitSearch.Finally", ex);
                }

                _shouldStopSearch = false;
                searchThread = null;
            }
        }

        void ThreadNextSearch(object[] args)
        {
            bool pausedForScan = false;
            bool stopped = false;

            try
            {
                if (!codes.ConnectAndAttach(searchMemoryStopProc))
                {
                    Invoke((MethodInvoker)delegate
                    {
                        nextSearchMem.Text = "Next Scan";
                        Form1.Instance.statusLabel1.Text = "Unable to connect or attach to the PS3";
                    });
                    return;
                }

                Invoke((MethodInvoker)delegate
                {
                    searchListView1.isUsingListA = !searchListView1.isUsingListA;
                    searchListView1.ClearItems();
                });

                pausedForScan = (bool)args[3];
                if (pausedForScan)
                    Form1.PauseProcess();

                SetProgBar(0);

                SearchControl.ncSearcher searcher = (SearchControl.ncSearcher)args[0];
                SearchListView.SearchListViewItem[] items = (SearchListView.SearchListViewItem[])args[1];

                System.Diagnostics.Stopwatch stopw = new System.Diagnostics.Stopwatch();
                stopw.Start();
                searcher.NextSearch(items, (string[])args[2]);
                stopw.Stop();
                CaptureLastScanStatsFromUi("Next Scan", stopw.ElapsedMilliseconds);

                stopped = _shouldStopSearch;

                SetProgBar(0);

                if (pausedForScan)
                    Form1.ContinueProcess();

                Invoke((MethodInvoker)delegate
                {
                    nextSearchMem.Text = "Next Scan";

                    if (stopped)
                        Form1.Instance.statusLabel1.Text = "Scan stopped";
                    else
                        Form1.Instance.statusLabel1.Text = "Scan took " + ((float)stopw.ElapsedMilliseconds / 1000f).ToString("0.00") + " seconds";
                });
            }
            catch (Exception ex)
            {
                CrashLogger.Log("SearchControl.ThreadNextSearch", ex);

                Invoke((MethodInvoker)delegate
                {
                    nextSearchMem.Text = "Next Scan";
                    Form1.Instance.statusLabel1.Text = "Next scan crashed. See NetCheatPS3_crash.log";
                });
            }
            finally
            {
                try
                {
                    if (pausedForScan && Form1.connected && Form1.attached && Form1.Instance.isProcessStopped())
                        Form1.ContinueProcess();
                }
                catch (Exception ex)
                {
                    CrashLogger.Log("SearchControl.ThreadNextSearch.Finally", ex);
                }

                _shouldStopSearch = false;
                searchThread = null;
            }
        }

        public Thread searchThread;
        private void nextSearchMem_Click(object sender, EventArgs e)
        {
            if (searchThread != null && nextSearchMem.Text == "Stop")
            {
                _shouldStopSearch = true;
                //while (_shouldStopSearch)
                //    Thread.Sleep(50);
                searchThread = null;
                return;
            }
            else if (nextSearchMem.Text == "Next Scan")
            {
                try
                {
                    if (searchThread != null)
                        searchThread = null;
                    progBar.printText = "";

                    _shouldStopSearch = false;
                    searchMemoryStopProc = Form1.Instance.isProcessStopped();
                    ulong start = Convert.ToUInt64(startAddrTB.Text, 16);
                    ulong stop = Convert.ToUInt64(stopAddrTB.Text, 16);
                    string[] args = new string[SearchArgs.Count];
                    
                    if (HasEmptyVisibleSearchArgText())
                    {
                        ShowEmptySearchArgWarning();
                        return;
                    }
for (int x = 0; x < args.Length; x++)
                        args[x] = SearchArgs[x].GetDefValue();
                    int typeIndex = 0;
                    for (typeIndex = 0; typeIndex < SearchTypes.Count; typeIndex++)
                    {
                        if (SearchTypes[typeIndex].Name == searchTypeBox.SelectedItem.ToString())
                            break;
                    }

                    string err;
                    if (!SearchTypes[typeIndex].areArgsValid(args, out err))
                    {
                        MessageBox.Show(err);
                        return;
                    }

                    ncSearcher searcher = SearchComparisons.Where(sc => sc.Name == searchNameBox.Items[searchNameBox.SelectedIndex].ToString()).FirstOrDefault();

                    //searchThread = new Thread(searcher.InitialSearch(start, stop, searchTypeBox.SelectedIndex, args));
                    //searchThread = new Thread(ThreadInitSearch(start, stop, searchTypeBox.SelectedIndex, args));

                    //progBar.printText = "Duplicating results...";
                    //SearchListView.SearchListViewItem[] items = searchListView1.CloneItems();
                    //progBar.printText = "";
                    searchThread = new Thread(() => ThreadNextSearch(new object[] { searcher, searchListView1.a.ToArray(), args, searchPWS.Checked && isPWSVisible }));
                    searchThread.IsBackground = true;
                    searchThread.Start();
                    nextSearchMem.Text = "Stop";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        static bool searchMemoryStopProc = false;
        private void searchMemory_Click(object sender, EventArgs e)
        {
            if (searchThread != null && searchMemory.Text == "Stop")
            {
                _shouldStopSearch = true;
                //while (_shouldStopSearch)
                //    Thread.Sleep(50);
                searchThread = null;
                return;
            }
            else if (searchMemory.Text == "New Scan")
            {
                searchListView1.ClearItems();
                searchMemory.Text = "Initial Scan";
                progBar.printText = "";
                forceTBUpdate = true;
                isInitialScan = true;
                forceTBUpdate = false;
            }
            else if (searchMemory.Text == "Initial Scan")
            {
                try
                {
                    if (searchThread != null)
                        searchThread = null;

                    _shouldStopSearch = false;
                    ClearActiveSnapshot();

                    activeScanUsesNewEngine = false;
                    activeScanLittleEndian = IsLittleEndianModeChecked();

                    searchMemoryStopProc = Form1.Instance.isProcessStopped();
                    ulong start = Convert.ToUInt64(startAddrTB.Text, 16);
                    ulong stop = Convert.ToUInt64(stopAddrTB.Text, 16);
                    string[] args = new string[SearchArgs.Count];
                    
                    if (HasEmptyVisibleSearchArgText())
                    {
                        ShowEmptySearchArgWarning();
                        return;
                    }
for (int x = 0; x < args.Length; x++)
                        args[x] = SearchArgs[x].GetDefValue();
                    int typeIndex = 0;
                    for (typeIndex = 0; typeIndex < SearchTypes.Count; typeIndex++)
                    {
                        if (SearchTypes[typeIndex].Name == searchTypeBox.SelectedItem.ToString())
                            break;
                    }

                    string err;
                    if (!SearchTypes[typeIndex].areArgsValid(args, out err))
                    {
                        MessageBox.Show(err);
                        return;
                    }

                    if (searchNameBox.SelectedIndex < 0)
                        searchNameBox.SelectedIndex = lastSearchIndex;
                    ncSearcher searcher = SearchComparisons.Where(sc => sc.Name == searchNameBox.Items[searchNameBox.SelectedIndex].ToString()).FirstOrDefault();

                    //searchThread = new Thread(searcher.InitialSearch(start, stop, searchTypeBox.SelectedIndex, args));
                    //searchThread = new Thread(ThreadInitSearch(start, stop, searchTypeBox.SelectedIndex, args));
                    searchThread = new Thread(() => ThreadInitSearch(new object[] { searcher, start, stop, typeIndex, args, searchPWS.Checked && isPWSVisible }));
                    searchThread.Priority = ThreadPriority.Highest;
                    searchThread.IsBackground = true;
                    searchThread.Start();
                    searchMemory.Text = "Stop";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void refreshFromMem_Click(object sender, EventArgs e)
        {
            RefreshResults(1);
        }

        public void RefreshResults(int mode)
        {
            switch (mode)
            {

                case 0:
                    searchListView1.AddItemsFromList();
                    for (int x = 0; x < searchListView1.TotalCount; x++)
                    {
                        //int itemIndex1 = x / SearchListView.MaxItemSize;
                        //int itemIndex2 = x - (itemIndex1 * SearchListView.MaxItemSize);

                        SearchListView.SearchListViewItem item = searchListView1.GetItemAtIndex(x);
                        byte[] getBytes = new byte[item.newVal.Length];
                        if (Form1.apiGetMem(item.addr, ref getBytes))
                        {
                            item.oldVal = item.newVal;
                            item.newVal = getBytes;
                            //searchListView1.a[item.addr] = item;
                            searchListView1.UpdateItemAtIndex(x);
                        }
                    }
                    break;
                case 1:
                    for (int x = 0; x < searchListView1.a.Count; x++)
                    {
                        SearchListView.SearchListViewItem item = searchListView1.GetItemAtIndex(x);
                        item.refresh = true;
                        searchListView1.a[x] = item;
                    }
                    break;
            }
            searchListView1.Refresh();
        }

        private void dumpMem_Click(object sender, EventArgs e)
        {
            dumpForm df = new dumpForm();
            df.start = Convert.ToUInt64(startAddrTB.Text, 16);
            df.stop = Convert.ToUInt64(stopAddrTB.Text, 16);
            df.ShowDialog();
        }

        private void saveScan_Click(object sender, EventArgs e)
        {
            SaveFileDialog fd = new SaveFileDialog();
            fd.Filter = "Text Files (*.txt)|*.txt|All files (*.*)|*.*";
            fd.RestoreDirectory = true;

            if (fd.ShowDialog() == DialogResult.OK)
            {
                Stream stream = File.Open(fd.FileName, FileMode.Create);
                BinaryFormatter bformatter = new BinaryFormatter();
                //bformatter.Serialize(stream, searchListView1.GetItemsArray());
                bformatter.Serialize(stream, searchListView1.a.ToArray());
                stream.Close();

                MessageBox.Show("Saved!");
            }
        }

        private void loadScan_Click(object sender, EventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog();
            fd.Filter = "Text Files (*.txt)|*.txt|All files (*.*)|*.*";
            fd.RestoreDirectory = true;

            if (fd.ShowDialog() == DialogResult.OK)
            {
                SearchListView.SearchListViewItem[] items;
                using (Stream file = File.Open(fd.FileName, FileMode.Open))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    object obj = bf.Deserialize(file);

                    items = (obj as IEnumerable<SearchListView.SearchListViewItem>).ToArray();
                    searchListView1.ClearItems();
                    searchListView1.AddItemRange(items);
                    //searchListView1.a.AddRange(items);
                }

                if (searchListView1.TotalCount > 0 && items != null && items.Length > 0)
                {
                    SearchListView.SearchListViewItem item = items[0];

                    int ind = 0;
                    for (ind = 0; ind < searchTypeBox.Items.Count; ind++)
                        if (searchTypeBox.Items[ind].ToString() == SearchTypes[item.align].Name)
                            break;

                    searchTypeBox.SelectedIndex = ind;

                    SearchTypes[item.align].Initialize(SearchTypes[item.align].ItemToLString(item)[1], item.align);

                    isInitialScan = false;
                    searchMemory.Text = "New Scan";
                    MessageBox.Show("Loaded!");
                }

                items = null;
            }
        }

        #endregion

        #region Search Types

        int GetAlign(int def)
        {
            Form1.IBArg[] a = new Form1.IBArg[1];
            a[0].defStr = def.ToString();
            a[0].label = "Please enter an alignment to search by. Leave as 1 if you don't understand.";

            a = Form1.Instance.CallIBox(a);
            if (a == null || a[0].retStr == null || a[0].retStr == "")
            {
                return def;
            }
            else
            {
                try
                {
                    if (a[0].retStr.IndexOf("0x") == 0)
                        return (int)Convert.ToUInt32(a[0].retStr.Replace("0x", ""), 16);
                    else
                        return (int)uint.Parse(a[0].retStr);
                }
                catch { }
            }

            return def;
        }

        void ResetSearchCompBox()
        {
            lastSearchIndex = searchNameBox.SelectedIndex;
            string nameBoxSelItem = "";
            if (searchNameBox.SelectedItem != null)
                nameBoxSelItem = searchNameBox.SelectedItem.ToString();


            searchNameBox.Items.Clear();
            string curType = "";
            if (searchTypeBox.SelectedItem != null)
                curType = searchTypeBox.SelectedItem.ToString();
            foreach (ncSearcher nS in SearchComparisons)
            {
                if (nS.Type == SearchType.Both && !_isInitialScan && !nS.Exceptions.Contains(curType))
                {
                    searchNameBox.Items.Add(nS.Name);
                }
                else if (nS.Type == SearchType.Both && _isInitialScan)
                {
                    searchNameBox.Items.Add(nS.Name);
                }
                else if (isInitialScan && nS.Type == SearchType.InitialSearchOnly && !nS.Exceptions.Contains(curType))
                {
                    searchNameBox.Items.Add(nS.Name);
                }
                else if (!isInitialScan && nS.Type == SearchType.NextSearchOnly && !nS.Exceptions.Contains(curType))
                {
                    searchNameBox.Items.Add(nS.Name);
                }
            }

            if (lastSearchIndex >= searchNameBox.Items.Count || lastSearchIndex < 0)
                lastSearchIndex = 0;

            searchNameBox.SelectedIndex = -1;
            if (searchNameBox.Items.Count > 0)
            {
                searchNameBox.SelectedIndex = lastSearchIndex;
                for (int i = 0; i < searchNameBox.Items.Count; i++)
                {
                    if (searchNameBox.Items[i].ToString() == nameBoxSelItem)
                    {
                        searchNameBox.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void LoadSearch()
        {
            //Search Comparisons
            ncSearcher ncS = new ncSearcher();

            //Equal To
            ncS.Name = "Equal To";
            ncS.Type = SearchType.Both;
            ncS.Args = new string[] { "Value" };
            ncS.InitialSearch = new InitialSearch(EqualTo_InitSearch);
            ncS.NextSearch = new NextSearch(EqualTo_NextSearch);
            ncS.Exceptions = new string[0];
            ncS.TypeColumnOverride = new string[0];
            ncS.ItemToLString = null;
            ncS.ItemToString = null;
            SearchComparisons.Add(ncS);

            //Not Equal To
            ncS.Name = "Not Equal To";
            ncS.Type = SearchType.Both;
            ncS.Args = new string[] { "Value" };
            ncS.InitialSearch = new InitialSearch(NotEqualTo_InitSearch);
            ncS.NextSearch = new NextSearch(NotEqualTo_NextSearch);
            ncS.Exceptions = new string[0];
            ncS.TypeColumnOverride = new string[0];
            ncS.ItemToLString = null;
            ncS.ItemToString = null;
            SearchComparisons.Add(ncS);

            //Less Than (Signed)
            ncS.Name = "Less Than (S)";
            ncS.Type = SearchType.Both;
            ncS.Args = new string[] { "Value" };
            ncS.InitialSearch = new InitialSearch(LessThan_InitSearch);
            ncS.NextSearch = new NextSearch(LessThan_NextSearch);
            ncS.Exceptions = new string[0];
            ncS.TypeColumnOverride = new string[0];
            ncS.ItemToLString = null;
            ncS.ItemToString = null;
            SearchComparisons.Add(ncS);

            //Less Than or Equal (Signed)
            ncS.Name = "Less Than or Equal (S)";
            ncS.Type = SearchType.Both;
            ncS.Args = new string[] { "Value" };
            ncS.InitialSearch = new InitialSearch(LessThanEqualTo_InitSearch);
            ncS.NextSearch = new NextSearch(LessThanEqualTo_NextSearch);
            ncS.Exceptions = new string[0];
            ncS.TypeColumnOverride = new string[0];
            ncS.ItemToLString = null;
            ncS.ItemToString = null;
            SearchComparisons.Add(ncS);

            //Less Than (Unsigned)
            ncS.Name = "Less Than (U)";
            ncS.Type = SearchType.Both;
            ncS.Args = new string[] { "Value" };
            ncS.InitialSearch = new InitialSearch(LessThanU_InitSearch);
            ncS.NextSearch = new NextSearch(LessThanU_NextSearch);
            ncS.Exceptions = new string[0];
            ncS.TypeColumnOverride = new string[0];
            ncS.ItemToLString = null;
            ncS.ItemToString = null;
            SearchComparisons.Add(ncS);

            //Less Than or Equal (Unsigned)
            ncS.Name = "Less Than or Equal (U)";
            ncS.Type = SearchType.Both;
            ncS.Args = new string[] { "Value" };
            ncS.InitialSearch = new InitialSearch(LessThanEqualToU_InitSearch);
            ncS.NextSearch = new NextSearch(LessThanEqualToU_NextSearch);
            ncS.Exceptions = new string[0];
            ncS.TypeColumnOverride = new string[0];
            ncS.ItemToLString = null;
            ncS.ItemToString = null;
            SearchComparisons.Add(ncS);

            //Greater Than (Signed)
            ncS.Name = "Greater Than (S)";
            ncS.Type = SearchType.Both;
            ncS.Args = new string[] { "Value" };
            ncS.InitialSearch = new InitialSearch(GreaterThan_InitSearch);
            ncS.NextSearch = new NextSearch(GreaterThan_NextSearch);
            ncS.Exceptions = new string[0];
            ncS.TypeColumnOverride = new string[0];
            ncS.ItemToLString = null;
            ncS.ItemToString = null;
            SearchComparisons.Add(ncS);

            //Greater Than or Equal (Signed)
            ncS.Name = "Greater Than or Equal (S)";
            ncS.Type = SearchType.Both;
            ncS.Args = new string[] { "Value" };
            ncS.InitialSearch = new InitialSearch(GreaterThanEqualTo_InitSearch);
            ncS.NextSearch = new NextSearch(GreaterThanEqualTo_NextSearch);
            ncS.Exceptions = new string[0];
            ncS.TypeColumnOverride = new string[0];
            ncS.ItemToLString = null;
            ncS.ItemToString = null;
            SearchComparisons.Add(ncS);

            //Greater Than (Unsigned)
            ncS.Name = "Greater Than (U)";
            ncS.Type = SearchType.Both;
            ncS.Args = new string[] { "Value" };
            ncS.InitialSearch = new InitialSearch(GreaterThanU_InitSearch);
            ncS.NextSearch = new NextSearch(GreaterThanU_NextSearch);
            ncS.Exceptions = new string[0];
            ncS.TypeColumnOverride = new string[0];
            ncS.ItemToLString = null;
            ncS.ItemToString = null;
            SearchComparisons.Add(ncS);

            //Greater Than or Equal (Unsigned)
            ncS.Name = "Greater Than or Equal (U)";
            ncS.Type = SearchType.Both;
            ncS.Args = new string[] { "Value" };
            ncS.InitialSearch = new InitialSearch(GreaterThanEqualToU_InitSearch);
            ncS.NextSearch = new NextSearch(GreaterThanEqualToU_NextSearch);
            ncS.Exceptions = new string[0];
            ncS.TypeColumnOverride = new string[0];
            ncS.ItemToLString = null;
            ncS.ItemToString = null;
            SearchComparisons.Add(ncS);

            //Value Between (Unsigned)
            ncS.Name = "Value Between (U)";
            ncS.Type = SearchType.Both;
            ncS.Args = new string[] { "Min", "Max" };
            ncS.InitialSearch = new InitialSearch(ValueBetween_InitSearch);
            ncS.NextSearch = new NextSearch(ValueBetween_NextSearch);
            ncS.Exceptions = new string[] { "Text" };
            ncS.TypeColumnOverride = new string[0];
            ncS.ItemToLString = null;
            ncS.ItemToString = null;
            SearchComparisons.Add(ncS);

            //Pointer
            ncS.Name = "Pointer";
            ncS.Type = SearchType.Both;
            ncS.Args = new string[] { "Value" };
            ncS.InitialSearch = new InitialSearch(Pointer_InitSearch);
            ncS.NextSearch = new NextSearch(Pointer_NextSearch);
            ncS.Exceptions = new string[] { "1 byte", "2 bytes", "8 bytes", "X bytes", "Text", "Float", "Double" };
            ncS.TypeColumnOverride = new string[] { "Address", "Value", "Offset", "Type" };
            ncS.ItemToLString = new ParseItemToListString(Pointer_ItemToLString);
            ncS.ItemToString = new ToSListViewItem(Pointer_ItemToString);
            SearchComparisons.Add(ncS);

            //Unknown Initial Value
            ncS.Name = "Unknown Value";
            ncS.Type = SearchType.InitialSearchOnly;
            ncS.Args = new string[] { };
            ncS.InitialSearch = new InitialSearch(UnknownValue_InitSearch);
            ncS.NextSearch = null;
            ncS.Exceptions = new string[] { "X bytes", "Text" };
            ncS.TypeColumnOverride = new string[0];
            ncS.ItemToLString = null;
            ncS.ItemToString = null;
            SearchComparisons.Add(ncS);

            //Increased (Signed)
            ncS.Name = "Increased (S)";
            ncS.Type = SearchType.NextSearchOnly;
            ncS.Args = new string[] { };
            ncS.InitialSearch = null;
            ncS.NextSearch = new NextSearch(Increased_NextSearch);
            ncS.Exceptions = new string[] { "Text" };
            ncS.TypeColumnOverride = new string[0];
            ncS.ItemToLString = null;
            ncS.ItemToString = null;
            SearchComparisons.Add(ncS);

            //Increased By (Unsigned)
            ncS.Name = "Increased By (U)";
            ncS.Type = SearchType.NextSearchOnly;
            ncS.Args = new string[] { "Value" };
            ncS.InitialSearch = null;
            ncS.NextSearch = new NextSearch(IncreasedBy_NextSearch);
            ncS.Exceptions = new string[] { "Text" };
            ncS.TypeColumnOverride = new string[0];
            ncS.ItemToLString = null;
            ncS.ItemToString = null;
            SearchComparisons.Add(ncS);

            //Decreased (Signed)
            ncS.Name = "Decreased (S)";
            ncS.Type = SearchType.NextSearchOnly;
            ncS.Args = new string[] { };
            ncS.InitialSearch = null;
            ncS.NextSearch = new NextSearch(Decreased_NextSearch);
            ncS.Exceptions = new string[] { "Text" };
            ncS.TypeColumnOverride = new string[0];
            ncS.ItemToLString = null;
            ncS.ItemToString = null;
            SearchComparisons.Add(ncS);

            //Decreased By (Unsigned)
            ncS.Name = "Decreased By (U)";
            ncS.Type = SearchType.NextSearchOnly;
            ncS.Args = new string[] { "Value" };
            ncS.InitialSearch = null;
            ncS.NextSearch = new NextSearch(DecreasedBy_NextSearch);
            ncS.Exceptions = new string[] { "Text" };
            ncS.TypeColumnOverride = new string[0];
            ncS.ItemToLString = null;
            ncS.ItemToString = null;
            SearchComparisons.Add(ncS);

            //Changed
            ncS.Name = "Changed";
            ncS.Type = SearchType.NextSearchOnly;
            ncS.Args = new string[] { };
            ncS.InitialSearch = null;
            ncS.NextSearch = new NextSearch(Changed_NextSearch);
            ncS.Exceptions = new string[] { };
            ncS.TypeColumnOverride = new string[0];
            ncS.ItemToLString = null;
            ncS.ItemToString = null;
            SearchComparisons.Add(ncS);

            //Unchanged
            ncS.Name = "Unchanged";
            ncS.Type = SearchType.NextSearchOnly;
            ncS.Args = new string[] { };
            ncS.InitialSearch = null;
            ncS.NextSearch = new NextSearch(Unchanged_NextSearch);
            ncS.Exceptions = new string[] { };
            ncS.TypeColumnOverride = new string[0];
            ncS.ItemToLString = null;
            ncS.ItemToString = null;
            SearchComparisons.Add(ncS);

            //Joker/Pad Address Finder
            ncS.Name = @"Joker/Pad Address Finder";
            ncS.Type = SearchType.Both;
            ncS.Args = new string[] { };
            ncS.InitialSearch = new InitialSearch(Joker_InitSearch);
            ncS.NextSearch = new NextSearch(Joker_NextSearch);
            ncS.Exceptions = new string[] { "1 byte", "2 bytes", "8 bytes", "X bytes", "Text", "Float", "Double" };
            ncS.TypeColumnOverride = new string[0];
            ncS.ItemToLString = null;
            ncS.ItemToString = null;
            SearchComparisons.Add(ncS);

            //Bit Difference
            ncS.Name = @"Bit Difference";
            ncS.Type = SearchType.NextSearchOnly;
            ncS.Args = new string[] { "Bits Changed" };
            ncS.InitialSearch = null;
            ncS.NextSearch = new NextSearch(BitDif_NextSearch);
            ncS.Exceptions = new string[] { "Text", "Float", "Double" };
            ncS.TypeColumnOverride = new string[] { "Address", "Value", "Difference", "Type" };
            ncS.ItemToLString = new ParseItemToListString(BitDif_ItemToLString);
            ncS.ItemToString = null;
            SearchComparisons.Add(ncS);

            //Search Types
            ncSearchType ncST = new ncSearchType();

            //1 byte
            ncST.ByteSize = 1;
            ncST.Name = "1 byte";
            ncST.ListColumnNames = new string[] { "Address", "Value", "Dec", "Type" };
            ncST.ToItem = new SearchToItem(standardByte_ToItem);
            ncST.BAToString = new ByteAToString(sType1B_ToString);
            ncST.CheckboxName = "Hex";
            ncST.DefaultValue = "00";
            ncST.CheckboxConvert = new CheckboxConvert(ConvertHexDec);
            ncST.ToByteArray = new StringToByteA(sType1B_ToByteArray);
            ncST.ItemToString = new ToSListViewItem(sType1B_ItemToString);
            ncST.ItemToLString = new ParseItemToListString(standardByte_ItemToLString);
            ncST.Initialize = new TypeInitialize(NullTypeInitialize);
            ncST.areArgsValid = new isValueValid(sType1B_areArgsValid);
            ncST.ignoreAlignment = false;
            SearchTypes.Add(ncST);

            //2 bytes
            ncST.ByteSize = 2;
            ncST.Name = "2 bytes";
            ncST.ListColumnNames = new string[] { "Address", "Value", "Dec", "Type" };
            ncST.ToItem = new SearchToItem(standardByte_ToItem);
            ncST.BAToString = new ByteAToString(sType2B_ToString);
            ncST.CheckboxName = "Hex";
            ncST.DefaultValue = "0000";
            ncST.CheckboxConvert = new CheckboxConvert(ConvertHexDec);
            ncST.ToByteArray = new StringToByteA(sType2B_ToByteArray);
            ncST.ItemToString = new ToSListViewItem(sType2B_ItemToString);
            ncST.ItemToLString = new ParseItemToListString(standardByte_ItemToLString);
            ncST.Initialize = new TypeInitialize(NullTypeInitialize);
            ncST.areArgsValid = new isValueValid(sType2B_areArgsValid);
            ncST.ignoreAlignment = false;
            SearchTypes.Add(ncST);

            //4 bytes
            ncST.ByteSize = 4;
            ncST.Name = "4 bytes";
            ncST.ListColumnNames = new string[] { "Address", "Value", "Dec", "Type" };
            ncST.ToItem = new SearchToItem(standardByte_ToItem);
            ncST.BAToString = new ByteAToString(sType4B_ToString);
            ncST.CheckboxName = "Hex";
            ncST.DefaultValue = "00000000";
            ncST.CheckboxConvert = new CheckboxConvert(ConvertHexDec);
            ncST.ToByteArray = new StringToByteA(sType4B_ToByteArray);
            ncST.ItemToString = new ToSListViewItem(sType4B_ItemToString);
            ncST.ItemToLString = new ParseItemToListString(standardByte_ItemToLString);
            ncST.Initialize = new TypeInitialize(NullTypeInitialize);
            ncST.areArgsValid = new isValueValid(sType4B_areArgsValid);
            ncST.ignoreAlignment = false;
            SearchTypes.Add(ncST);

            //8 bytes
            ncST.ByteSize = 8;
            ncST.Name = "8 bytes";
            ncST.ListColumnNames = new string[] { "Address", "Value", "Dec", "Type" };
            ncST.ToItem = new SearchToItem(standardByte_ToItem);
            ncST.BAToString = new ByteAToString(sType8B_ToString);
            ncST.CheckboxName = "Hex";
            ncST.DefaultValue = "0000000000000000";
            ncST.CheckboxConvert = new CheckboxConvert(ConvertHexDec);
            ncST.ToByteArray = new StringToByteA(sType8B_ToByteArray);
            ncST.ItemToString = new ToSListViewItem(sType8B_ItemToString);
            ncST.ItemToLString = new ParseItemToListString(standardByte_ItemToLString);
            ncST.Initialize = new TypeInitialize(NullTypeInitialize);
            ncST.areArgsValid = new isValueValid(sType8B_areArgsValid);
            ncST.ignoreAlignment = false;
            SearchTypes.Add(ncST);

            //X bytes
            ncST.ByteSize = 0;
            ncST.Name = "X bytes";
            ncST.ListColumnNames = new string[] { "Address", "Value", "Dec", "Type" };
            ncST.ToItem = new SearchToItem(standardByte_ToItem);
            ncST.BAToString = new ByteAToString(sTypeXB_ToString);
            ncST.CheckboxName = "";
            ncST.DefaultValue = "00000000";
            ncST.CheckboxConvert = new CheckboxConvert(ConvertHexDec);
            ncST.ToByteArray = new StringToByteA(sTypeXB_ToByteArray);
            ncST.ItemToString = new ToSListViewItem(sTypeXB_ItemToString);
            ncST.ItemToLString = new ParseItemToListString(sTypeXB_ItemToLString);
            ncST.Initialize = new TypeInitialize(sTypeXB_Initialize);
            ncST.areArgsValid = new isValueValid(sTypeXB_areArgsValid);
            ncST.ignoreAlignment = true;
            SearchTypes.Add(ncST);

            //Text
            ncST.ByteSize = 0;
            ncST.Name = "Text";
            ncST.ListColumnNames = new string[] { "Address", "Text", "Invalid", "Type" };
            ncST.ToItem = new SearchToItem(standardByte_ToItem);
            ncST.BAToString = new ByteAToString(sTypeText_ToString);
            ncST.CheckboxName = "Match Case";
            ncST.DefaultValue = "Example";
            ncST.CheckboxConvert = new CheckboxConvert(NullCheckboxConvert);
            ncST.ToByteArray = new StringToByteA(sTypeText_ToByteArray);
            ncST.ItemToString = new ToSListViewItem(sTypeText_ItemToString);
            ncST.ItemToLString = new ParseItemToListString(sTypeText_ItemToLString);
            ncST.Initialize = new TypeInitialize(sTypeText_Initialize);
            ncST.areArgsValid = new isValueValid(NullareArgsValid);
            ncST.ignoreAlignment = true;
            SearchTypes.Add(ncST);

            //Float
            ncST.ByteSize = 4;
            ncST.Name = "Float";
            ncST.ListColumnNames = new string[] { "Address", "Hex", "Float", "Type" };
            ncST.ToItem = new SearchToItem(standardByte_ToItem);
            ncST.BAToString = new ByteAToString(sTypeFloat_ToString);
            ncST.CheckboxName = "";
            ncST.DefaultValue = "1.0";
            ncST.CheckboxConvert = new CheckboxConvert(NullCheckboxConvert);
            ncST.ToByteArray = new StringToByteA(sTypeFloat_ToByteArray);
            ncST.ItemToString = new ToSListViewItem(sTypeFloat_ItemToString);
            ncST.ItemToLString = new ParseItemToListString(sTypeFloat_ItemToLString);
            ncST.Initialize = new TypeInitialize(sTypeFloat_Initialize);
            ncST.areArgsValid = new isValueValid(sTypeFloat_areArgsValid);
            ncST.ignoreAlignment = false;
            SearchTypes.Add(ncST);

            //Double
            ncST.ByteSize = 8;
            ncST.Name = "Double";
            ncST.ListColumnNames = new string[] { "Address", "Hex", "Double", "Type" };
            ncST.ToItem = new SearchToItem(standardByte_ToItem);
            ncST.BAToString = new ByteAToString(sTypeDouble_ToString);
            ncST.CheckboxName = "";
            ncST.DefaultValue = "1.0";
            ncST.CheckboxConvert = new CheckboxConvert(NullCheckboxConvert);
            ncST.ToByteArray = new StringToByteA(sTypeDouble_ToByteArray);
            ncST.ItemToString = new ToSListViewItem(sTypeDouble_ItemToString);
            ncST.ItemToLString = new ParseItemToListString(sTypeDouble_ItemToLString);
            ncST.Initialize = new TypeInitialize(sTypeDouble_Initialize);
            ncST.areArgsValid = new isValueValid(sTypeDouble_areArgsValid);
            ncST.ignoreAlignment = false;
            SearchTypes.Add(ncST);

            //Populate Search combo box
            ResetSearchCompBox();

            //Populate Search type combo box
            searchTypeBox.Items.Clear();
            foreach (ncSearchType nST in SearchTypes)
            {
                searchTypeBox.Items.Add(nST.Name);
            }

            searchTypeBox.SelectedIndex = 0;
            searchNameBox.SelectedIndex = 0;
        }

        #region 1 Byte

        byte[] sType1B_ToByteArray(string str)
        {
            return StringToByteArray(str, 1 * 2);
        }

        string sType1B_ToString(byte[] val)
        {
            return standardByte_ToString(val, 1);
        }

        string sType1B_ItemToString(SearchListView.SearchListViewItem item)
        {
            return "0 " + item.addr.ToString("X8") + " " + SearchTypes[item.align].BAToString(item.newVal);
        }

        bool sType1B_areArgsValid(string[] args, out string error)
        {
            error = "";

            for (int x = 0; x < args.Length; x++)
            {
                for (int y = 0; y < args[x].Length; y++)
                {
                    char tempC = args[x][y];
                    //Make upper case
                    if (tempC >= 0x61)
                        tempC -= (char)0x20;

                    if (!((tempC >= 0x30 && tempC <= 0x39) || (tempC >= 0x41 && tempC <= 0x46)))
                    {
                        error += "Argument " + (x + 1).ToString() + " contains invalid character \'" + args[x][y].ToString() + "\' (Index: " + (y + 1).ToString() + ")" + Environment.NewLine;
                    }
                }
            }

            if (error != "")
                return false;
            return true;
        }

        #endregion

        #region 2 Bytes

        byte[] sType2B_ToByteArray(string str)
        {
            return StringToByteArray(str, 2 * 2);
        }

        string sType2B_ToString(byte[] val)
        {
            return standardByte_ToString(val, 2);
        }

        string sType2B_ItemToString(SearchListView.SearchListViewItem item)
        {
            return "0 " + item.addr.ToString("X8") + " " + SearchTypes[item.align].BAToString(item.newVal);
        }

        bool sType2B_areArgsValid(string[] args, out string error)
        {
            error = "";

            for (int x = 0; x < args.Length; x++)
            {
                for (int y = 0; y < args[x].Length; y++)
                {
                    char tempC = args[x][y];
                    //Make upper case
                    if (tempC >= 0x61)
                        tempC -= (char)0x20;

                    if (!((tempC >= 0x30 && tempC <= 0x39) || (tempC >= 0x41 && tempC <= 0x46)))
                    {
                        error += "Argument " + (x + 1).ToString() + " contains invalid character \'" + args[x][y].ToString() + "\' (Index: " + (y + 1).ToString() + ")" + Environment.NewLine;
                    }
                }
            }

            if (error != "")
                return false;
            return true;
        }

        #endregion

        #region 4 Bytes

        byte[] sType4B_ToByteArray(string str)
        {
            return StringToByteArray(str, 4 * 2);
        }

        string sType4B_ToString(byte[] val)
        {
            return standardByte_ToString(val, 4);
        }

        string sType4B_ItemToString(SearchListView.SearchListViewItem item)
        {
            return "0 " + item.addr.ToString("X8") + " " + SearchTypes[item.align].BAToString(item.newVal);
        }

        bool sType4B_areArgsValid(string[] args, out string error)
        {
            error = "";

            for (int x = 0; x < args.Length; x++)
            {
                for (int y = 0; y < args[x].Length; y++)
                {
                    char tempC = args[x][y];
                    //Make upper case
                    if (tempC >= 0x61)
                        tempC -= (char)0x20;

                    if (!((tempC >= 0x30 && tempC <= 0x39) || (tempC >= 0x41 && tempC <= 0x46)))
                    {
                        error += "Argument " + (x + 1).ToString() + " contains invalid character \'" + args[x][y].ToString() + "\' (Index: " + (y + 1).ToString() + ")" + Environment.NewLine;
                    }
                }
            }

            if (error != "")
                return false;
            return true;
        }

        #endregion

        #region 8 Bytes

        byte[] sType8B_ToByteArray(string str)
        {
            return StringToByteArray(str, 8 * 2);
        }

        string sType8B_ToString(byte[] val)
        {
            return standardByte_ToString(val, 8);
        }

        string sType8B_ItemToString(SearchListView.SearchListViewItem item)
        {
            return "0 " + item.addr.ToString("X8") + " " + SearchTypes[item.align].BAToString(item.newVal);
        }

        bool sType8B_areArgsValid(string[] args, out string error)
        {
            error = "";

            for (int x = 0; x < args.Length; x++)
            {
                for (int y = 0; y < args[x].Length; y++)
                {
                    char tempC = args[x][y];
                    //Make upper case
                    if (tempC >= 0x61)
                        tempC -= (char)0x20;

                    if (!((tempC >= 0x30 && tempC <= 0x39) || (tempC >= 0x41 && tempC <= 0x46)))
                    {
                        error += "Argument " + (x + 1).ToString() + " contains invalid character \'" + args[x][y].ToString() + "\' (Index: " + (y + 1).ToString() + ")" + Environment.NewLine;
                    }
                }
            }

            if (error != "")
                return false;
            return true;
        }

        #endregion

        #region X Bytes

        byte[] sTypeXB_ToByteArray(string str)
        {
            int len = str.Length;
            if ((len % 2) == 1)
                len++;
            return StringToByteArray(str, len);
        }

        string sTypeXB_ToString(byte[] val)
        {
            return standardByte_ToString(val, val.Length);
        }

        void sTypeXB_Initialize(string arg, int typeIndex)
        {
            int len = arg.Length;
            if ((len % 2) == 1)
                len++;
            ncSearchType type = SearchTypes[typeIndex];
            type.ByteSize = len / 2;
            SearchTypes[typeIndex] = type;
        }

        string sTypeXB_ItemToString(SearchListView.SearchListViewItem item)
        {
            return "0 " + item.addr.ToString("X8") + " " + SearchTypes[item.align].BAToString(item.newVal);
        }

        string[] sTypeXB_ItemToLString(SearchListView.SearchListViewItem item)
        {
            ncSearchType type = SearchTypes[item.align];
            string[] ret = new string[4];
            int size = type.ByteSize;

            ret[0] = item.addr.ToString("X8");
            ret[3] = type.Name;
            if (size <= 8)
            {
                ulong val = misc.ByteArrayToLong(item.newVal, 0, size);
                ret[1] = val.ToString("X" + (type.ByteSize * 2).ToString());
                ret[2] = val.ToString();
            }
            else
            {
                ret[2] = "(Too Large)";
                ret[1] = type.BAToString(item.newVal);
            }
            return ret;
        }

        bool sTypeXB_areArgsValid(string[] args, out string error)
        {
            error = "";

            for (int x = 0; x < args.Length; x++)
            {
                for (int y = 0; y < args[x].Length; y++)
                {
                    char tempC = args[x][y];
                    //Make upper case
                    if (tempC >= 0x61)
                        tempC -= (char)0x20;

                    if (!((tempC >= 0x30 && tempC <= 0x39) || (tempC >= 0x41 && tempC <= 0x46)))
                    {
                        error += "Argument " + (x + 1).ToString() + " contains invalid character \'" + args[x][y].ToString() + "\' (Index: " + (y + 1).ToString() + ")" + Environment.NewLine;
                    }
                }
            }

            if (error != "")
                return false;
            return true;
        }

        #endregion

        #region Text

        byte[] sTypeText_ToByteArray(string str)
        {
            byte[] ret = new byte[str.Length];
            for (int x = 0; x < str.Length; x++)
            {
                ret[x] = (byte)((char)str[x]);
            }

            return ret;
        }

        string sTypeText_ToString(byte[] val)
        {
            return misc.ByteAToString(val, "");
        }

        void sTypeText_Initialize(string arg, int typeIndex)
        {
            int len = arg.Length;
            if ((len % 2) == 1)
                len++;
            ncSearchType type = SearchTypes[typeIndex];
            type.ByteSize = len;
            SearchTypes[typeIndex] = type;
        }

        string sTypeText_ItemToString(SearchListView.SearchListViewItem item)
        {
            return "1 " + item.addr.ToString("X8") + " " + SearchTypes[item.align].BAToString(item.newVal);
        }

        string[] sTypeText_ItemToLString(SearchListView.SearchListViewItem item)
        {
            ncSearchType type = SearchTypes[item.align];
            string[] ret = new string[4];
            ret[0] = item.addr.ToString("X8");
            ret[1] = misc.ByteAToString(item.newVal, "");
            ret[2] = "Invalid";
            ret[3] = type.Name;
            return ret;
        }

        #endregion

        #region Float

        byte[] sTypeFloat_ToByteArray(string str)
        {
            Single flt = Single.Parse(str);
            byte[] b = BitConverter.GetBytes(flt);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(b);
            return b;
        }

        string sTypeFloat_ToString(byte[] val)
        {
            byte[] temp = (byte[])val.Clone();
            if (BitConverter.IsLittleEndian)
                Array.Reverse(temp);
            Single flt = BitConverter.ToSingle(temp, 0);
            return flt.ToString("G");
        }

        void sTypeFloat_Initialize(string arg, int typeIndex)
        {

        }

        string sTypeFloat_ItemToString(SearchListView.SearchListViewItem item)
        {
            return "2 " + item.addr.ToString("X8") + " " + SearchTypes[item.align].BAToString(item.newVal);
        }

        string[] sTypeFloat_ItemToLString(SearchListView.SearchListViewItem item)
        {
            ncSearchType type = SearchTypes[item.align];
            string[] ret = new string[4];
            int size = type.ByteSize;

            ulong val = misc.ByteArrayToLong(item.newVal, 0, size);
            ret[0] = item.addr.ToString("X8");
            ret[1] = val.ToString("X" + (type.ByteSize * 2).ToString());
            ret[2] = SearchTypes[item.align].BAToString(item.newVal);
            ret[3] = type.Name;
            return ret;
        }

        bool sTypeFloat_areArgsValid(string[] args, out string error)
        {
            error = "";

            for (int x = 0; x < args.Length; x++)
            {
                for (int y = 0; y < args[x].Length; y++)
                {
                    char tempC = args[x][y];

                    if (!((tempC >= 0x30 && tempC <= 0x39) || tempC == '.'))
                    {
                        error += "Argument " + (x + 1).ToString() + " contains invalid character \'" + args[x][y].ToString() + "\' (Index: " + (y + 1).ToString() + ")" + Environment.NewLine;
                    }
                }
            }

            if (error != "")
                return false;
            return true;
        }

        #endregion

        #region Double

        byte[] sTypeDouble_ToByteArray(string str)
        {
            Double flt = Double.Parse(str);
            byte[] b = BitConverter.GetBytes(flt);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(b);
            return b;
        }

        string sTypeDouble_ToString(byte[] val)
        {
            byte[] temp = (byte[])val.Clone();
            if (BitConverter.IsLittleEndian)
                Array.Reverse(temp);
            Double flt = BitConverter.ToDouble(temp, 0);
            return flt.ToString("G");
        }

        void sTypeDouble_Initialize(string arg, int typeIndex)
        {

        }

        string sTypeDouble_ItemToString(SearchListView.SearchListViewItem item)
        {
            return "0 " + item.addr.ToString("X8") + " " + standardByte_ToString(item.newVal, 8);
        }

        string[] sTypeDouble_ItemToLString(SearchListView.SearchListViewItem item)
        {
            ncSearchType type = SearchTypes[item.align];
            string[] ret = new string[4];
            int size = type.ByteSize;

            ulong val = misc.ByteArrayToLong(item.newVal, 0, size);
            ret[0] = item.addr.ToString("X8");
            ret[1] = val.ToString("X" + (type.ByteSize * 2).ToString());
            ret[2] = SearchTypes[item.align].BAToString(item.newVal);
            ret[3] = type.Name;
            return ret;
        }

        bool sTypeDouble_areArgsValid(string[] args, out string error)
        {
            error = "";

            for (int x = 0; x < args.Length; x++)
            {
                for (int y = 0; y < args[x].Length; y++)
                {
                    char tempC = args[x][y];

                    if (!((tempC >= 0x30 && tempC <= 0x39) || tempC == '.'))
                    {
                        error += "Argument " + (x + 1).ToString() + " contains invalid character \'" + args[x][y].ToString() + "\' (Index: " + (y + 1).ToString() + ")" + Environment.NewLine;
                    }
                }
            }

            if (error != "")
                return false;
            return true;
        }

        #endregion

        #region Pointer

        string Pointer_ItemToString(SearchListView.SearchListViewItem item)
        {
            uint off = item.misc - (uint)misc.ByteArrayToLong(item.newVal, 0, 4);

            string ret =  "6 " + item.addr.ToString("X8") + " " + misc.sRight((off).ToString("X8"), 8) + "\r\n";
            ret += "0 00000000 " + ((uint)Form1.getVal((uint)Form1.getVal(item.addr, Form1.ValueType.UINT) + off, Form1.ValueType.UINT)).ToString("X8");

            return ret;
        }

        #endregion

        void NullTypeInitialize(string arg, int typeIndex)
        {

        }

        bool NullareArgsValid(string[] args, out string error)
        {
            error = "";
            return true;
        }

        string NullCheckboxConvert(string val, bool state)
        {
            return val;
        }

        string ConvertHexDec(string val, bool isHex)
        {
            if (val == null)
                return "";

            string clean = val.Trim();

            if (clean.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                clean = clean.Substring(2);

            if (clean.Length == 0)
                return "";

            try
            {
                if (!isHex)
                {
                    // User is switching from Hex -> Dec.
                    ulong cval = Convert.ToUInt64(clean, 16);
                    return cval.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                {
                    // User is switching from Dec -> Hex.
                    // If it already looks like hex, preserve it instead of throwing.
                    bool hasHexLetters = false;
                    for (int i = 0; i < clean.Length; i++)
                    {
                        char c = clean[i];
                        if ((c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'))
                        {
                            hasHexLetters = true;
                            break;
                        }
                    }

                    if (hasHexLetters)
                        return clean.ToUpperInvariant();

                    ulong cval = Convert.ToUInt64(clean, 10);
                    return cval.ToString("X");
                }
            }
            catch
            {
                // Do not destroy the user's input or spam exception boxes.
                return val;
            }
        }

        byte[] StringToByteArray(string val, int size)
        {
            val = val.PadLeft(size, '0');
            val = misc.sLeft(val, size);

            byte[] ret = new byte[size / 2];
            for (int x = 0; x < size; x += 2)
                ret[x / 2] = byte.Parse(misc.sMid(val, x, 2), System.Globalization.NumberStyles.HexNumber);
            return ret;
        }

        SearchListView.SearchListViewItem standardByte_ToItem(ulong addr, byte[] newVal, byte[] oldVal, int typeIndex, uint misc = 0)
        {
            SearchListView.SearchListViewItem ret = new SearchListView.SearchListViewItem();

            ret.addr = (uint)addr;
            ret.align = typeIndex;
            ret.oldVal = oldVal;
            ret.newVal = newVal;
            ret.misc = misc;

            return ret;
        }

        string[] standardByte_ItemToLString(SearchListView.SearchListViewItem item)
        {
            ncSearchType type = SearchTypes[item.align];
            string[] ret = new string[4];
            int size = type.ByteSize;

            ulong val = misc.ByteArrayToLong(item.newVal, 0, size);
            ret[0] = item.addr.ToString("X8");
            ret[1] = val.ToString("X" + (type.ByteSize * 2).ToString());
            ret[2] = val.ToString();
            ret[3] = type.Name;
            return ret;
        }

        string standardByte_ToString(byte[] val, int size)
        {
            string ret = "";
            for (int x = 0; x < size; x++)
            {
                if (x < val.Length)
                    ret += val[x].ToString("X2");
                else
                    ret += "00";
            }
            return ret;
        }

        string[] Pointer_ItemToLString(SearchListView.SearchListViewItem item)
        {
            ncSearchType type = SearchTypes[item.align];
            string[] ret = new string[4];
            int size = type.ByteSize;

            ulong val = misc.ByteArrayToLong(item.newVal, 0, size);
            ret[0] = item.addr.ToString("X8");
            ret[1] = val.ToString("X" + (type.ByteSize * 2).ToString());
            ret[2] = misc.sRight(((ulong)item.misc - val).ToString("X8"), 8);
            ret[3] = type.Name;
            return ret;
        }

        string[] BitDif_ItemToLString(SearchListView.SearchListViewItem item)
        {
            ncSearchType type = SearchTypes[item.align];
            string[] ret = new string[4];
            int size = type.ByteSize;

            ulong val = misc.ByteArrayToLong(item.newVal, 0, size);
            ulong val2 = misc.ByteArrayToLong(item.oldVal, 0, size) ^ val;
            ret[0] = item.addr.ToString("X8");
            ret[1] = val.ToString("X" + (type.ByteSize * 2).ToString());
            ret[2] = val2.ToString("X" + (type.ByteSize * 2).ToString());
            ret[3] = type.Name;
            return ret;
        }

        #endregion

        #region Initial Searches

        void standardByte_InitSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args, int cmpIntType)
        {
            bool gotRes = false;
            ncSearchType type = SearchTypes[typeIndex];
            if (args.Length > 0)
            {
                type.Initialize((string)args[0], typeIndex);
                type = SearchTypes[typeIndex];
            }

            bool updateCont = false;
            int size = 0x10000;
            //if ((stopAddr - startAddr) <= (ulong)size)
            //    updateCont = true;

            byte[] cmp = new byte[size];

            int realDif = 0;
            if (updateCont)
                realDif = (int)(stopAddr - startAddr);
            else
                realDif = (int)GetRealDif(startAddr, stopAddr, (ulong)size);
            SetProgBarMax(realDif - 1);

            byte[] cmpArray = type.ToByteArray((string)args[0]);
            int resCnt = 0;
            int incSize = type.ByteSize;
            if (type.ignoreAlignment)
                incSize = GetAlign(1);
            List<SearchListView.SearchListViewItem> itemsToAdd = new List<SearchListView.SearchListViewItem>();

            bool isMatchCase = false;
            if (type.Name == "Text")
            {
                Invoke((MethodInvoker)delegate
                {
                    isMatchCase = SearchArgs[0].GetState();
                });

                if (!isMatchCase)
                {
                    for (int x = 0; x < cmpArray.Length; x++)
                        if (cmpArray[x] > 0x60)
                            cmpArray[x] -= 0x20;
                }
            }

            for (ulong addr = startAddr; addr < stopAddr; addr += (ulong)size)
            {
                if (_shouldStopSearch)
                    break;

                gotRes = false;
                addr = misc.ParseSchAddr(addr);

                if (addr != startAddr && incSize == 1 && type.ByteSize != 1)
                {
                    addr -= (ulong)(cmpArray.Length + 1);
                }

                if (Form1.apiGetMem(addr, ref cmp))
                {
                    for (int x = 0; x <= (size - type.ByteSize); x += incSize)
                    {
                        if (_shouldStopSearch)
                            break;

                        byte[] tempArr = new byte[type.ByteSize];
                        Array.Copy(cmp, x, tempArr, 0, type.ByteSize);
                        tempArr = misc.notrevif(tempArr);
                        bool isTrue = false;
                        if (type.Name == "Text")
                        {
                            isTrue = misc.ArrayCompareText(cmpArray, tempArr, isMatchCase, cmpIntType);
                        }
                        else
                        {
                            isTrue = misc.ArrayCompare(cmpArray, tempArr, null, cmpIntType);
                        }

                        if (isTrue)
                        {
                            itemsToAdd.Add(type.ToItem(addr + (ulong)x, tempArr, new byte[0], typeIndex));
                            resCnt++;
                            gotRes = true;
                            //SetStatusLabel("Results: " + resCnt.ToString());
                        }
                        //if (updateCont)
                        //    IncProgBar(incSize);
                    }

                    if (gotRes)
                    {
                        SetStatusLabel("Results: " + resCnt.ToString("N0"));
                        AddResultRange(itemsToAdd);
                        itemsToAdd.Clear();
                    }

                }

                //Thread.Sleep(100);
                //if (!updateCont)
                    IncProgBar(1);
            }

            Invoke((MethodInvoker)delegate
            {
                searchListView1.AddItemsFromList();
            });
        }

        private void ClearActiveSnapshot()
        {
            try
            {
                if (!String.IsNullOrEmpty(activeSnapshotPath) && File.Exists(activeSnapshotPath))
                    File.Delete(activeSnapshotPath);
            }
            catch
            {
            }

            activeSnapshotPath = null;
            activeSnapshotTypeIndex = -1;
            activeSnapshotByteSize = 0;
            activeSnapshotResultCount = 0;
        }

        private string CreateSnapshotPath()
        {
            string dir = Path.Combine(Application.StartupPath, "Scans");

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            return Path.Combine(dir, "scan_" + DateTime.Now.Ticks.ToString() + ".ncsnap");
        }

        private bool HasActiveSnapshot()
        {
            return !String.IsNullOrEmpty(activeSnapshotPath) && File.Exists(activeSnapshotPath);
        }

        private bool IsLittleEndianModeChecked()
        {
            if (littleEndianCB == null)
                return false;

            bool result = false;

            if (littleEndianCB.InvokeRequired)
            {
                littleEndianCB.Invoke((MethodInvoker)delegate
                {
                    result = littleEndianCB.Checked;
                });
            }
            else
            {
                result = littleEndianCB.Checked;
            }

            return result;
        }

        private bool IsNoNegativeChecked()
        {
            if (noNegativeCB == null)
                return false;

            bool result = false;

            if (noNegativeCB.InvokeRequired)
            {
                noNegativeCB.Invoke((MethodInvoker)delegate
                {
                    result = noNegativeCB.Checked;
                });
            }
            else
            {
                result = noNegativeCB.Checked;
            }

            return result;
        }

        private bool IsCleanFloatChecked()
        {
            if (cleanFloatCB == null)
                return false;

            bool result = false;

            if (cleanFloatCB.InvokeRequired)
            {
                cleanFloatCB.Invoke((MethodInvoker)delegate
                {
                    result = cleanFloatCB.Checked;
                });
            }
            else
            {
                result = cleanFloatCB.Checked;
            }

            return result;
        }

        private bool IsNoZeroChecked()
        {
            if (noZeroCB == null)
                return false;

            bool result = false;

            if (noZeroCB.InvokeRequired)
            {
                noZeroCB.Invoke((MethodInvoker)delegate
                {
                    result = noZeroCB.Checked;
                });
            }
            else
            {
                result = noZeroCB.Checked;
            }

            return result;
        }

        private void RefreshNewScannerFilterOptions()
        {
            activeFilterNoNegative = IsNoNegativeChecked();
            activeFilterCleanFloat = IsCleanFloatChecked();
            activeFilterNoZero = IsNoZeroChecked();
        }

        private bool CanUseNewExactEqualSearch(int typeIndex)
        {
            string typeName = SearchTypes[typeIndex].Name;
            return typeName == "1 byte" ||
                   typeName == "2 bytes" ||
                   typeName == "4 bytes" ||
                   typeName == "8 bytes" ||
                   typeName == "Float" ||
                   typeName == "Double";
        }

        private void RunNewExactEqualSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args)
        {
            ncSearchType type = SearchTypes[typeIndex];
            if (args.Length > 0)
            {
                type.Initialize((string)args[0], typeIndex);
                type = SearchTypes[typeIndex];
            }

            int alignment = type.ignoreAlignment ? GetAlign(1) : type.ByteSize;
            if (alignment <= 0)
                alignment = type.ByteSize;

            byte[] compareBytes = type.ToByteArray((string)args[0]);

            int totalBlocks = (int)(((stopAddr - startAddr) + (ulong)ExactScanner.DefaultBlockSize - 1) / (ulong)ExactScanner.DefaultBlockSize);
            if (totalBlocks <= 0)
                totalBlocks = 1;

            SetProgBarMax(totalBlocks - 1);
            SetProgBar(0);

            MemoryReader reader = new MemoryReader();
            ExactScanRequest request = new ExactScanRequest();
            request.Start = startAddr;
            request.Stop = stopAddr;
            request.BlockSize = ExactScanner.DefaultBlockSize;
            request.ByteSize = type.ByteSize;
            request.Alignment = alignment;
            request.CompareBytes = compareBytes;
            activeScanUsesNewEngine = true;
            activeScanLittleEndian = IsLittleEndianModeChecked();
            RefreshNewScannerFilterOptions();
            request.EndianMode = activeScanLittleEndian ? EndianMode.Little : EndianMode.Big;
            request.KeepRawBytes = false;

            List<SearchListView.SearchListViewItem> batch = new List<SearchListView.SearchListViewItem>(512);
            int resCnt = 0;

            ExactScanner.Scan(
                request,
                reader,
                delegate () { return _shouldStopSearch; },
                delegate (int blockIndex)
                {
                    if ((blockIndex & 0x0F) == 0)
                    {
                        SetProgBar(blockIndex);
                        SetStatusLabel("Results: " + resCnt.ToString("N0"));
                    }
                },
                delegate (ulong addr, byte[] displayBytes)
                {
                    if (!NewScanner_ShouldKeepDisplayedValue(displayBytes, typeIndex, false))
                        return;

                    batch.Add(type.ToItem(addr, displayBytes, new byte[0], typeIndex));
                    resCnt++;

                    if (batch.Count >= 512)
                    {
                        AddResultRange(batch);
                        batch.Clear();
                    }
                });

            if (batch.Count > 0)
            {
                AddResultRange(batch);
                batch.Clear();
            }

            Invoke((MethodInvoker)delegate
            {
                searchListView1.AddItemsFromList();
            });

            SetProgBar(totalBlocks - 1);
            SetStatusLabel("Results: " + resCnt.ToString("N0"));
        }

        private byte[] NewScanner_ValueBytesToRaw(byte[] valueBytes, int byteSize)
        {
            byte[] raw = new byte[byteSize];

            if (valueBytes != null)
            {
                int count = Math.Min(valueBytes.Length, byteSize);
                Buffer.BlockCopy(valueBytes, 0, raw, 0, count);
            }

            if (activeScanLittleEndian && byteSize > 1)
                Array.Reverse(raw);

            return raw;
        }

        private byte[] NewScanner_RawBytesToValue(byte[] rawBytes, int byteSize)
        {
            byte[] value = new byte[byteSize];

            if (rawBytes != null)
            {
                int count = Math.Min(rawBytes.Length, byteSize);
                Buffer.BlockCopy(rawBytes, 0, value, 0, count);
            }

            if (activeScanLittleEndian && byteSize > 1)
                Array.Reverse(value);

            return value;
        }

        private bool NewScanner_BytesEqual(byte[] a, byte[] b, int size)
        {
            if (a == null || b == null)
                return false;
            if (a.Length < size || b.Length < size)
                return false;

            for (int i = 0; i < size; i++)
            {
                if (a[i] != b[i])
                    return false;
            }

            return true;
        }

        private bool NewScanner_TryReadCurrentRawValue(
            MemoryReader reader,
            ulong addr,
            int byteSize,
            ref ulong cachedBlockBase,
            ref byte[] cachedBlock,
            ref bool cachedBlockValid,
            out byte[] rawValue)
        {
            rawValue = null;

            if (reader == null)
                return false;

            int blockSize = ExactScanner.DefaultBlockSize;
            ulong blockBase = addr - (addr % (ulong)blockSize);
            int offset = (int)(addr - blockBase);

            if (!cachedBlockValid || cachedBlockBase != blockBase)
            {
                cachedBlockBase = blockBase;
                cachedBlockValid = reader.TryReadBlock(cachedBlockBase, cachedBlock);
            }

            if (cachedBlockValid && offset >= 0 && (offset + byteSize) <= cachedBlock.Length)
            {
                rawValue = new byte[byteSize];
                Buffer.BlockCopy(cachedBlock, offset, rawValue, 0, byteSize);
                return true;
            }

            byte[] direct = new byte[byteSize];
            if (reader.TryReadBlock(addr, direct))
            {
                rawValue = direct;
                return true;
            }

            return false;
        }

        private void RunNewBasicNextSearch(SearchListView.SearchListViewItem[] old, string[] args, int nextMode)
        {
            RefreshNewScannerFilterOptions();

            if (old == null || old.Length == 0)
            {
                SetProgBarMax(1);
                SetProgBar(0);
                SetStatusLabel("Results: 0");

                Invoke((MethodInvoker)delegate
                {
                    searchListView1.AddItemsFromList();
                });

                return;
            }

            SetProgBarMax(old.Length - 1);
            SetProgBar(0);

            ClearItems();

            MemoryReader reader = new MemoryReader();
            byte[] cachedBlock = new byte[ExactScanner.DefaultBlockSize];
            ulong cachedBlockBase = 0;
            bool cachedBlockValid = false;

            List<SearchListView.SearchListViewItem> batch = new List<SearchListView.SearchListViewItem>(512);
            int resCnt = 0;

            byte[] equalCompareRaw = null;
            int equalCompareAlign = -1;

            for (int cnt = 0; cnt < old.Length; cnt++)
            {
                if (_shouldStopSearch)
                    break;

                SearchListView.SearchListViewItem oldItem = old[cnt];
                ncSearchType type = SearchTypes[oldItem.align];
                int byteSize = type.ByteSize;

                if (byteSize <= 0)
                    continue;

                byte[] currentRaw;
                bool readOk = NewScanner_TryReadCurrentRawValue(
                    reader,
                    oldItem.addr,
                    byteSize,
                    ref cachedBlockBase,
                    ref cachedBlock,
                    ref cachedBlockValid,
                    out currentRaw);

                if (!readOk)
                    continue;

                bool isMatch = false;

                if (nextMode == Form1.compEq || nextMode == Form1.compNEq)
                {
                    if (equalCompareRaw == null || equalCompareAlign != oldItem.align)
                    {
                        byte[] valueBytes = type.ToByteArray((string)args[0]);
                        equalCompareRaw = NewScanner_ValueBytesToRaw(valueBytes, byteSize);
                        equalCompareAlign = oldItem.align;
                    }

                    bool equal = NewScanner_BytesEqual(currentRaw, equalCompareRaw, byteSize);
                    isMatch = nextMode == Form1.compEq ? equal : !equal;
                }
                else if (nextMode == Form1.compChg)
                {
                    byte[] oldRaw = NewScanner_ValueBytesToRaw(oldItem.newVal, byteSize);
                    isMatch = !NewScanner_BytesEqual(currentRaw, oldRaw, byteSize);
                }
                else if (nextMode == Form1.compUChg)
                {
                    byte[] oldRaw = NewScanner_ValueBytesToRaw(oldItem.newVal, byteSize);
                    isMatch = NewScanner_BytesEqual(currentRaw, oldRaw, byteSize);
                }

                if (isMatch)
                {
                    byte[] newValue = NewScanner_RawBytesToValue(currentRaw, byteSize);

                    if (!NewScanner_ShouldKeepDisplayedValue(newValue, oldItem.align, false))
                        continue;

                    SearchListView.SearchListViewItem newItem = oldItem;
                    newItem.oldVal = oldItem.newVal;
                    newItem.newVal = newValue;
                    batch.Add(newItem);
                    resCnt++;

                    if (batch.Count >= 512)
                    {
                        AddResultRange(batch);
                        batch.Clear();
                    }
                }

                if ((cnt & 0x3FF) == 0)
                {
                    SetProgBar(cnt);
                    SetStatusLabel("Results: " + resCnt.ToString("N0"));
                }
            }

            if (batch.Count > 0)
            {
                AddResultRange(batch);
                batch.Clear();
            }

            Invoke((MethodInvoker)delegate
            {
                searchListView1.AddItemsFromList();
            });

            SetProgBar(old.Length - 1);
            SetStatusLabel(
                "Results: " + resCnt.ToString("N0") +
                " | Reads OK: " + reader.Stats.ReadSuccesses.ToString("N0") +
                " | Failed: " + reader.Stats.ReadFailures.ToString("N0"));
        }

        private long NewScanner_ToSignedInteger(byte[] valueBytes, int byteSize)
        {
            if (valueBytes == null || valueBytes.Length < byteSize)
                return 0;

            ulong raw = 0;
            for (int i = 0; i < byteSize; i++)
                raw = (raw << 8) | valueBytes[i];

            switch (byteSize)
            {
                case 1:
                    return unchecked((sbyte)raw);
                case 2:
                    return unchecked((short)raw);
                case 4:
                    return unchecked((int)raw);
                case 8:
                    return unchecked((long)raw);
            }

            return unchecked((long)raw);
        }

        private float NewScanner_ToFloat(byte[] valueBytes)
        {
            if (valueBytes == null || valueBytes.Length < 4)
                return 0.0f;

            byte[] temp = new byte[4];
            Buffer.BlockCopy(valueBytes, 0, temp, 0, 4);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(temp);

            return BitConverter.ToSingle(temp, 0);
        }

        private double NewScanner_ToDouble(byte[] valueBytes)
        {
            if (valueBytes == null || valueBytes.Length < 8)
                return 0.0;

            byte[] temp = new byte[8];
            Buffer.BlockCopy(valueBytes, 0, temp, 0, 8);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(temp);

            return BitConverter.ToDouble(temp, 0);
        }

        private bool NewScanner_IsDirectionMatch(byte[] previousValue, byte[] currentValue, int typeIndex, bool increased)
        {
            if (previousValue == null || currentValue == null)
                return false;

            ncSearchType type = SearchTypes[typeIndex];
            string typeName = type.Name;

            if (typeName == "Float")
            {
                float oldFloat = NewScanner_ToFloat(previousValue);
                float newFloat = NewScanner_ToFloat(currentValue);

                if (float.IsNaN(oldFloat) || float.IsNaN(newFloat))
                    return false;

                return increased ? newFloat > oldFloat : newFloat < oldFloat;
            }

            if (typeName == "Double")
            {
                double oldDouble = NewScanner_ToDouble(previousValue);
                double newDouble = NewScanner_ToDouble(currentValue);

                if (double.IsNaN(oldDouble) || double.IsNaN(newDouble))
                    return false;

                return increased ? newDouble > oldDouble : newDouble < oldDouble;
            }

            long oldInt = NewScanner_ToSignedInteger(previousValue, type.ByteSize);
            long newInt = NewScanner_ToSignedInteger(currentValue, type.ByteSize);

            return increased ? newInt > oldInt : newInt < oldInt;
        }

        private void RunNewDirectionNextSearch(SearchListView.SearchListViewItem[] old, bool increased)
        {
            RefreshNewScannerFilterOptions();

            if (old == null || old.Length == 0)
            {
                SetProgBarMax(1);
                SetProgBar(0);
                SetStatusLabel("Results: 0");

                Invoke((MethodInvoker)delegate
                {
                    searchListView1.AddItemsFromList();
                });

                return;
            }

            SetProgBarMax(old.Length - 1);
            SetProgBar(0);

            ClearItems();

            MemoryReader reader = new MemoryReader();
            byte[] cachedBlock = new byte[ExactScanner.DefaultBlockSize];
            ulong cachedBlockBase = 0;
            bool cachedBlockValid = false;

            List<SearchListView.SearchListViewItem> batch = new List<SearchListView.SearchListViewItem>(512);
            int resCnt = 0;

            for (int cnt = 0; cnt < old.Length; cnt++)
            {
                if (_shouldStopSearch)
                    break;

                SearchListView.SearchListViewItem oldItem = old[cnt];
                ncSearchType type = SearchTypes[oldItem.align];
                int byteSize = type.ByteSize;

                if (byteSize <= 0)
                    continue;

                byte[] currentRaw;
                bool readOk = NewScanner_TryReadCurrentRawValue(
                    reader,
                    oldItem.addr,
                    byteSize,
                    ref cachedBlockBase,
                    ref cachedBlock,
                    ref cachedBlockValid,
                    out currentRaw);

                if (!readOk)
                    continue;

                byte[] currentValue = NewScanner_RawBytesToValue(currentRaw, byteSize);

                if (NewScanner_IsDirectionMatch(oldItem.newVal, currentValue, oldItem.align, increased))
                {
                    if (!NewScanner_ShouldKeepDisplayedValue(currentValue, oldItem.align, false))
                        continue;

                    SearchListView.SearchListViewItem newItem = oldItem;
                    newItem.oldVal = oldItem.newVal;
                    newItem.newVal = currentValue;
                    batch.Add(newItem);
                    resCnt++;

                    if (batch.Count >= 512)
                    {
                        AddResultRange(batch);
                        batch.Clear();
                    }
                }

                if ((cnt & 0x3FF) == 0)
                {
                    SetProgBar(cnt);
                    SetStatusLabel("Results: " + resCnt.ToString("N0"));
                }
            }

            if (batch.Count > 0)
            {
                AddResultRange(batch);
                batch.Clear();
            }

            Invoke((MethodInvoker)delegate
            {
                searchListView1.AddItemsFromList();
            });

            SetProgBar(old.Length - 1);
            SetStatusLabel(
                "Results: " + resCnt.ToString("N0") +
                " | Reads OK: " + reader.Stats.ReadSuccesses.ToString("N0") +
                " | Failed: " + reader.Stats.ReadFailures.ToString("N0"));
        }

        private ulong NewScanner_ToUnsignedInteger(byte[] valueBytes, int byteSize)
        {
            if (valueBytes == null || valueBytes.Length < byteSize)
                return 0;

            ulong raw = 0;
            for (int i = 0; i < byteSize; i++)
                raw = (raw << 8) | valueBytes[i];

            return raw;
        }

        private bool NewScanner_AreDoublesClose(double a, double b)
        {
            double diff = Math.Abs(a - b);
            double scale = Math.Max(1.0, Math.Max(Math.Abs(a), Math.Abs(b)));
            return diff <= (scale * 0.000001);
        }

        private bool NewScanner_IsDeltaMatch(byte[] previousValue, byte[] currentValue, byte[] deltaValue, int typeIndex, bool increased)
        {
            if (previousValue == null || currentValue == null || deltaValue == null)
                return false;

            ncSearchType type = SearchTypes[typeIndex];
            string typeName = type.Name;

            if (typeName == "Float")
            {
                float oldFloat = NewScanner_ToFloat(previousValue);
                float newFloat = NewScanner_ToFloat(currentValue);
                float deltaFloat = NewScanner_ToFloat(deltaValue);

                if (float.IsNaN(oldFloat) || float.IsNaN(newFloat) || float.IsNaN(deltaFloat))
                    return false;

                double actual = increased ? (newFloat - oldFloat) : (oldFloat - newFloat);
                if (actual < 0.0)
                    return false;

                return NewScanner_AreDoublesClose(actual, deltaFloat);
            }

            if (typeName == "Double")
            {
                double oldDouble = NewScanner_ToDouble(previousValue);
                double newDouble = NewScanner_ToDouble(currentValue);
                double deltaDouble = NewScanner_ToDouble(deltaValue);

                if (double.IsNaN(oldDouble) || double.IsNaN(newDouble) || double.IsNaN(deltaDouble))
                    return false;

                double actual = increased ? (newDouble - oldDouble) : (oldDouble - newDouble);
                if (actual < 0.0)
                    return false;

                return NewScanner_AreDoublesClose(actual, deltaDouble);
            }

            ulong oldInt = NewScanner_ToUnsignedInteger(previousValue, type.ByteSize);
            ulong newInt = NewScanner_ToUnsignedInteger(currentValue, type.ByteSize);
            ulong deltaInt = NewScanner_ToUnsignedInteger(deltaValue, type.ByteSize);

            if (increased)
            {
                if (newInt < oldInt)
                    return false;

                return (newInt - oldInt) == deltaInt;
            }
            else
            {
                if (oldInt < newInt)
                    return false;

                return (oldInt - newInt) == deltaInt;
            }
        }

        private void RunNewDeltaNextSearch(SearchListView.SearchListViewItem[] old, string[] args, bool increased)
        {
            RefreshNewScannerFilterOptions();

            if (old == null || old.Length == 0 || args == null || args.Length == 0)
            {
                SetProgBarMax(1);
                SetProgBar(0);
                SetStatusLabel("Results: 0");

                Invoke((MethodInvoker)delegate
                {
                    searchListView1.AddItemsFromList();
                });

                return;
            }

            SetProgBarMax(old.Length - 1);
            SetProgBar(0);

            ClearItems();

            MemoryReader reader = new MemoryReader();
            byte[] cachedBlock = new byte[ExactScanner.DefaultBlockSize];
            ulong cachedBlockBase = 0;
            bool cachedBlockValid = false;

            List<SearchListView.SearchListViewItem> batch = new List<SearchListView.SearchListViewItem>(512);
            int resCnt = 0;

            byte[] deltaValue = null;
            int deltaAlign = -1;

            for (int cnt = 0; cnt < old.Length; cnt++)
            {
                if (_shouldStopSearch)
                    break;

                SearchListView.SearchListViewItem oldItem = old[cnt];
                ncSearchType type = SearchTypes[oldItem.align];
                int byteSize = type.ByteSize;

                if (byteSize <= 0)
                    continue;

                if (deltaValue == null || deltaAlign != oldItem.align)
                {
                    deltaValue = type.ToByteArray((string)args[0]);
                    deltaAlign = oldItem.align;
                }

                byte[] currentRaw;
                bool readOk = NewScanner_TryReadCurrentRawValue(
                    reader,
                    oldItem.addr,
                    byteSize,
                    ref cachedBlockBase,
                    ref cachedBlock,
                    ref cachedBlockValid,
                    out currentRaw);

                if (!readOk)
                    continue;

                byte[] currentValue = NewScanner_RawBytesToValue(currentRaw, byteSize);

                if (NewScanner_IsDeltaMatch(oldItem.newVal, currentValue, deltaValue, oldItem.align, increased))
                {
                    if (!NewScanner_ShouldKeepDisplayedValue(currentValue, oldItem.align, false))
                        continue;

                    SearchListView.SearchListViewItem newItem = oldItem;
                    newItem.oldVal = oldItem.newVal;
                    newItem.newVal = currentValue;
                    batch.Add(newItem);
                    resCnt++;

                    if (batch.Count >= 512)
                    {
                        AddResultRange(batch);
                        batch.Clear();
                    }
                }

                if ((cnt & 0x3FF) == 0)
                {
                    SetProgBar(cnt);
                    SetStatusLabel("Results: " + resCnt.ToString("N0"));
                }
            }

            if (batch.Count > 0)
            {
                AddResultRange(batch);
                batch.Clear();
            }

            Invoke((MethodInvoker)delegate
            {
                searchListView1.AddItemsFromList();
            });

            SetProgBar(old.Length - 1);
            SetStatusLabel(
                "Results: " + resCnt.ToString("N0") +
                " | Reads OK: " + reader.Stats.ReadSuccesses.ToString("N0") +
                " | Failed: " + reader.Stats.ReadFailures.ToString("N0"));
        }

        private ulong NewScanner_ToUnsignedIntegerFromRaw(byte[] rawBytes, int offset, int byteSize)
        {
            if (rawBytes == null || offset < 0 || byteSize <= 0 || (offset + byteSize) > rawBytes.Length)
                return 0;

            ulong value = 0;

            if (activeScanLittleEndian && byteSize > 1)
            {
                for (int i = byteSize - 1; i >= 0; i--)
                    value = (value << 8) | rawBytes[offset + i];
            }
            else
            {
                for (int i = 0; i < byteSize; i++)
                    value = (value << 8) | rawBytes[offset + i];
            }

            return value;
        }

        private double NewScanner_ToFloatingFromRaw(byte[] rawBytes, int offset, int byteSize, byte[] scratch)
        {
            if (rawBytes == null || scratch == null)
                return 0.0;
            if (offset < 0 || byteSize <= 0 || (offset + byteSize) > rawBytes.Length)
                return 0.0;
            if (scratch.Length < byteSize)
                return 0.0;

            bool rawIsLittleEndian = activeScanLittleEndian;

            if (BitConverter.IsLittleEndian)
            {
                if (rawIsLittleEndian)
                {
                    Buffer.BlockCopy(rawBytes, offset, scratch, 0, byteSize);
                }
                else
                {
                    for (int i = 0; i < byteSize; i++)
                        scratch[i] = rawBytes[offset + (byteSize - 1 - i)];
                }
            }
            else
            {
                if (rawIsLittleEndian)
                {
                    for (int i = 0; i < byteSize; i++)
                        scratch[i] = rawBytes[offset + (byteSize - 1 - i)];
                }
                else
                {
                    Buffer.BlockCopy(rawBytes, offset, scratch, 0, byteSize);
                }
            }

            if (byteSize == 4)
                return BitConverter.ToSingle(scratch, 0);

            if (byteSize == 8)
                return BitConverter.ToDouble(scratch, 0);

            return 0.0;
        }

        private bool NewScanner_ShouldKeepDisplayedValue(byte[] valueBytes, int typeIndex, bool applyNoZeroFilter)
        {
            if (valueBytes == null)
                return false;

            ncSearchType type = SearchTypes[typeIndex];
            string typeName = type.Name;

            bool noNegative = activeFilterNoNegative;
            bool cleanFloat = activeFilterCleanFloat;
            bool noZero = applyNoZeroFilter && activeFilterNoZero;

            if (typeName == "Float")
            {
                float value = NewScanner_ToFloat(valueBytes);

                if (float.IsNaN(value) || float.IsInfinity(value))
                    return false;

                if (noZero && value == 0.0f)
                    return false;

                if (noNegative && value < 0.0f)
                    return false;

                if (cleanFloat)
                {
                    double abs = Math.Abs(value);

                    // Keep useful normal values, remove tiny float noise like 1.04425E-05.
                    if (value != 0.0f && abs < CleanFloatMinAbs)
                        return false;

                    // Remove huge scientific-notation garbage like 3.355456E+07.
                    if (abs > CleanFloatMaxAbs)
                        return false;
                }

                return true;
            }

            if (typeName == "Double")
            {
                double value = NewScanner_ToDouble(valueBytes);

                if (double.IsNaN(value) || double.IsInfinity(value))
                    return false;

                if (noZero && value == 0.0)
                    return false;

                if (noNegative && value < 0.0)
                    return false;

                if (cleanFloat)
                {
                    double abs = Math.Abs(value);

                    // Keep useful normal values, remove tiny double noise.
                    if (value != 0.0 && abs < CleanFloatMinAbs)
                        return false;

                    // Remove huge scientific-notation garbage.
                    if (abs > CleanFloatMaxAbs)
                        return false;
                }

                return true;
            }

            if (noZero)
            {
                ulong unsignedValue = NewScanner_ToUnsignedInteger(valueBytes, type.ByteSize);
                if (unsignedValue == 0)
                    return false;
            }

            if (noNegative && type.ByteSize > 1)
            {
                long signedValue = NewScanner_ToSignedInteger(valueBytes, type.ByteSize);
                if (signedValue < 0)
                    return false;
            }

            return true;
        }

        private bool NewScanner_IsValueBetween(byte[] valueBytes, byte[] minBytes, byte[] maxBytes, int typeIndex)
        {
            if (valueBytes == null || minBytes == null || maxBytes == null)
                return false;

            ncSearchType type = SearchTypes[typeIndex];
            string typeName = type.Name;

            if (typeName == "Float")
            {
                double value = NewScanner_ToFloat(valueBytes);
                double min = NewScanner_ToFloat(minBytes);
                double max = NewScanner_ToFloat(maxBytes);

                if (double.IsNaN(value) || double.IsNaN(min) || double.IsNaN(max))
                    return false;

                if (min > max)
                {
                    double tmp = min;
                    min = max;
                    max = tmp;
                }

                return value >= min && value <= max;
            }

            if (typeName == "Double")
            {
                double value = NewScanner_ToDouble(valueBytes);
                double min = NewScanner_ToDouble(minBytes);
                double max = NewScanner_ToDouble(maxBytes);

                if (double.IsNaN(value) || double.IsNaN(min) || double.IsNaN(max))
                    return false;

                if (min > max)
                {
                    double tmp = min;
                    min = max;
                    max = tmp;
                }

                return value >= min && value <= max;
            }

            ulong uintValue = NewScanner_ToUnsignedInteger(valueBytes, type.ByteSize);
            ulong uintMin = NewScanner_ToUnsignedInteger(minBytes, type.ByteSize);
            ulong uintMax = NewScanner_ToUnsignedInteger(maxBytes, type.ByteSize);

            if (uintMin > uintMax)
            {
                ulong tmp = uintMin;
                uintMin = uintMax;
                uintMax = tmp;
            }

            return uintValue >= uintMin && uintValue <= uintMax;
        }

        private bool NewScanner_IsRawValueBetween(
            byte[] rawBytes,
            int offset,
            int byteSize,
            int typeIndex,
            byte[] minBytes,
            byte[] maxBytes,
            byte[] scratch)
        {
            if (rawBytes == null || minBytes == null || maxBytes == null)
                return false;

            ncSearchType type = SearchTypes[typeIndex];
            string typeName = type.Name;

            if (typeName == "Float")
            {
                double value = NewScanner_ToFloatingFromRaw(rawBytes, offset, 4, scratch);
                double min = NewScanner_ToFloat(minBytes);
                double max = NewScanner_ToFloat(maxBytes);

                if (double.IsNaN(value) || double.IsNaN(min) || double.IsNaN(max))
                    return false;

                if (min > max)
                {
                    double tmp = min;
                    min = max;
                    max = tmp;
                }

                return value >= min && value <= max;
            }

            if (typeName == "Double")
            {
                double value = NewScanner_ToFloatingFromRaw(rawBytes, offset, 8, scratch);
                double min = NewScanner_ToDouble(minBytes);
                double max = NewScanner_ToDouble(maxBytes);

                if (double.IsNaN(value) || double.IsNaN(min) || double.IsNaN(max))
                    return false;

                if (min > max)
                {
                    double tmp = min;
                    min = max;
                    max = tmp;
                }

                return value >= min && value <= max;
            }

            ulong valueInt = NewScanner_ToUnsignedIntegerFromRaw(rawBytes, offset, byteSize);
            ulong minInt = NewScanner_ToUnsignedInteger(minBytes, byteSize);
            ulong maxInt = NewScanner_ToUnsignedInteger(maxBytes, byteSize);

            if (minInt > maxInt)
            {
                ulong tmp = minInt;
                minInt = maxInt;
                maxInt = tmp;
            }

            return valueInt >= minInt && valueInt <= maxInt;
        }

        private void RunNewValueBetweenInitialSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args)
        {
            if (args == null || args.Length < 2)
            {
                SetStatusLabel("Value Between requires min and max values");
                return;
            }

            ncSearchType type = SearchTypes[typeIndex];

            type.Initialize((string)args[0], typeIndex);
            type = SearchTypes[typeIndex];

            int alignment = type.ignoreAlignment ? GetAlign(1) : type.ByteSize;
            if (alignment <= 0)
                alignment = type.ByteSize;

            activeScanUsesNewEngine = true;
            activeScanLittleEndian = IsLittleEndianModeChecked();
            RefreshNewScannerFilterOptions();

            byte[] minBytes = type.ToByteArray((string)args[0]);
            byte[] maxBytes = type.ToByteArray((string)args[1]);

            int blockSize = ExactScanner.DefaultBlockSize;
            int totalBlocks = (int)(((stopAddr - startAddr) + (ulong)blockSize - 1) / (ulong)blockSize);
            if (totalBlocks <= 0)
                totalBlocks = 1;

            SetProgBarMax(totalBlocks - 1);
            SetProgBar(0);

            MemoryReader reader = new MemoryReader();
            byte[] fullBlock = new byte[blockSize];
            byte[] floatScratch = new byte[8];

            List<SearchListView.SearchListViewItem> batch = new List<SearchListView.SearchListViewItem>(512);
            int resCnt = 0;
            int blockIndex = 0;

            for (ulong addr = startAddr; addr < stopAddr; addr += (ulong)blockSize, blockIndex++)
            {
                if (_shouldStopSearch)
                    break;

                int usable = (int)Math.Min((ulong)blockSize, stopAddr - addr);
                if (usable <= 0)
                    break;

                byte[] readBlock = usable == blockSize ? fullBlock : new byte[usable];

                if (reader.TryReadBlock(addr, readBlock))
                {
                    int maxOffset = usable - type.ByteSize;

                    for (int off = 0; off <= maxOffset; off += alignment)
                    {
                        if (_shouldStopSearch)
                            break;

                        if (NewScanner_IsRawValueBetween(readBlock, off, type.ByteSize, typeIndex, minBytes, maxBytes, floatScratch))
                        {
                            byte[] hitRaw = new byte[type.ByteSize];
                            Buffer.BlockCopy(readBlock, off, hitRaw, 0, type.ByteSize);

                            byte[] displayBytes = NewScanner_RawBytesToValue(hitRaw, type.ByteSize);

                            if (!NewScanner_ShouldKeepDisplayedValue(displayBytes, typeIndex, false))
                                continue;

                            batch.Add(type.ToItem(addr + (ulong)off, displayBytes, new byte[0], typeIndex));
                            resCnt++;

                            if (batch.Count >= 512)
                            {
                                AddResultRange(batch);
                                batch.Clear();
                            }
                        }
                    }
                }

                if ((blockIndex & 0x0F) == 0)
                {
                    SetProgBar(blockIndex);
                    SetStatusLabel("Results: " + resCnt.ToString("N0"));
                }
            }

            if (batch.Count > 0)
            {
                AddResultRange(batch);
                batch.Clear();
            }

            Invoke((MethodInvoker)delegate
            {
                searchListView1.AddItemsFromList();
            });

            SetProgBar(totalBlocks - 1);
            SetStatusLabel(
                "Results: " + resCnt.ToString("N0") +
                " | Reads OK: " + reader.Stats.ReadSuccesses.ToString("N0") +
                " | Failed: " + reader.Stats.ReadFailures.ToString("N0"));
        }

        private void RunNewValueBetweenNextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            RefreshNewScannerFilterOptions();

            if (old == null || old.Length == 0 || args == null || args.Length < 2)
            {
                SetProgBarMax(1);
                SetProgBar(0);
                SetStatusLabel("Results: 0");

                Invoke((MethodInvoker)delegate
                {
                    searchListView1.AddItemsFromList();
                });

                return;
            }

            SetProgBarMax(old.Length - 1);
            SetProgBar(0);

            ClearItems();

            MemoryReader reader = new MemoryReader();
            byte[] cachedBlock = new byte[ExactScanner.DefaultBlockSize];
            ulong cachedBlockBase = 0;
            bool cachedBlockValid = false;

            List<SearchListView.SearchListViewItem> batch = new List<SearchListView.SearchListViewItem>(512);
            int resCnt = 0;

            byte[] minBytes = null;
            byte[] maxBytes = null;
            int cachedAlign = -1;

            for (int cnt = 0; cnt < old.Length; cnt++)
            {
                if (_shouldStopSearch)
                    break;

                SearchListView.SearchListViewItem oldItem = old[cnt];
                ncSearchType type = SearchTypes[oldItem.align];
                int byteSize = type.ByteSize;

                if (byteSize <= 0)
                    continue;

                if (minBytes == null || maxBytes == null || cachedAlign != oldItem.align)
                {
                    minBytes = type.ToByteArray((string)args[0]);
                    maxBytes = type.ToByteArray((string)args[1]);
                    cachedAlign = oldItem.align;
                }

                byte[] currentRaw;
                bool readOk = NewScanner_TryReadCurrentRawValue(
                    reader,
                    oldItem.addr,
                    byteSize,
                    ref cachedBlockBase,
                    ref cachedBlock,
                    ref cachedBlockValid,
                    out currentRaw);

                if (!readOk)
                    continue;

                byte[] currentValue = NewScanner_RawBytesToValue(currentRaw, byteSize);

                if (NewScanner_IsValueBetween(currentValue, minBytes, maxBytes, oldItem.align))
                {
                    if (!NewScanner_ShouldKeepDisplayedValue(currentValue, oldItem.align, false))
                        continue;

                    SearchListView.SearchListViewItem newItem = oldItem;
                    newItem.oldVal = oldItem.newVal;
                    newItem.newVal = currentValue;
                    batch.Add(newItem);
                    resCnt++;

                    if (batch.Count >= 512)
                    {
                        AddResultRange(batch);
                        batch.Clear();
                    }
                }

                if ((cnt & 0x3FF) == 0)
                {
                    SetProgBar(cnt);
                    SetStatusLabel("Results: " + resCnt.ToString("N0"));
                }
            }

            if (batch.Count > 0)
            {
                AddResultRange(batch);
                batch.Clear();
            }

            Invoke((MethodInvoker)delegate
            {
                searchListView1.AddItemsFromList();
            });

            SetProgBar(old.Length - 1);
            SetStatusLabel(
                "Results: " + resCnt.ToString("N0") +
                " | Reads OK: " + reader.Stats.ReadSuccesses.ToString("N0") +
                " | Failed: " + reader.Stats.ReadFailures.ToString("N0"));
        }

        private void RunNewUnknownInitialSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args)
        {
            ncSearchType type = SearchTypes[typeIndex];

            int alignment = type.ignoreAlignment ? GetAlign(1) : type.ByteSize;
            if (alignment <= 0)
                alignment = type.ByteSize;

            activeScanUsesNewEngine = true;
            activeScanLittleEndian = IsLittleEndianModeChecked();
            RefreshNewScannerFilterOptions();

            activeSnapshotTypeIndex = typeIndex;
            activeSnapshotByteSize = type.ByteSize;
            activeSnapshotResultCount = 0;

            int blockSize = ExactScanner.DefaultBlockSize;
            int totalBlocks = (int)(((stopAddr - startAddr) + (ulong)blockSize - 1) / (ulong)blockSize);
            if (totalBlocks <= 0)
                totalBlocks = 1;

            long estimatedResults = 0;
            if (type.ByteSize > 0)
                estimatedResults = (long)((stopAddr - startAddr) / (ulong)alignment);

            string msg =
                "This Unknown Value scan will write a disk-backed snapshot.\r\n\r\n" +
                "Estimated records: " + estimatedResults.ToString("N0") + "\r\n" +
                "Only the first " + MaxVisibleSnapshotResults.ToString("N0") + " results will be shown in the UI.\r\n" +
                "All records will still be kept on disk for Next Scan.\r\n\r\n" +
                "Continue?";

            if (MessageBox.Show(msg, "Unknown Value Snapshot", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.No)
                return;

            SetProgBarMax(totalBlocks - 1);
            SetProgBar(0);

            string snapshotPath = CreateSnapshotPath();
            MemoryReader reader = new MemoryReader();
            byte[] fullBlock = new byte[blockSize];

            List<SearchListView.SearchListViewItem> batch = new List<SearchListView.SearchListViewItem>(512);
            int visibleCount = 0;
            long totalCount = 0;
            int blockIndex = 0;

            using (SnapshotStore store = SnapshotStore.Create(snapshotPath, typeIndex, type.ByteSize, activeScanLittleEndian))
            {
                for (ulong addr = startAddr; addr < stopAddr; addr += (ulong)blockSize, blockIndex++)
                {
                    if (_shouldStopSearch)
                        break;

                    int usable = (int)Math.Min((ulong)blockSize, stopAddr - addr);
                    if (usable <= 0)
                        break;

                    byte[] readBlock = usable == blockSize ? fullBlock : new byte[usable];

                    if (reader.TryReadBlock(addr, readBlock))
                    {
                        int maxOffset = usable - type.ByteSize;

                        for (int off = 0; off <= maxOffset; off += alignment)
                        {
                            if (_shouldStopSearch)
                                break;

                            byte[] raw = new byte[type.ByteSize];
                            Buffer.BlockCopy(readBlock, off, raw, 0, type.ByteSize);

                            byte[] valueBytes = NewScanner_RawBytesToValue(raw, type.ByteSize);

                            if (!NewScanner_ShouldKeepDisplayedValue(valueBytes, typeIndex, true))
                                continue;

                            store.WriteRecord(addr + (ulong)off, valueBytes);
                            totalCount++;

                            if (visibleCount < MaxVisibleSnapshotResults)
                            {
                                batch.Add(type.ToItem(addr + (ulong)off, valueBytes, new byte[0], typeIndex));
                                visibleCount++;

                                if (batch.Count >= 512)
                                {
                                    AddResultRange(batch);
                                    batch.Clear();
                                }
                            }
                        }
                    }

                    if ((blockIndex & 0x0F) == 0)
                    {
                        SetProgBar(blockIndex);
                        SetStatusLabel("Snapshot records: " + totalCount.ToString("N0") + " | Visible: " + visibleCount.ToString("N0"));
                    }
                }
            }

            if (batch.Count > 0)
            {
                AddResultRange(batch);
                batch.Clear();
            }

            activeSnapshotPath = snapshotPath;
            activeSnapshotResultCount = totalCount;

            Invoke((MethodInvoker)delegate
            {
                searchListView1.AddItemsFromList();
            });

            SetProgBar(totalBlocks - 1);
            SetStatusLabel(
                "Snapshot records: " + totalCount.ToString("N0") +
                " | Visible: " + visibleCount.ToString("N0") +
                " | Reads OK: " + reader.Stats.ReadSuccesses.ToString("N0") +
                " | Failed: " + reader.Stats.ReadFailures.ToString("N0"));
        }

        private void RunNewSnapshotNextSearch(string[] args, int mode)
        {
            if (!HasActiveSnapshot())
            {
                SetStatusLabel("No active snapshot");
                return;
            }

            SnapshotHeader header = SnapshotStore.ReadHeader(activeSnapshotPath);

            activeScanUsesNewEngine = true;
            activeScanLittleEndian = header.LittleEndian;
            RefreshNewScannerFilterOptions();

            activeSnapshotTypeIndex = header.TypeIndex;
            activeSnapshotByteSize = header.ByteSize;

            ncSearchType type = SearchTypes[header.TypeIndex];

            SetProgBarMax(header.Count > 0 ? (int)Math.Min(header.Count - 1, Int32.MaxValue) : 1);
            SetProgBar(0);

            ClearItems();

            string newSnapshotPath = CreateSnapshotPath();

            MemoryReader reader = new MemoryReader();
            byte[] cachedBlock = new byte[ExactScanner.DefaultBlockSize];
            ulong cachedBlockBase = 0;
            bool cachedBlockValid = false;

            List<SearchListView.SearchListViewItem> batch = new List<SearchListView.SearchListViewItem>(512);
            int visibleCount = 0;
            long resultCount = 0;
            long processed = 0;

            byte[] compareValue = null;
            byte[] minValue = null;
            byte[] maxValue = null;

            if ((mode == Form1.compEq || mode == Form1.compNEq) && args != null && args.Length > 0)
                compareValue = type.ToByteArray((string)args[0]);

            if (mode == Form1.compVBet && args != null && args.Length >= 2)
            {
                minValue = type.ToByteArray((string)args[0]);
                maxValue = type.ToByteArray((string)args[1]);
            }

            byte[] deltaValue = null;
            if ((mode == Form1.compINC || mode == Form1.compDEC) && args != null && args.Length > 0)
                deltaValue = type.ToByteArray((string)args[0]);

            using (SnapshotStore newStore = SnapshotStore.Create(newSnapshotPath, header.TypeIndex, header.ByteSize, header.LittleEndian))
            {
                foreach (SnapshotRecord record in SnapshotStore.ReadRecords(activeSnapshotPath))
                {
                    if (_shouldStopSearch)
                        break;

                    byte[] currentRaw;
                    bool readOk = NewScanner_TryReadCurrentRawValue(
                        reader,
                        record.Address,
                        header.ByteSize,
                        ref cachedBlockBase,
                        ref cachedBlock,
                        ref cachedBlockValid,
                        out currentRaw);

                    if (!readOk)
                    {
                        processed++;
                        continue;
                    }

                    byte[] currentValue = NewScanner_RawBytesToValue(currentRaw, header.ByteSize);

                    if (!NewScanner_ShouldKeepDisplayedValue(currentValue, header.TypeIndex, true))
                    {
                        processed++;
                        continue;
                    }

                    bool isMatch = false;

                    if (mode == Form1.compEq)
                    {
                        isMatch = NewScanner_BytesEqual(currentValue, compareValue, header.ByteSize);
                    }
                    else if (mode == Form1.compNEq)
                    {
                        isMatch = !NewScanner_BytesEqual(currentValue, compareValue, header.ByteSize);
                    }
                    else if (mode == Form1.compChg)
                    {
                        isMatch = !NewScanner_BytesEqual(currentValue, record.Value, header.ByteSize);
                    }
                    else if (mode == Form1.compUChg)
                    {
                        isMatch = NewScanner_BytesEqual(currentValue, record.Value, header.ByteSize);
                    }
                    else if (mode == Form1.compGT)
                    {
                        isMatch = NewScanner_IsDirectionMatch(record.Value, currentValue, header.TypeIndex, true);
                    }
                    else if (mode == Form1.compLT)
                    {
                        isMatch = NewScanner_IsDirectionMatch(record.Value, currentValue, header.TypeIndex, false);
                    }
                    else if (mode == Form1.compINC)
                    {
                        isMatch = NewScanner_IsDeltaMatch(record.Value, currentValue, deltaValue, header.TypeIndex, true);
                    }
                    else if (mode == Form1.compDEC)
                    {
                        isMatch = NewScanner_IsDeltaMatch(record.Value, currentValue, deltaValue, header.TypeIndex, false);
                    }
                    else if (mode == Form1.compVBet)
                    {
                        isMatch = NewScanner_IsValueBetween(currentValue, minValue, maxValue, header.TypeIndex);
                    }

                    if (isMatch)
                    {
                        newStore.WriteRecord(record.Address, currentValue);
                        resultCount++;

                        if (visibleCount < MaxVisibleSnapshotResults)
                        {
                            SearchListView.SearchListViewItem item = type.ToItem(record.Address, currentValue, record.Value, header.TypeIndex);
                            batch.Add(item);
                            visibleCount++;

                            if (batch.Count >= 512)
                            {
                                AddResultRange(batch);
                                batch.Clear();
                            }
                        }
                    }

                    processed++;

                    if ((processed & 0x3FFF) == 0)
                    {
                        SetProgBar((int)Math.Min(processed, Int32.MaxValue));
                        SetStatusLabel("Snapshot results: " + resultCount.ToString("N0") + " | Processed: " + processed.ToString("N0"));
                    }
                }
            }

            if (batch.Count > 0)
            {
                AddResultRange(batch);
                batch.Clear();
            }

            string oldSnapshotPath = activeSnapshotPath;
            activeSnapshotPath = newSnapshotPath;
            activeSnapshotResultCount = resultCount;

            try
            {
                if (!String.IsNullOrEmpty(oldSnapshotPath) && File.Exists(oldSnapshotPath))
                    File.Delete(oldSnapshotPath);
            }
            catch
            {
            }

            Invoke((MethodInvoker)delegate
            {
                searchListView1.AddItemsFromList();
            });

            SetProgBar((int)Math.Min(processed, Int32.MaxValue));
            SetStatusLabel(
                "Snapshot results: " + resultCount.ToString("N0") +
                " | Visible: " + visibleCount.ToString("N0") +
                " | Reads OK: " + reader.Stats.ReadSuccesses.ToString("N0") +
                " | Failed: " + reader.Stats.ReadFailures.ToString("N0"));
        }

        void EqualTo_InitSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args)
        {
            if (CanUseNewExactEqualSearch(typeIndex))
            {
                RunNewExactEqualSearch(startAddr, stopAddr, typeIndex, args);
                return;
            }

            standardByte_InitSearch(startAddr, stopAddr, typeIndex, args, Form1.compEq);
        }

        void NotEqualTo_InitSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args)
        {
            standardByte_InitSearch(startAddr, stopAddr, typeIndex, args, Form1.compNEq);
        }

        void LessThan_InitSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args)
        {
            standardByte_InitSearch(startAddr, stopAddr, typeIndex, args, Form1.compLT);
        }

        void LessThanEqualTo_InitSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args)
        {
            standardByte_InitSearch(startAddr, stopAddr, typeIndex, args, Form1.compLTE);
        }

        void LessThanU_InitSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args)
        {
            standardByte_InitSearch(startAddr, stopAddr, typeIndex, args, Form1.compLTU);
        }

        void LessThanEqualToU_InitSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args)
        {
            standardByte_InitSearch(startAddr, stopAddr, typeIndex, args, Form1.compLTEU);
        }

        void GreaterThan_InitSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args)
        {
            standardByte_InitSearch(startAddr, stopAddr, typeIndex, args, Form1.compGT);
        }

        void GreaterThanEqualTo_InitSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args)
        {
            standardByte_InitSearch(startAddr, stopAddr, typeIndex, args, Form1.compGTE);
        }

        void GreaterThanU_InitSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args)
        {
            standardByte_InitSearch(startAddr, stopAddr, typeIndex, args, Form1.compGTU);
        }

        void GreaterThanEqualToU_InitSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args)
        {
            standardByte_InitSearch(startAddr, stopAddr, typeIndex, args, Form1.compGTEU);
        }

        void ValueBetween_InitSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args)
        {
            if (CanUseNewExactEqualSearch(typeIndex))
            {
                RunNewValueBetweenInitialSearch(startAddr, stopAddr, typeIndex, args);
                return;
            }

            ncSearchType type = SearchTypes[typeIndex];
            type.Initialize((string)args[0], typeIndex);
            type = SearchTypes[typeIndex];

            bool gotRes = false;
            bool updateCont = false;
            int size = 0x10000;
            //if ((stopAddr - startAddr) <= (ulong)size)
            //    updateCont = true;

            byte[] cmp = new byte[size];

            int realDif = 0;
            if (updateCont)
                realDif = (int)(stopAddr - startAddr);
            else
                realDif = (int)GetRealDif(startAddr, stopAddr, (ulong)size);
            SetProgBarMax(realDif - 1);

            byte[] cmpArrayMin = type.ToByteArray((string)args[0]);
            byte[] cmpArrayMax = type.ToByteArray((string)args[1]);
            int resCnt = 0;
            int incSize = type.ByteSize;
            if (type.ignoreAlignment)
                incSize = GetAlign(1);
            //List<SearchListView.SearchListViewItem> itemsToAdd = new List<SearchListView.SearchListViewItem>();

            for (ulong addr = startAddr; addr < stopAddr; addr += (ulong)size)
            {
                if (_shouldStopSearch)
                    break;

                addr = misc.ParseSchAddr(addr);
                gotRes = false;
                searchListView1.BeginUpdate();

                if (addr != startAddr && incSize == 1 && type.ByteSize != 1)
                {
                    addr -= (ulong)(cmpArrayMin.Length + 1);
                }

                List<SearchListView.SearchListViewItem> resItems = new List<SearchListView.SearchListViewItem>();

                if (Form1.apiGetMem(addr, ref cmp))
                {
                    for (int x = 0; x <= (size - type.ByteSize); x += incSize)
                    {
                        if (_shouldStopSearch)
                            break;

                        byte[] tempArr = new byte[type.ByteSize];
                        Array.Copy(cmp, x, tempArr, 0, type.ByteSize);
                        tempArr = misc.notrevif(tempArr);

                        bool isTrue = misc.ArrayCompare(cmpArrayMin, tempArr, cmpArrayMax, Form1.compVBet);

                        if (isTrue)
                        {
                            resItems.Add(type.ToItem(addr + (ulong)x, tempArr, new byte[0], typeIndex));
                            resCnt++;
                            gotRes = true;
                        }
                        //if (updateCont)
                        //    IncProgBar(incSize);
                    }

                    if (gotRes)
                    {
                        SetStatusLabel("Results: " + resCnt.ToString("N0"));
                        AddResultRange(resItems);
                        resItems.Clear();
                    }
                }

                searchListView1.EndUpdate();
                Invoke((MethodInvoker)delegate
                {
                    searchListView1.AddItemsFromList();
                });
                //Thread.Sleep(100);
                //if (!updateCont)
                    IncProgBar(1);
            }
        }

        void Pointer_InitSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args)
        {
            uint val = uint.Parse((string)args[0], System.Globalization.NumberStyles.HexNumber);

            ncSearchType type = SearchTypes[typeIndex];
            type.Initialize((string)args[0], typeIndex);
            type = SearchTypes[typeIndex];

            bool gotRes = false;
            bool updateCont = false;
            int size = 0x10000;
            //if ((stopAddr - startAddr) <= (ulong)size)
            //    updateCont = true;

            byte[] cmp = new byte[size];

            int realDif = 0;
            if (updateCont)
                realDif = (int)(stopAddr - startAddr);
            else
                realDif = (int)GetRealDif(startAddr, stopAddr, (ulong)size);
            SetProgBarMax(realDif - 1);

            byte[] cmpArrayMin = type.ToByteArray((val - 0x7FFF).ToString("X8"));
            byte[] cmpArrayMax = type.ToByteArray((val + 0x7FFF).ToString("X8"));
            int resCnt = 0;
            int incSize = type.ByteSize;
            if (type.ignoreAlignment)
                incSize = GetAlign(1);
            //List<SearchListView.SearchListViewItem> itemsToAdd = new List<SearchListView.SearchListViewItem>();

            for (ulong addr = startAddr; addr < stopAddr; addr += (ulong)size)
            {
                if (_shouldStopSearch)
                    break;

                addr = misc.ParseSchAddr(addr);
                gotRes = false;
                searchListView1.BeginUpdate();

                if (addr != startAddr && incSize == 1 && type.ByteSize != 1)
                {
                    addr -= (ulong)(cmpArrayMin.Length + 1);
                }

                List<SearchListView.SearchListViewItem> resItems = new List<SearchListView.SearchListViewItem>();

                if (Form1.apiGetMem(addr, ref cmp))
                {
                    for (int x = 0; x <= (size - type.ByteSize); x += incSize)
                    {
                        if (_shouldStopSearch)
                            break;

                        byte[] tempArr = new byte[type.ByteSize];
                        Array.Copy(cmp, x, tempArr, 0, type.ByteSize);
                        tempArr = misc.notrevif(tempArr);

                        bool isTrue = misc.ArrayCompare(cmpArrayMin, tempArr, cmpArrayMax, Form1.compVBet);

                        //Also check if it is 4 byte aligned
                        if (isTrue && (tempArr[3] % 4) == 0)
                        {
                            resItems.Add(type.ToItem(addr + (ulong)x, tempArr, new byte[0], typeIndex, val));
                            resCnt++;
                            gotRes = true;
                        }
                        //if (updateCont)
                        //    IncProgBar(incSize);
                    }

                    if (gotRes)
                    {
                        SetStatusLabel("Results: " + resCnt.ToString("N0"));
                        AddResultRange(resItems);
                        resItems.Clear();
                    }
                }

                searchListView1.EndUpdate();
                Invoke((MethodInvoker)delegate
                {
                    searchListView1.AddItemsFromList();
                });
                //Thread.Sleep(100);
                //if (!updateCont)
                IncProgBar(1);
            }
        }

        void UnknownValue_InitSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args)
        {
            if (CanUseNewExactEqualSearch(typeIndex))
            {
                RunNewUnknownInitialSearch(startAddr, stopAddr, typeIndex, args);
                return;
            }

            ncSearchType type = SearchTypes[typeIndex];

            int size = 0x10000;
            byte[] cmp = new byte[size];

            int realDif = (int)GetRealDif(startAddr, stopAddr, (ulong)size);
            ulong dif = (ulong)(GetRealDif(startAddr, stopAddr, 1) / (type.ignoreAlignment ? 1 : type.ByteSize));
            string disp = "This search will have " + dif.ToString() + " results! During this search no results will be shown in the list until it is complete.\nDo you wish to continue?";
            if (MessageBox.Show(disp, "Do you wish to continue?", MessageBoxButtons.YesNo) == DialogResult.No)
            {
                return;
            }

            Invoke((MethodInvoker)delegate
            {
                searchListView1.Enabled = false;
            });

            SetProgBarMax(realDif - 1);

            int resCnt = 0;
            int incSize = type.ByteSize;
            if (type.ignoreAlignment)
                incSize = GetAlign(1);
            List<SearchListView.SearchListViewItem> itemsToAdd = new List<SearchListView.SearchListViewItem>();

            for (ulong addr = startAddr; addr < stopAddr; addr += (ulong)size)
            {
                if (_shouldStopSearch)
                    break;

                addr = misc.ParseSchAddr(addr);
                if (Form1.apiGetMem(addr, ref cmp))
                {
                    for (int x = 0; x <= (size - type.ByteSize); x += incSize)
                    {
                        if (_shouldStopSearch)
                            break;

                        byte[] tempArr = new byte[type.ByteSize];
                        Array.Copy(cmp, x, tempArr, 0, type.ByteSize);
                        tempArr = misc.notrevif(tempArr);
                        itemsToAdd.Add(type.ToItem(addr + (ulong)x, tempArr, new byte[0], typeIndex));
                        resCnt++;
                        //gotRes = true;
                        //SetStatusLabel("Results: " + resCnt.ToString());
                        //if (updateCont)
                        //    IncProgBar(incSize);
                    }
                    SetStatusLabel("Results: " + resCnt.ToString("N0"));
                    AddResultRange(itemsToAdd);
                    itemsToAdd.Clear();

                }

                //Thread.Sleep(100);
                //if (!updateCont)
                IncProgBar(1);
            }

            Invoke((MethodInvoker)delegate
            {
                searchListView1.Enabled = true;
                searchListView1.AddItemsFromList();
            });
        }

        void Joker_InitSearch(ulong startAddr, ulong stopAddr, int typeIndex, string[] args)
        {
            bool gotRes = false;
            ncSearchType type = SearchTypes[typeIndex];
            if (args.Length > 0)
            {
                type.Initialize((string)args[0], typeIndex);
                type = SearchTypes[typeIndex];
            }

            bool updateCont = false;
            int size = 0x10000;
            //if ((stopAddr - startAddr) <= (ulong)size)
            //    updateCont = true;

            byte[] cmp = new byte[size];

            int realDif = 0;
            if (updateCont)
                realDif = (int)(stopAddr - startAddr);
            else
                realDif = (int)GetRealDif(startAddr, stopAddr, (ulong)size);
            SetProgBarMax(realDif - 1);

            int resCnt = 0;
            int incSize = type.ByteSize;
            if (type.ignoreAlignment)
                incSize = GetAlign(1);
            List<SearchListView.SearchListViewItem> itemsToAdd = new List<SearchListView.SearchListViewItem>();

            MessageBox.Show("Please press and hold the following button: " + Joker_ButtonChecks[0], "Button to hold", MessageBoxButtons.OK);

            for (ulong addr = startAddr; addr < stopAddr; addr += (ulong)size)
            {
                if (_shouldStopSearch)
                    break;

                gotRes = false;
                addr = misc.ParseSchAddr(addr);

                if (addr != startAddr && incSize == 1 && type.ByteSize != 1)
                {
                    addr -= (ulong)(type.ByteSize + 1);
                }

                if (Form1.apiGetMem(addr, ref cmp))
                {
                    for (int x = 0; x <= (size - type.ByteSize); x += incSize)
                    {
                        if (_shouldStopSearch)
                            break;

                        byte[] tempArr = new byte[type.ByteSize];
                        Array.Copy(cmp, x, tempArr, 0, type.ByteSize);
                        tempArr = misc.notrevif(tempArr);
                        bool isTrue = false; // misc.ArrayCompare(cmpArray, tempArr, null, cmpIntType);
                        int arrCnt = 0;
                        for (int i = 0; i < tempArr.Length; i++)
                        {
                            if (tempArr[i] != 0)
                            {
                                arrCnt++;

                                uint tempVal = BitConverter.ToUInt32(tempArr, 0);
                                //Make sure only 1 bit is set
                                if ((tempVal & (tempVal - 1)) == 0)
                                {
                                    isTrue = true;
                                }
                            }
                        }

                        if (isTrue && arrCnt == 1)
                        {
                            itemsToAdd.Add(type.ToItem(addr + (ulong)x, tempArr, new byte[0], typeIndex, 0));
                            resCnt++;
                            gotRes = true;
                            //SetStatusLabel("Results: " + resCnt.ToString());
                        }
                        //if (updateCont)
                        //    IncProgBar(incSize);
                    }

                    if (gotRes)
                    {
                        SetStatusLabel("Results: " + resCnt.ToString("N0"));
                        AddResultRange(itemsToAdd);
                        itemsToAdd.Clear();
                    }

                }

                //Thread.Sleep(100);
                //if (!updateCont)
                IncProgBar(1);
            }

            Invoke((MethodInvoker)delegate
            {
                searchListView1.AddItemsFromList();
            });
        }

        #endregion

        #region Next Searches

        void standardByte_NextSearch(SearchListView.SearchListViewItem[] old, string[] args, int cmpIntType)
        {
            SetProgBarMax(old.Length - 1);
            int resCnt = 0;
            int updateCnt = 0;

            ncSearchType type = new ncSearchType();
            byte[] cmpArray = null;

            ClearItems();

            List<SearchListView.SearchListViewItem> itemsToAdd = new List<SearchListView.SearchListViewItem>();
            ulong curAddrIndex = 0;
            byte[] rec = new byte[0x10000];
            for (int cnt = 0; cnt < old.Length; cnt++)
            {
                int off = (int)(old[cnt].addr - curAddrIndex);
                if ((uint)off >= (uint)rec.Length)
                {
                    curAddrIndex = old[cnt].addr - (old[cnt].addr % (ulong)rec.Length);
                    Form1.apiGetMem(curAddrIndex, ref rec);
                    off = (int)(old[cnt].addr - curAddrIndex);
                    updateCnt++;
                }

                if (_shouldStopSearch)
                    break;

                if (type.ToByteArray == null)
                    type = SearchTypes[old[cnt].align];
                if (cmpArray == null)
                    cmpArray = type.ToByteArray((string)args[0]);

                byte[] tempArr = new byte[type.ByteSize];
                Array.Copy(rec, off, tempArr, 0, type.ByteSize);
                tempArr = misc.notrevif(tempArr);
                if (misc.ArrayCompare(cmpArray, tempArr, null, cmpIntType))
                {
                    SearchListView.SearchListViewItem item = old[cnt];
                    item.oldVal = item.newVal;
                    item.newVal = tempArr;
                    itemsToAdd.Add(item);
                    resCnt++;
                }

                if ((cnt % rec.Length) == 0 || updateCnt > 50)
                {
                    SetStatusLabel("Results: " + resCnt.ToString("N0"));
                    AddResultRange(itemsToAdd);
                    itemsToAdd.Clear();
                    SetProgBar(cnt);
                    updateCnt = 0;
                }

                //Thread.Sleep(100);
            }

            if (itemsToAdd.Count > 0)
            {
                AddResultRange(itemsToAdd);
                itemsToAdd.Clear();
            }

            Invoke((MethodInvoker)delegate
            {
                searchListView1.AddItemsFromList();
            });
            SetStatusLabel("Results: " + resCnt.ToString("N0"));
        }

        void EqualTo_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            if (HasActiveSnapshot())
            {
                RunNewSnapshotNextSearch(args, Form1.compEq);
                return;
            }

            if (activeScanUsesNewEngine && old != null && old.Length > 0 && CanUseNewExactEqualSearch(old[0].align))
            {
                RunNewBasicNextSearch(old, args, Form1.compEq);
                return;
            }

            standardByte_NextSearch(old, args, Form1.compEq);
        }

        void NotEqualTo_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            if (HasActiveSnapshot())
            {
                RunNewSnapshotNextSearch(args, Form1.compNEq);
                return;
            }

            if (activeScanUsesNewEngine && old != null && old.Length > 0 && CanUseNewExactEqualSearch(old[0].align))
            {
                RunNewBasicNextSearch(old, args, Form1.compNEq);
                return;
            }

            standardByte_NextSearch(old, args, Form1.compNEq);
        }

        void LessThan_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            standardByte_NextSearch(old, args, Form1.compLT);
        }

        void LessThanEqualTo_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            standardByte_NextSearch(old, args, Form1.compLTE);
        }

        void LessThanU_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            standardByte_NextSearch(old, args, Form1.compLTU);
        }

        void LessThanEqualToU_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            standardByte_NextSearch(old, args, Form1.compLTEU);
        }

        void GreaterThan_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            standardByte_NextSearch(old, args, Form1.compGT);
        }

        void GreaterThanEqualTo_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            standardByte_NextSearch(old, args, Form1.compGTE);
        }

        void GreaterThanU_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            standardByte_NextSearch(old, args, Form1.compGTU);
        }

        void GreaterThanEqualToU_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            standardByte_NextSearch(old, args, Form1.compGTEU);
        }

        void ValueBetween_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            if (HasActiveSnapshot())
            {
                RunNewSnapshotNextSearch(args, Form1.compVBet);
                return;
            }

            if (activeScanUsesNewEngine && old != null && old.Length > 0 && CanUseNewExactEqualSearch(old[0].align))
            {
                RunNewValueBetweenNextSearch(old, args);
                return;
            }

            SetProgBarMax(old.Length - 1);
            int resCnt = 0;

            ncSearchType type = new ncSearchType();
            byte[] cmpArrayMin = null, cmpArrayMax = null;
            
            ClearItems();

            List<SearchListView.SearchListViewItem> itemsToAdd = new List<SearchListView.SearchListViewItem>();
            ulong curAddrIndex = 0;
            byte[] rec = new byte[0x10000];
            for (int cnt = 0; cnt < old.Length; cnt++)
            {
                int off = (int)(old[cnt].addr - curAddrIndex);
                if ((uint)off >= (uint)rec.Length)
                {
                    curAddrIndex = old[cnt].addr - (old[cnt].addr % (ulong)rec.Length);
                    Form1.apiGetMem(curAddrIndex, ref rec);
                    off = (int)(old[cnt].addr - curAddrIndex);
                }

                if (_shouldStopSearch)
                    break;

                if (type.ToByteArray == null)
                    type = SearchTypes[old[cnt].align];
                if (cmpArrayMin == null)
                    cmpArrayMin = type.ToByteArray((string)args[0]);
                if (cmpArrayMax == null)
                    cmpArrayMax = type.ToByteArray((string)args[1]);

                byte[] tempArr = new byte[type.ByteSize];
                Array.Copy(rec, off, tempArr, 0, type.ByteSize);
                tempArr = misc.notrevif(tempArr);
                if (misc.ArrayCompare(cmpArrayMin, tempArr, cmpArrayMax, Form1.compVBet))
                {
                    SearchListView.SearchListViewItem item = old[cnt];
                    item.oldVal = item.newVal;
                    item.newVal = tempArr;
                    itemsToAdd.Add(item);
                    resCnt++;
                }

                if ((cnt % rec.Length) == 0)
                {
                    SetStatusLabel("Results: " + resCnt.ToString("N0"));
                    AddResultRange(itemsToAdd);
                    itemsToAdd.Clear();
                    SetProgBar(cnt);
                }
            }

            if (itemsToAdd.Count > 0)
            {
                AddResultRange(itemsToAdd);
                itemsToAdd.Clear();
            }

            Invoke((MethodInvoker)delegate
            {
                searchListView1.AddItemsFromList();
            });
            SetStatusLabel("Results: " + resCnt.ToString("N0"));
        }

        void standardIncDec_NextSearch(SearchListView.SearchListViewItem[] old, string[] args, int cmpIntIndex)
        {
            SetProgBarMax(old.Length - 1);
            int resCnt = 0;

            ncSearchType type = new ncSearchType();
            
            byte[] c = null;

            ClearItems();

            List<SearchListView.SearchListViewItem> itemsToAdd = new List<SearchListView.SearchListViewItem>();
            ulong curAddrIndex = 0;
            byte[] rec = new byte[0x10000];
            int off = 0;
            for (int cnt = 0; cnt < old.Length; cnt++)
            {
                off = (int)(old[cnt].addr - curAddrIndex);
                if ((uint)off >= (uint)rec.Length)
                {
                    curAddrIndex = old[cnt].addr - (old[cnt].addr % (ulong)rec.Length);
                    Form1.apiGetMem(curAddrIndex, ref rec);
                    off = (int)(old[cnt].addr - curAddrIndex);
                }

                if (_shouldStopSearch)
                    break;

                if (type.ToByteArray == null)
                    type = SearchTypes[old[cnt].align];
                if (args.Length > 0 && c == null)
                    c = type.ToByteArray((string)args[0]);

                byte[] tempArr = new byte[type.ByteSize];
                Array.Copy(rec, off, tempArr, 0, type.ByteSize);
                tempArr = misc.notrevif(tempArr);
                if (misc.ArrayCompare(old[cnt].newVal, tempArr, c, cmpIntIndex))
                {
                    SearchListView.SearchListViewItem item = old[cnt];
                    item.oldVal = item.newVal;
                    item.newVal = tempArr;
                    itemsToAdd.Add(item);
                    resCnt++;
                }


                if ((cnt % rec.Length) == 0)
                {
                    SetStatusLabel("Results: " + resCnt.ToString("N0"));
                    AddResultRange(itemsToAdd);
                    itemsToAdd.Clear();
                    SetProgBar(cnt);
                }
            }

            if (itemsToAdd.Count > 0)
            {
                AddResultRange(itemsToAdd);
                itemsToAdd.Clear();
            }

            Invoke((MethodInvoker)delegate
            {
                searchListView1.AddItemsFromList();
            });
            SetStatusLabel("Results: " + resCnt.ToString("N0"));
        }

        void Increased_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            if (HasActiveSnapshot())
            {
                RunNewSnapshotNextSearch(args, Form1.compGT);
                return;
            }

            if (activeScanUsesNewEngine && old != null && old.Length > 0 && CanUseNewExactEqualSearch(old[0].align))
            {
                RunNewDirectionNextSearch(old, true);
                return;
            }

            standardIncDec_NextSearch(old, new string[0], Form1.compGT);
        }

        void IncreasedBy_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            if (HasActiveSnapshot())
            {
                RunNewSnapshotNextSearch(args, Form1.compINC);
                return;
            }

            if (activeScanUsesNewEngine && old != null && old.Length > 0 && CanUseNewExactEqualSearch(old[0].align))
            {
                RunNewDeltaNextSearch(old, args, true);
                return;
            }

            standardIncDec_NextSearch(old, args, Form1.compINC);
        }

        void Decreased_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            if (HasActiveSnapshot())
            {
                RunNewSnapshotNextSearch(args, Form1.compLT);
                return;
            }

            if (activeScanUsesNewEngine && old != null && old.Length > 0 && CanUseNewExactEqualSearch(old[0].align))
            {
                RunNewDirectionNextSearch(old, false);
                return;
            }

            standardIncDec_NextSearch(old, new string[0], Form1.compLT);
        }

        void DecreasedBy_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            if (HasActiveSnapshot())
            {
                RunNewSnapshotNextSearch(args, Form1.compDEC);
                return;
            }

            if (activeScanUsesNewEngine && old != null && old.Length > 0 && CanUseNewExactEqualSearch(old[0].align))
            {
                RunNewDeltaNextSearch(old, args, false);
                return;
            }

            standardIncDec_NextSearch(old, args, Form1.compDEC);
        }

        void Changed_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            if (HasActiveSnapshot())
            {
                RunNewSnapshotNextSearch(args, Form1.compChg);
                return;
            }

            if (activeScanUsesNewEngine && old != null && old.Length > 0 && CanUseNewExactEqualSearch(old[0].align))
            {
                RunNewBasicNextSearch(old, new string[0], Form1.compChg);
                return;
            }

            standardIncDec_NextSearch(old, new string[0], Form1.compNEq);
        }

        void Unchanged_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            if (HasActiveSnapshot())
            {
                RunNewSnapshotNextSearch(args, Form1.compUChg);
                return;
            }

            if (activeScanUsesNewEngine && old != null && old.Length > 0 && CanUseNewExactEqualSearch(old[0].align))
            {
                RunNewBasicNextSearch(old, new string[0], Form1.compUChg);
                return;
            }

            standardIncDec_NextSearch(old, new string[0], Form1.compEq);
        }

        void Pointer_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            uint val = uint.Parse((string)args[0], System.Globalization.NumberStyles.HexNumber);

            SetProgBarMax(old.Length - 1);
            int resCnt = 0;

            ncSearchType type = new ncSearchType();
            byte[] cmpArrayMin = null, cmpArrayMax = null;

            ClearItems();

            List<SearchListView.SearchListViewItem> itemsToAdd = new List<SearchListView.SearchListViewItem>();
            ulong curAddrIndex = 0;
            byte[] rec = new byte[0x10000];
            for (int cnt = 0; cnt < old.Length; cnt++)
            {
                int off = (int)(old[cnt].addr - curAddrIndex);
                if ((uint)off >= (uint)rec.Length)
                {
                    curAddrIndex = old[cnt].addr - (old[cnt].addr % (ulong)rec.Length);
                    Form1.apiGetMem(curAddrIndex, ref rec);
                    off = (int)(old[cnt].addr - curAddrIndex);
                }

                if (_shouldStopSearch)
                    break;

                if (type.ToByteArray == null)
                    type = SearchTypes[old[cnt].align];
                if (cmpArrayMin == null)
                    cmpArrayMin = (type.ToByteArray((val - 0x7FFF).ToString("X8")));
                if (cmpArrayMax == null)
                    cmpArrayMax = (type.ToByteArray((val + 0x7FFF).ToString("X8")));

                byte[] tempArr = new byte[type.ByteSize];
                Array.Copy(rec, off, tempArr, 0, type.ByteSize);
                tempArr = misc.notrevif(tempArr);
                if (misc.ArrayCompare(cmpArrayMin, tempArr, cmpArrayMax, Form1.compVBet) && (tempArr[3] % 4) == 0)
                {
                    SearchListView.SearchListViewItem item = old[cnt];

                    uint offset =  item.misc - (uint)misc.ByteArrayToLong(item.newVal, 0, 4);
                    uint newOff = val - (uint)misc.ByteArrayToLong(tempArr, 0, 4);
                    if (offset == newOff)
                    {
                        item.oldVal = item.newVal;
                        item.newVal = tempArr;
                        item.misc = val;
                        itemsToAdd.Add(item);
                        resCnt++;
                    }
                }

                if ((cnt % rec.Length) == 0)
                {
                    SetStatusLabel("Results: " + resCnt.ToString("N0"));
                    AddResultRange(itemsToAdd);
                    itemsToAdd.Clear();
                    SetProgBar(cnt);
                }
            }

            if (itemsToAdd.Count > 0)
            {
                AddResultRange(itemsToAdd);
                itemsToAdd.Clear();
            }

            Invoke((MethodInvoker)delegate
            {
                searchListView1.AddItemsFromList();
            });
            SetStatusLabel("Results: " + resCnt.ToString("N0"));
        }

        void Joker_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            SetProgBarMax(old.Length - 1);
            int resCnt = 0;
            int updateCnt = 0;

            ncSearchType type = new ncSearchType();
            byte[] cmpArray = null;

            ClearItems();

            List<SearchListView.SearchListViewItem> itemsToAdd = new List<SearchListView.SearchListViewItem>();
            ulong curAddrIndex = 0;
            byte[] rec = new byte[0x10000];

            if (old.Length <= 0)
            {
                MessageBox.Show("No more results to work with. Try scanning a different range.", "Joker Finder", MessageBoxButtons.OK);
                SetStatusLabel("Results: " + resCnt.ToString("N0"));
                return;
            }

            uint dis = old[0].misc + 1;
            if (dis >= Joker_ButtonChecks.Length)
                dis = 0;
            MessageBox.Show("Please press and hold the following button: " + Joker_ButtonChecks[dis], "Button to hold", MessageBoxButtons.OK);

            for (int cnt = 0; cnt < old.Length; cnt++)
            {
                int off = (int)(old[cnt].addr - curAddrIndex);
                if ((uint)off >= (uint)rec.Length)
                {
                    curAddrIndex = old[cnt].addr - (old[cnt].addr % (ulong)rec.Length);
                    Form1.apiGetMem(curAddrIndex, ref rec);
                    off = (int)(old[cnt].addr - curAddrIndex);
                    updateCnt++;
                }

                if (_shouldStopSearch)
                    break;

                if (type.ToByteArray == null)
                    type = SearchTypes[old[cnt].align];

                byte[] tempArr = new byte[type.ByteSize];
                Array.Copy(rec, off, tempArr, 0, type.ByteSize);
                tempArr = misc.notrevif(tempArr);

                bool isTrue = false;
                uint tempVal = BitConverter.ToUInt32(tempArr, 0);
                int cc = 0;
                if ((tempVal & (tempVal - 1)) == 0 && misc.ArrayCompare(tempArr, old[cnt].newVal, null, Form1.compNEq) && tempVal != 0)
                {
                    //Starting new check button region
                    if (((old[cnt].misc + 1) % 4) == 0)
                    {
                        isTrue = true;
                    }
                    else //same button region
                    {
                        isTrue = true;
                        for (cc = 0; cc < old[cnt].newVal.Length; cc++)
                        {
                            if (!((old[cnt].newVal[cc] == 0 && tempArr[cc] == 0) || (old[cnt].newVal[cc] != tempArr[cc])))
                            {
                                isTrue = false;
                                break;
                            }
                        }
                    }
                }

                //if (misc.ArrayCompare(cmpArray, tempArr, null, cmpIntType))
                if (isTrue)
                {
                    SearchListView.SearchListViewItem item = old[cnt];
                    item.oldVal = item.newVal;
                    item.newVal = tempArr;
                    item.misc = old[cnt].misc + 1;
                    if (item.misc >= Joker_ButtonChecks.Length)
                        item.misc = 0;
                    itemsToAdd.Add(item);
                    resCnt++;
                }

                if ((cnt % rec.Length) == 0 || updateCnt > 50)
                {
                    SetStatusLabel("Results: " + resCnt.ToString("N0"));
                    AddResultRange(itemsToAdd);
                    itemsToAdd.Clear();
                    SetProgBar(cnt);
                    updateCnt = 0;
                }

                //Thread.Sleep(100);
            }

            if (itemsToAdd.Count > 0)
            {
                AddResultRange(itemsToAdd);
                itemsToAdd.Clear();
            }

            Invoke((MethodInvoker)delegate
            {
                searchListView1.AddItemsFromList();
            });
            SetStatusLabel("Results: " + resCnt.ToString("N0"));
        }

        void BitDif_NextSearch(SearchListView.SearchListViewItem[] old, string[] args)
        {
            SetProgBarMax(old.Length - 1);
            int resCnt = 0;

            ncSearchType type = new ncSearchType();
            byte[] cmpArray = null;

            ClearItems();

            List<SearchListView.SearchListViewItem> itemsToAdd = new List<SearchListView.SearchListViewItem>();
            ulong curAddrIndex = 0;
            byte[] rec = new byte[0x10000];

            int bitDifCount = 0;
            if (args.Length > 0)
            {
                bitDifCount = int.Parse((string)args[0], System.Globalization.NumberStyles.HexNumber);
            }

            for (int cnt = 0; cnt < old.Length; cnt++)
            {
                int off = (int)(old[cnt].addr - curAddrIndex);
                if ((uint)off >= (uint)rec.Length)
                {
                    curAddrIndex = old[cnt].addr - (old[cnt].addr % (ulong)rec.Length);
                    Form1.apiGetMem(curAddrIndex, ref rec);
                    off = (int)(old[cnt].addr - curAddrIndex);
                }

                if (_shouldStopSearch)
                    break;

                if (type.ToByteArray == null)
                    type = SearchTypes[old[cnt].align];

                byte[] tempArr = new byte[type.ByteSize];
                Array.Copy(rec, off, tempArr, 0, type.ByteSize);
                tempArr = misc.notrevif(tempArr);

                bool isTrue = false;
                int cc = 0;
                if (misc.ArrayCompare(tempArr, old[cnt].newVal, null, Form1.compNEq))
                {
                    int difCnt = 0;
                    byte[] res = new byte[tempArr.Length];
                    for (cc = 0; cc < res.Length; cc++)
                    {
                        res[cc] = (byte)(tempArr[cc] ^ old[cnt].newVal[cc]);
                        while (res[cc] != 0)
                        {
                            difCnt++;
                            res[cc] &= (byte)(res[cc] - 1);
                        }
                    }

                    if (difCnt == bitDifCount)
                        isTrue = true;
                }

                //if (misc.ArrayCompare(cmpArray, tempArr, null, cmpIntType))
                if (isTrue)
                {
                    SearchListView.SearchListViewItem item = old[cnt];
                    item.oldVal = item.newVal;
                    item.newVal = tempArr;
                    item.misc = old[cnt].misc + 1;
                    if (item.misc >= Joker_ButtonChecks.Length)
                        item.misc = 0;
                    itemsToAdd.Add(item);
                    resCnt++;
                }

                if ((cnt % rec.Length) == 0)
                {
                    SetStatusLabel("Results: " + resCnt.ToString("N0"));
                    AddResultRange(itemsToAdd);
                    itemsToAdd.Clear();
                    SetProgBar(cnt);
                }

                //Thread.Sleep(100);
            }

            if (itemsToAdd.Count > 0)
            {
                AddResultRange(itemsToAdd);
                itemsToAdd.Clear();
            }

            Invoke((MethodInvoker)delegate
            {
                searchListView1.AddItemsFromList();
            });
            SetStatusLabel("Results: " + resCnt.ToString("N0"));
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
        }

        private void searchNameBox_SelectedIndexChanged(object sender, EventArgs e)
        {
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
