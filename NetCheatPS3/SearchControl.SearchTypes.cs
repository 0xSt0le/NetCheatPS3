using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NetCheatPS3
{
    public partial class SearchControl
    {
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
            PopulateCleanSearchModeDropdown();
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
            ncS.Exceptions = new string[] { "String" };
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
            ncS.Exceptions = new string[] { "1 byte", "2 bytes", "8 bytes", "Array of Bytes", "String", "Float", "Double" };
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
            ncS.Exceptions = new string[] { "Array of Bytes", "String" };
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
            ncS.Exceptions = new string[] { "String" };
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
            ncS.Exceptions = new string[] { "String" };
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
            ncS.Exceptions = new string[] { "String" };
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
            ncS.Exceptions = new string[] { "String" };
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
            ncS.Exceptions = new string[] { "1 byte", "2 bytes", "8 bytes", "Array of Bytes", "String", "Float", "Double" };
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
            ncS.Exceptions = new string[] { "String", "Float", "Double" };
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
            ncST.DefaultValue = "";
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
            ncST.DefaultValue = "";
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
            ncST.DefaultValue = "";
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
            ncST.DefaultValue = "";
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
            ncST.Name = "Array of Bytes";
            ncST.ListColumnNames = new string[] { "Address", "Value", "Dec", "Type" };
            ncST.ToItem = new SearchToItem(standardByte_ToItem);
            ncST.BAToString = new ByteAToString(sTypeXB_ToString);
            ncST.CheckboxName = "";
            ncST.DefaultValue = "";
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
            ncST.Name = "String";
            ncST.ListColumnNames = new string[] { "Address", "String", "Invalid", "Type" };
            ncST.ToItem = new SearchToItem(standardByte_ToItem);
            ncST.BAToString = new ByteAToString(sTypeText_ToString);
            ncST.CheckboxName = "Match Case";
            ncST.DefaultValue = "";
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
            ncST.DefaultValue = "";
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
            ncST.DefaultValue = "";
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
    }
}
