using System;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace NetCheatPS3
{
    public partial class SearchControl
    {
        private bool TryBuildScanRequest(
            string actionName,
            out ulong start,
            out ulong stop,
            out int typeIndex,
            out ncSearcher searcher,
            out string[] args)
        {
            start = 0;
            stop = 0;
            typeIndex = -1;
            searcher = new ncSearcher();
            args = new string[0];

            string startText = startAddrTB == null ? "" : (startAddrTB.Text ?? "").Trim();
            string stopText = stopAddrTB == null ? "" : (stopAddrTB.Text ?? "").Trim();

            if (startText.Length == 0)
            {
                ShowScanInputValidationWarning("Start Address is empty.");
                return false;
            }

            if (stopText.Length == 0)
            {
                ShowScanInputValidationWarning("End Address is empty.");
                return false;
            }

            if (!TryParseHexAddress(startText, out start))
            {
                ShowScanInputValidationWarning("Start Address must be a valid hex address.");
                return false;
            }

            if (!TryParseHexAddress(stopText, out stop))
            {
                ShowScanInputValidationWarning("End Address must be a valid hex address.");
                return false;
            }

            if (stop <= start)
            {
                ShowScanInputValidationWarning("End Address must be bigger than Start Address.");
                return false;
            }

            if (searchTypeBox == null || searchTypeBox.SelectedItem == null)
            {
                ShowScanInputValidationWarning("Search type is not selected.");
                return false;
            }

            string selectedTypeName = searchTypeBox.SelectedItem.ToString();
            for (int i = 0; i < SearchTypes.Count; i++)
            {
                if (String.Equals(SearchTypes[i].Name, selectedTypeName, StringComparison.OrdinalIgnoreCase))
                {
                    typeIndex = i;
                    break;
                }
            }

            if (typeIndex < 0 || typeIndex >= SearchTypes.Count)
            {
                ShowScanInputValidationWarning("Selected search type is invalid.");
                return false;
            }

            if (searchNameBox == null || searchNameBox.Items.Count == 0)
            {
                ShowScanInputValidationWarning("Search mode list is empty.");
                return false;
            }

            if (searchNameBox.SelectedIndex < 0)
            {
                if (lastSearchIndex >= 0 && lastSearchIndex < searchNameBox.Items.Count)
                    searchNameBox.SelectedIndex = lastSearchIndex;
                else
                {
                    ShowScanInputValidationWarning("Search mode is not selected.");
                    return false;
                }
            }

            if (searchNameBox.SelectedItem == null)
            {
                ShowScanInputValidationWarning("Search mode is not selected.");
                return false;
            }

            string selectedSearchName = searchNameBox.SelectedItem.ToString();
            searcher = SearchComparisons
                .Where(sc => String.Equals(sc.Name, selectedSearchName, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (searcher.Name == null)
            {
                ShowScanInputValidationWarning("Selected search mode is invalid.");
                return false;
            }

            args = new string[SearchArgs.Count];

            if (HasEmptyVisibleSearchArgText())
            {
                ShowEmptySearchArgWarning();
                return false;
            }

            for (int x = 0; x < args.Length; x++)
                args[x] = SearchArgs[x].GetDefValue();

            string err;
            if (!SearchTypes[typeIndex].areArgsValid(args, out err))
            {
                if (String.IsNullOrWhiteSpace(err))
                    err = "Search value is invalid.";

                ShowScanInputValidationWarning(err);
                return false;
            }

            return true;
        }

        private bool TryParseHexAddress(string text, out ulong value)
        {
            value = 0;

            if (String.IsNullOrWhiteSpace(text))
                return false;

            text = text.Trim();

            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                text = text.Substring(2);

            if (text.Length == 0 || text.Length > 8)
                return false;

            return UInt64.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value);
        }

        private void ShowScanInputValidationWarning(string message)
        {
            if (String.IsNullOrWhiteSpace(message))
                message = "Invalid scan input.";

            try
            {
                Form1.SetMainStatusSafe(message);
            }
            catch
            {
            }

            MessageBox.Show(
                message,
                "NetCheatPS3",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }
}