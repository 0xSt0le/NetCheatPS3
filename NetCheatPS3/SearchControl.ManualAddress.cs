using System;
using System.Windows.Forms;

namespace NetCheatPS3
{
    public partial class SearchControl
    {
        private void ShowAddManualAddressDialog()
        {
            using (AddManualAddressForm form = new AddManualAddressForm())
            {
                form.TryAddAddress = TryAddManualSearchResult;
                form.ShowDialog(this);
            }
        }

        private bool TryAddManualSearchResult(ulong address, string typeName, int byteSize)
        {
            if (!Form1.connected || !Form1.attached || Form1.curAPI == null || Form1.curAPI.Instance == null)
            {
                string message = "Connect and attach before adding a manual address.";
                SetProgBarText(message);
                MessageBox.Show(this, message, "Add Address", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            int typeIndex = FindManualAddressTypeIndex(typeName, byteSize);
            if (typeIndex < 0)
            {
                MessageBox.Show(this, "Could not match the selected type to a search result type.", "Add Address", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            byte[] value = new byte[byteSize];
            if (!Form1.apiGetMem(address, ref value))
            {
                string message = "Could not read the current value at 0x" + address.ToString("X8") + ".";
                SetProgBarText(message);
                MessageBox.Show(this, message, "Add Address", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            int existingIndex = FindManualSearchResultIndex(address, typeIndex);
            SearchListView.SearchListViewItem item = new SearchListView.SearchListViewItem();
            item.addr = (uint)address;
            item.newVal = CloneBytes(value);
            item.oldVal = CloneBytes(value);
            item.align = typeIndex;
            item.refresh = false;
            item.misc = 0;

            if (existingIndex >= 0)
            {
                searchListView1.SetItemAtIndex(item, existingIndex);
                searchListView1.SelectAndShowIndex(existingIndex);
                searchListView1.Refresh();
                SetProgBarText("Manual address 0x" + address.ToString("X8") + " already exists; refreshed value.");
                return true;
            }

            searchListView1.AddVisibleItem(item);
            SetProgBarText("Added manual address 0x" + address.ToString("X8") + " as " + typeName + ". Results: " + searchListView1.TotalCount.ToString("N0"));
            return true;
        }

        private int FindManualAddressTypeIndex(string typeName, int byteSize)
        {
            string normalizedTypeName = NormalizeManualTypeName(typeName);
            for (int index = 0; index < SearchTypes.Count; index++)
            {
                ncSearchType type = SearchTypes[index];
                if (type.ByteSize == byteSize &&
                    String.Equals(NormalizeManualTypeName(type.Name), normalizedTypeName, StringComparison.OrdinalIgnoreCase))
                {
                    return index;
                }
            }

            return -1;
        }

        private static string NormalizeManualTypeName(string typeName)
        {
            return String.IsNullOrEmpty(typeName)
                ? String.Empty
                : typeName.Replace(" ", "").ToLowerInvariant();
        }

        private int FindManualSearchResultIndex(ulong address, int typeIndex)
        {
            SearchListView.SearchListViewItem[] items = searchListView1.GetItemsArray();
            for (int index = 0; index < items.Length; index++)
            {
                if (items[index].addr == (uint)address && items[index].align == typeIndex)
                    return index;
            }

            return -1;
        }

        private static byte[] CloneBytes(byte[] bytes)
        {
            if (bytes == null)
                return null;

            byte[] clone = new byte[bytes.Length];
            Array.Copy(bytes, clone, bytes.Length);
            return clone;
        }
    }
}
