using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace NetCheatPS3
{
    public partial class SearchControl
    {
        private bool suppressSearchModeEvents = false;

        private void SimplifySearchComparisonModes()
        {
            if (SearchComparisons == null || SearchComparisons.Count == 0)
                return;

            List<ncSearcher> source = new List<ncSearcher>(SearchComparisons);
            List<ncSearcher> cleaned = new List<ncSearcher>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            AddComparisonIfPresent(source, cleaned, seen, "Equal To", "Exact Value", SearchType.Both, null);
            AddComparisonIfPresent(source, cleaned, seen, "Exact Value", "Exact Value", SearchType.Both, null);

            AddComparisonIfPresent(source, cleaned, seen, "Not Equal To", "Not Equal To", SearchType.Both, null);

            AddComparisonIfPresent(source, cleaned, seen, "Greater Than (S)", "Bigger Than", SearchType.Both, null);
            AddComparisonIfPresent(source, cleaned, seen, "Greater Than", "Bigger Than", SearchType.Both, null);
            AddComparisonIfPresent(source, cleaned, seen, "Bigger Than", "Bigger Than", SearchType.Both, null);

            AddComparisonIfPresent(source, cleaned, seen, "Less Than (S)", "Smaller Than", SearchType.Both, null);
            AddComparisonIfPresent(source, cleaned, seen, "Less Than", "Smaller Than", SearchType.Both, null);
            AddComparisonIfPresent(source, cleaned, seen, "Smaller Than", "Smaller Than", SearchType.Both, null);

            AddComparisonIfPresent(source, cleaned, seen, "Value Between (U)", "Value Between", SearchType.Both, null);
            AddComparisonIfPresent(source, cleaned, seen, "Value Between", "Value Between", SearchType.Both, null);

            AddComparisonIfPresent(source, cleaned, seen, "Unknown Value", "Unknown Value", SearchType.Both, new NextSearch(UnknownValue_NextSearchNotSupported));

            AddComparisonIfPresent(source, cleaned, seen, "Pointer", "Pointer", SearchType.InitialSearchOnly, null);

            AddComparisonIfPresent(source, cleaned, seen, "Joker/Pad Address Finder", "Joker Finder", SearchType.InitialSearchOnly, null);
            AddComparisonIfPresent(source, cleaned, seen, "Joker Finder", "Joker Finder", SearchType.InitialSearchOnly, null);

            AddComparisonIfPresent(source, cleaned, seen, "Increased (S)", "Increased", SearchType.NextSearchOnly, null);
            AddComparisonIfPresent(source, cleaned, seen, "Increased", "Increased", SearchType.NextSearchOnly, null);

            AddComparisonIfPresent(source, cleaned, seen, "Increased By (U)", "Increased By", SearchType.NextSearchOnly, null);
            AddComparisonIfPresent(source, cleaned, seen, "Increased By", "Increased By", SearchType.NextSearchOnly, null);

            AddComparisonIfPresent(source, cleaned, seen, "Decreased (S)", "Decreased", SearchType.NextSearchOnly, null);
            AddComparisonIfPresent(source, cleaned, seen, "Decreased", "Decreased", SearchType.NextSearchOnly, null);

            AddComparisonIfPresent(source, cleaned, seen, "Decreased By (U)", "Decreased By", SearchType.NextSearchOnly, null);
            AddComparisonIfPresent(source, cleaned, seen, "Decreased By", "Decreased By", SearchType.NextSearchOnly, null);

            AddComparisonIfPresent(source, cleaned, seen, "Changed", "Changed", SearchType.NextSearchOnly, null);
            AddComparisonIfPresent(source, cleaned, seen, "Unchanged", "Unchanged", SearchType.NextSearchOnly, null);

            // Preserve any custom/plugin searchers that are not the old clutter.
            for (int i = 0; i < source.Count; i++)
            {
                ncSearcher item = source[i];
                string originalName = item.Name == null ? "" : item.Name.Trim();
                string cleanName = RenameSearchModeForUi(GetCleanSearchModeName(originalName));

                if (ShouldDropSearchMode(originalName, cleanName))
                    continue;

                if (seen.Contains(cleanName))
                    continue;

                item.Name = cleanName;
                EnsureSafeSearcherFields(ref item);
                cleaned.Add(item);
                seen.Add(cleanName);
            }

            SearchComparisons.Clear();
            SearchComparisons.AddRange(cleaned);
        }

        private void AddComparisonIfPresent(
            List<ncSearcher> source,
            List<ncSearcher> output,
            HashSet<string> seen,
            string sourceName,
            string targetName,
            SearchType type,
            NextSearch nextOverride)
        {
            if (seen.Contains(targetName))
                return;

            for (int i = 0; i < source.Count; i++)
            {
                ncSearcher item = source[i];

                if (!String.Equals(item.Name, sourceName, StringComparison.OrdinalIgnoreCase))
                    continue;

                item.Name = targetName;
                item.Type = type;

                if (nextOverride != null)
                    item.NextSearch = nextOverride;

                EnsureSafeSearcherFields(ref item);

                output.Add(item);
                seen.Add(targetName);
                return;
            }
        }

        private void EnsureSafeSearcherFields(ref ncSearcher item)
        {
            if (item.Args == null)
                item.Args = new string[0];

            if (item.Exceptions == null)
                item.Exceptions = new string[0];

            if (item.TypeColumnOverride == null)
                item.TypeColumnOverride = new string[0];
        }

        private string GetCleanSearchModeName(string name)
        {
            if (String.IsNullOrWhiteSpace(name))
                return "";

            string clean = name.Trim();
            clean = clean.Replace(" (Signed)", "");
            clean = clean.Replace(" (Unsigned)", "");
            clean = clean.Replace(" (S)", "");
            clean = clean.Replace(" (U)", "");

            while (clean.IndexOf("  ", StringComparison.Ordinal) >= 0)
                clean = clean.Replace("  ", " ");

            return clean.Trim();
        }

        private string RenameSearchModeForUi(string name)
        {
            if (String.Equals(name, "Equal To", StringComparison.OrdinalIgnoreCase))
                return "Exact Value";

            if (String.Equals(name, "Greater Than", StringComparison.OrdinalIgnoreCase))
                return "Bigger Than";

            if (String.Equals(name, "Less Than", StringComparison.OrdinalIgnoreCase))
                return "Smaller Than";

            if (String.Equals(name, "Joker/Pad Address Finder", StringComparison.OrdinalIgnoreCase))
                return "Joker Finder";

            return name;
        }

        private bool ShouldDropSearchMode(string originalName, string cleanName)
        {
            if (String.IsNullOrWhiteSpace(cleanName))
                return true;

            if (cleanName.Equals("Less Than or Equal", StringComparison.OrdinalIgnoreCase))
                return true;

            if (cleanName.Equals("Greater Than or Equal", StringComparison.OrdinalIgnoreCase))
                return true;

            if (originalName.EndsWith("(U)", StringComparison.OrdinalIgnoreCase))
            {
                if (cleanName.Equals("Smaller Than", StringComparison.OrdinalIgnoreCase))
                    return true;

                if (cleanName.Equals("Bigger Than", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private string[] GetDesiredSearchModeOrder()
        {
            if (isInitialScan)
            {
                return new string[]
                {
                    "Exact Value",
                    "Not Equal To",
                    "Bigger Than",
                    "Smaller Than",
                    "Value Between",
                    "Unknown Value",
                    "Pointer",
                    "Joker Finder"
                };
            }

            return new string[]
            {
                "Exact Value",
                "Not Equal To",
                "Bigger Than",
                "Smaller Than",
                "Value Between",
                "Increased",
                "Increased By",
                "Decreased",
                "Decreased By",
                "Changed",
                "Unchanged",
                "Unknown Value"
            };
        }

        private bool IsSearchModeVisibleNow(ncSearcher searcher, string currentType)
        {
            if (searcher.Exceptions != null)
            {
                for (int i = 0; i < searcher.Exceptions.Length; i++)
                {
                    if (String.Equals(searcher.Exceptions[i], currentType, StringComparison.OrdinalIgnoreCase))
                        return false;
                }
            }

            if (searcher.Type == SearchType.Both)
                return true;

            if (isInitialScan && searcher.Type == SearchType.InitialSearchOnly)
                return true;

            if (!isInitialScan && searcher.Type == SearchType.NextSearchOnly)
                return true;

            return false;
        }

        private void PopulateCleanSearchModeDropdown()
        {
            if (searchNameBox == null)
                return;

            SimplifySearchComparisonModes();

            string previous = "";
            if (searchNameBox.SelectedItem != null)
                previous = searchNameBox.SelectedItem.ToString();

            string currentType = "";
            if (searchTypeBox != null && searchTypeBox.SelectedItem != null)
                currentType = searchTypeBox.SelectedItem.ToString();

            string[] desiredOrder = GetDesiredSearchModeOrder();

            int selected = 0;
            bool foundPrevious = false;

            suppressSearchModeEvents = true;
            searchNameBox.BeginUpdate();

            try
            {
                searchNameBox.Items.Clear();

                for (int i = 0; i < desiredOrder.Length; i++)
                {
                    ncSearcher found;
                    if (!TryGetSearchComparisonByName(desiredOrder[i], out found))
                        continue;

                    if (!IsSearchModeVisibleNow(found, currentType))
                        continue;

                    if (searchNameBox.Items.Contains(found.Name))
                        continue;

                    int newIndex = searchNameBox.Items.Add(found.Name);

                    if (!foundPrevious && String.Equals(found.Name, previous, StringComparison.OrdinalIgnoreCase))
                    {
                        selected = newIndex;
                        foundPrevious = true;
                    }
                }

                if (searchNameBox.Items.Count > 0)
                    searchNameBox.SelectedIndex = selected;
                else
                    searchNameBox.SelectedIndex = -1;
            }
            finally
            {
                searchNameBox.EndUpdate();
                suppressSearchModeEvents = false;
            }
        }

        private bool TryGetSearchComparisonByName(string name, out ncSearcher result)
        {
            for (int i = 0; i < SearchComparisons.Count; i++)
            {
                if (String.Equals(SearchComparisons[i].Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    result = SearchComparisons[i];
                    return true;
                }
            }

            result = new ncSearcher();
            return false;
        }

        private void UnknownValue_NextSearchNotSupported(SearchListView.SearchListViewItem[] old, string[] args)
        {
            SetProgBarText("Unknown Value is for initial scan. Use Changed/Increased/Decreased for Next Scan.");
        }
    }
}