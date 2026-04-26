using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace NetCheatPS3
{
    public partial class SearchControl
    {
        #region Search Thread Calls

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
    }
}
