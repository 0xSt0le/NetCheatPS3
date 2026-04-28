using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace NetCheatPS3
{
    public partial class SearchControl
    {
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
            if (searcher.Name == null || type.Name == null)
                return;

            int yOff = 5;

            ResetSearchTypes();
            string[] exceptions = searcher.Exceptions ?? new string[0];
            RemoveSearchTypes(exceptions);
            if (exceptions.Contains(type.Name))
                lastTypeIndex = 0;
            else
                lastTypeIndex = searchTypeBox.Items.IndexOf(type.Name);
            if (lastTypeIndex >= searchTypeBox.Items.Count)
                lastTypeIndex = 0;
            searchTypeBox.SelectedIndex = lastTypeIndex;
            type = SearchTypes.Where(st => st.Name == searchTypeBox.SelectedItem.ToString()).FirstOrDefault();
            if (type.Name == null)
                return;

            if (searcher.Args != null)
            {
                int cnt = 0;
                foreach (string str in searcher.Args)
                {
                    SearchValue a = new SearchValue();
                    string value;
                    bool expectedState;
                    bool currentState;
                    GetInitialSearchArgValue(type, cnt, searchArgsOldValue, searchArgsOldCheck, out value, out expectedState, out currentState);

                    a.SetSValue(str, value, type.CheckboxName, expectedState, currentState, type.CheckboxConvert);
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

            string[] searchArgsOldValue = new string[SearchArgs.Count];
            bool[] searchArgsOldCheck = new bool[SearchArgs.Count];
            for (int sAOV = 0; sAOV < searchArgsOldValue.Length; sAOV++)
            {
                searchArgsOldValue[sAOV] = SearchArgs[sAOV].getValue();
                searchArgsOldCheck[sAOV] = SearchArgs[sAOV].GetState();
            }

            RemoveSearchArgs();
            ncSearcher searcher = SearchComparisons.Where(ns => ns.Name == searchNameBox.Items[searchNameBox.SelectedIndex].ToString()).FirstOrDefault();
            //ncSearchType type = SearchTypes[searchTypeBox.SelectedIndex];
            ncSearchType type = SearchTypes.Where(ns => ns.Name == searchTypeBox.Items[searchTypeBox.SelectedIndex].ToString()).FirstOrDefault();
            if (searcher.Name == null || type.Name == null)
                return;

            int yOff = 5;

            if (searcher.TypeColumnOverride != null && searcher.TypeColumnOverride.Length == 4)
                searchListView1.SetColumnNames(searcher.TypeColumnOverride);
            else
                searchListView1.SetColumnNames(type.ListColumnNames);

            if (searcher.Args != null)
            {
                int cnt = 0;
                foreach (string str in searcher.Args)
                {
                    SearchValue a = new SearchValue();
                    string value;
                    bool expectedState;
                    bool currentState;
                    GetInitialSearchArgValue(type, cnt, searchArgsOldValue, searchArgsOldCheck, out value, out expectedState, out currentState);

                    a.SetSValue(str, value, type.CheckboxName, expectedState, currentState, type.CheckboxConvert);
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

            lastTypeIndex = searchTypeBox.SelectedIndex;
            SearchControl_Resize(null, null);
        }

        private void GetInitialSearchArgValue(
            ncSearchType type,
            int argIndex,
            string[] oldValues,
            bool[] oldStates,
            out string value,
            out bool expectedState,
            out bool currentState)
        {
            value = "";
            expectedState = GetExpectedCheckboxState(type);
            currentState = GetDefaultCheckboxState(type);

            if (oldValues == null || oldStates == null || argIndex >= oldValues.Length || argIndex >= oldStates.Length)
                return;

            string oldValue = oldValues[argIndex];
            bool oldState = oldStates[argIndex];
            if (!IsCompatibleSearchArgValue(type, oldValue, oldState))
                return;

            value = oldValue;
            currentState = GetCompatibleSearchArgState(type, oldValue, oldState);
        }

        private bool GetExpectedCheckboxState(ncSearchType type)
        {
            if (IsNumericSearchType(type))
                return true;

            return false;
        }

        private bool GetDefaultCheckboxState(ncSearchType type)
        {
            if (IsArrayOfBytesSearchType(type))
                return true;

            return false;
        }

        private bool GetCompatibleSearchArgState(ncSearchType type, string value, bool oldState)
        {
            if (IsArrayOfBytesSearchType(type))
                return true;

            if (IsNumericSearchType(type) && LooksLikeHexText(value))
                return true;

            return oldState;
        }

        private bool IsCompatibleSearchArgValue(ncSearchType type, string value, bool state)
        {
            if (String.IsNullOrWhiteSpace(value))
                return false;

            if (IsLegacyDefaultSearchValue(type, value, state))
                return false;

            if (IsArrayOfBytesSearchType(type))
                return IsValidHexByteText(value);

            if (IsNumericSearchType(type))
                return IsValidIntegerSearchText(value, GetCompatibleSearchArgState(type, value, state), type.ByteSize);

            if (type.Name == "Float")
            {
                float parsed;
                return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed);
            }

            if (type.Name == "Double")
            {
                double parsed;
                return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed);
            }

            return true;
        }

        private bool IsLegacyDefaultSearchValue(ncSearchType type, string value, bool state)
        {
            string clean = value == null ? "" : value.Trim();
            if (type.Name == "1 byte")
                return state && clean == "00";
            if (type.Name == "2 bytes")
                return state && clean == "0000";
            if (type.Name == "4 bytes")
                return state && clean == "00000000";
            if (type.Name == "8 bytes")
                return state && clean == "0000000000000000";
            if (type.Name == "Array of Bytes")
                return clean == "00000000";
            if (type.Name == "String")
                return clean == "Example";
            if (type.Name == "Float" || type.Name == "Double")
                return clean == "1.0";

            return false;
        }

        private bool IsNumericSearchType(ncSearchType type)
        {
            return type.Name == "1 byte" ||
                type.Name == "2 bytes" ||
                type.Name == "4 bytes" ||
                type.Name == "8 bytes";
        }

        private bool IsArrayOfBytesSearchType(ncSearchType type)
        {
            return type.Name == "Array of Bytes";
        }

        private bool IsValidIntegerSearchText(string value, bool isHex, int byteSize)
        {
            string clean = value.Trim();
            if (clean.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean.Substring(2);
                isHex = true;
            }

            clean = clean.Replace("_", "").Replace(" ", "");
            if (clean.Length == 0)
                return false;

            ulong parsed;
            NumberStyles styles = isHex ? NumberStyles.AllowHexSpecifier : NumberStyles.None;
            if (!ulong.TryParse(clean, styles, CultureInfo.InvariantCulture, out parsed))
                return false;

            if (byteSize >= 8)
                return true;

            return parsed <= ((1UL << (byteSize * 8)) - 1UL);
        }

        private bool IsValidHexByteText(string value)
        {
            string clean = value.Trim();
            if (clean.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                clean = clean.Substring(2);

            clean = clean.Replace(" ", "").Replace("_", "").Replace("-", "").Replace(",", "");
            if (clean.Length == 0)
                return false;

            for (int index = 0; index < clean.Length; index++)
            {
                char c = clean[index];
                if (!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
                    return false;
            }

            return true;
        }

        private bool LooksLikeHexText(string value)
        {
            if (value == null)
                return false;

            string clean = value.Trim();
            if (clean.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return true;

            for (int index = 0; index < clean.Length; index++)
            {
                char c = clean[index];
                if ((c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'))
                    return true;
            }

            return false;
        }
    }
}
