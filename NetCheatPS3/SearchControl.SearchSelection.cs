using System;
using System.Collections.Generic;
using System.Drawing;
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
    }
}