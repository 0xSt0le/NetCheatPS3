using System;
using System.Collections.Generic;

namespace NetCheatPS3
{
    public partial class SearchControl
    {
        private void SimplifySearchTypes()
        {
            if (SearchTypes == null || SearchTypes.Count == 0)
                return;

            NormalizeTextTypeName();
            NormalizeTextTypeExceptions();

            string[] desiredOrder = new string[]
            {
                "1 byte",
                "2 bytes",
                "4 bytes",
                "8 bytes",
                "X bytes",
                "Float",
                "Double",
                "String"
            };

            List<ncSearchType> source = new List<ncSearchType>(SearchTypes);
            List<ncSearchType> ordered = new List<ncSearchType>();
            HashSet<string> added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < desiredOrder.Length; i++)
            {
                for (int j = 0; j < source.Count; j++)
                {
                    if (!String.Equals(source[j].Name, desiredOrder[i], StringComparison.OrdinalIgnoreCase))
                        continue;

                    ordered.Add(source[j]);
                    added.Add(source[j].Name);
                    break;
                }
            }

            for (int i = 0; i < source.Count; i++)
            {
                if (String.IsNullOrWhiteSpace(source[i].Name))
                    continue;

                if (added.Contains(source[i].Name))
                    continue;

                ordered.Add(source[i]);
                added.Add(source[i].Name);
            }

            SearchTypes.Clear();
            SearchTypes.AddRange(ordered);
        }

        private void NormalizeTextTypeName()
        {
            for (int i = 0; i < SearchTypes.Count; i++)
            {
                ncSearchType type = SearchTypes[i];

                if (String.Equals(type.Name, "Text", StringComparison.OrdinalIgnoreCase))
                {
                    type.Name = "String";
                    SearchTypes[i] = type;
                }
            }
        }

        private void NormalizeTextTypeExceptions()
        {
            if (SearchComparisons == null)
                return;

            for (int i = 0; i < SearchComparisons.Count; i++)
            {
                ncSearcher searcher = SearchComparisons[i];

                if (searcher.Exceptions == null)
                {
                    searcher.Exceptions = new string[0];
                    SearchComparisons[i] = searcher;
                    continue;
                }

                bool changed = false;
                string[] exceptions = new string[searcher.Exceptions.Length];

                for (int j = 0; j < searcher.Exceptions.Length; j++)
                {
                    string value = searcher.Exceptions[j];

                    if (String.Equals(value, "Text", StringComparison.OrdinalIgnoreCase))
                    {
                        value = "String";
                        changed = true;
                    }

                    exceptions[j] = value;
                }

                if (changed)
                {
                    searcher.Exceptions = exceptions;
                    SearchComparisons[i] = searcher;
                }
            }
        }
    }
}