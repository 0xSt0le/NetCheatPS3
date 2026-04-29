using System;
using System.Threading;
using System.Windows.Forms;

namespace NetCheatPS3
{
    public partial class SearchControl
    {
        #region GUI Buttons

        public void ThreadInitSearch(object[] args)
        {
            bool pausedForScan = false;
            bool stopped = false;

            try
            {
                if (AbortScanThreadIfDisconnectedOrUnattached("Initial Scan"))
                    return;

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

                bool usedFuzzyValueScan = TryRunFuzzyValueInitialSearch(searcher, start, stop, index, passArgs);
                if (!usedFuzzyValueScan)
                    searcher.InitialSearch(start, stop, index, passArgs);

                CaptureFirstSnapshotAfterInitialScan(searcher);

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
                if (AbortScanThreadIfDisconnectedOrUnattached("Next Scan"))
                    return;

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

                bool usedSnapshotNext = TryRunSnapshotNextSearch(searcher, (string[])args[2]);
                if (!usedSnapshotNext)
                    searcher.NextSearch(items, (string[])args[2]);

                stopw.Stop();
                CaptureLastScanStatsFromUi(usedSnapshotNext ? "Snapshot Next Scan" : "Next Scan", stopw.ElapsedMilliseconds);

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
                searchThread = null;
                return;
            }
            else if (nextSearchMem.Text == "Next Scan")
            {
                if (!EnsureConnectedAndAttachedForScanAction("Next Scan"))
                    return;

                try
                {
                    if (searchThread != null)
                        searchThread = null;

                    progBar.printText = "";

                    _shouldStopSearch = false;
                    searchMemoryStopProc = Form1.Instance.isProcessStopped();

                    ulong start;
                    ulong stop;
                    int typeIndex;
                    string[] args;
                    ncSearcher searcher;

                    if (!TryBuildScanRequest("Next Scan", out start, out stop, out typeIndex, out searcher, out args))
                        return;

                    ulong probeAddress = GetNextScanProbeAddress(start);
                    if (!ValidateScanMemoryBeforeStart("Next Scan", probeAddress, SearchTypes[typeIndex].ByteSize))
                        return;

                    searchThread = new Thread(() => ThreadNextSearch(new object[]
                    {
                        searcher,
                        searchListView1.a.ToArray(),
                        args,
                        searchPWS.Checked && isPWSVisible
                    }));

                    searchThread.IsBackground = true;
                    searchThread.Start();

                    SetSearchActionRunningStatus("Next Scan");
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
                searchThread = null;
                return;
            }
            else if (searchMemory.Text == "New Scan")
            {
                searchListView1.ClearItems();

                ClearSnapshotStateAndDeleteTempFiles();
                ClearLastScanStats();

                searchMemory.Text = "Initial Scan";
                progBar.printText = "";

                forceTBUpdate = true;
                lastSearchIndex = -1;
                lastTypeIndex = -1;
                isInitialScan = true;
                searchNameBox_SelectedIndexChanged(searchNameBox, EventArgs.Empty);
                forceTBUpdate = false;
            }
            else if (searchMemory.Text == "Initial Scan")
            {
                if (!EnsureConnectedAndAttachedForScanAction("Initial Scan"))
                    return;

                try
                {
                    if (searchThread != null)
                        searchThread = null;

                    _shouldStopSearch = false;
                    ClearSnapshotStateAndDeleteTempFiles();
                    ClearLastScanStats();

                    activeScanUsesNewEngine = false;
                    activeScanLittleEndian = IsLittleEndianModeChecked();

                    searchMemoryStopProc = Form1.Instance.isProcessStopped();

                    ulong start;
                    ulong stop;
                    int typeIndex;
                    string[] args;
                    ncSearcher searcher;

                    if (!TryBuildScanRequest("Initial Scan", out start, out stop, out typeIndex, out searcher, out args))
                        return;

                    if (!ValidateScanMemoryBeforeStart("Initial Scan", start, SearchTypes[typeIndex].ByteSize))
                        return;

                    searchThread = new Thread(() => ThreadInitSearch(new object[]
                    {
                        searcher,
                        start,
                        stop,
                        typeIndex,
                        args,
                        searchPWS.Checked && isPWSVisible
                    }));

                    searchThread.Priority = ThreadPriority.Highest;
                    searchThread.IsBackground = true;
                    searchThread.Start();

                    SetSearchActionRunningStatus("Initial Scan");
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
            if (!EnsureConnectedAndAttachedForScanAction("Refresh From Memory"))
                return;

            ulong probeAddress;
            int byteSize;
            if (TryGetRefreshProbeAddress(out probeAddress, out byteSize) &&
                !ValidateScanMemoryBeforeStart("Refresh From Memory", probeAddress, byteSize))
                return;

            RefreshResults(1);
        }

        private ulong GetNextScanProbeAddress(ulong fallbackStart)
        {
            if (searchListView1 != null && searchListView1.a != null && searchListView1.a.Count > 0)
                return searchListView1.a[0].addr;

            return fallbackStart;
        }

        private bool TryGetRefreshProbeAddress(out ulong address, out int byteSize)
        {
            address = 0;
            byteSize = 4;

            if (searchListView1 == null || searchListView1.a == null || searchListView1.a.Count <= 0)
                return false;

            SearchListView.SearchListViewItem item = searchListView1.a[0];
            address = item.addr;
            if (item.newVal != null && item.newVal.Length > 0)
                byteSize = item.newVal.Length;

            return address != 0;
        }

        public void RefreshResults(int mode)
        {
            switch (mode)
            {
                case 0:
                    searchListView1.AddItemsFromList();

                    for (int x = 0; x < searchListView1.TotalCount; x++)
                    {
                        SearchListView.SearchListViewItem item = searchListView1.GetItemAtIndex(x);
                        byte[] getBytes = new byte[item.newVal.Length];

                        if (Form1.apiGetMem(item.addr, ref getBytes))
                        {
                            item.oldVal = item.newVal;
                            item.newVal = getBytes;
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


        #endregion
    }
}
