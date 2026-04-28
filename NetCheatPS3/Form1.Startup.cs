using System;
using System.Drawing;
using System.Windows.Forms;

namespace NetCheatPS3
{
    public partial class Form1
    {
        /* Saves the options to the ncps3.ini file */
        public static void SaveOptions()
        {
            using (System.IO.StreamWriter fd = new System.IO.StreamWriter(settFile, false))
            {
                //KeyBinds
                for (int x = 0; x < keyBinds.Length; x++)
                {
                    string key = keyBinds[x].GetHashCode().ToString();
                    fd.WriteLine(key);
                }

                //Colors
                fd.WriteLine(ncBackColor.A.ToString("X2") + ncBackColor.R.ToString("X2") + ncBackColor.G.ToString("X2") + ncBackColor.B.ToString("X2"));
                fd.WriteLine(ncForeColor.A.ToString("X2") + ncForeColor.R.ToString("X2") + ncForeColor.G.ToString("X2") + ncForeColor.B.ToString("X2"));

                //Recently opened ranges order
                string range = "";
                foreach (int val in rangeOrder)
                    range += val.ToString() + ";";
                if (range == "")
                    range = ";";
                fd.WriteLine(range);

                //Recently opened ranges paths
                range = "";
                foreach (string str in rangeImports)
                    if (str != "")
                        range += str + ";";
                if (range == "")
                    range = ";";
                fd.WriteLine(range);

                //API
                if (curAPI != null)
                    fd.WriteLine(curAPI.Instance.Name + " (" + curAPI.Instance.Version + ")");
                else
                    fd.WriteLine("0");

                //Donation Popup
                fd.WriteLine(ncDonatePopup.ToString());
            }
        }

        private void Main_Load(object sender, EventArgs e)
        {
            codes.ncCodeTypes = new ncCodeType[10];
            //Byte Write
            codes.ncCodeTypes[0].ParseCode = new ParseCode(codes.parseCode);
            codes.ncCodeTypes[0].ExecCode = new ExecCode(codes.execByteWrite);
            codes.ncCodeTypes[0].Command = '0';
            //Text Write
            codes.ncCodeTypes[1].ParseCode = new ParseCode(codes.parseTextCode);
            codes.ncCodeTypes[1].ExecCode = new ExecCode(codes.execByteWrite);
            codes.ncCodeTypes[1].Command = '1';
            //Pointer Execute
            codes.ncCodeTypes[2].ParseCode = new ParseCode(codes.parseCode);
            codes.ncCodeTypes[2].ExecCode = new ExecCode(codes.execPointerExecute);
            codes.ncCodeTypes[2].Command = '6';
            //Conditional Equal To Execute
            codes.ncCodeTypes[3].ParseCode = new ParseCode(codes.parseCode);
            codes.ncCodeTypes[3].ExecCode = new ExecCode(codes.execEQConditionalExecute);
            codes.ncCodeTypes[3].Command = 'D';
            //Conditional Mask Unset Execute
            codes.ncCodeTypes[4].ParseCode = new ParseCode(codes.parseCode);
            codes.ncCodeTypes[4].ExecCode = new ExecCode(codes.execMUConditionalExecute);
            codes.ncCodeTypes[4].Command = 'E';
            //Copy to address
            codes.ncCodeTypes[5].ParseCode = new ParseCode(codes.parseCode);
            codes.ncCodeTypes[5].ExecCode = new ExecCode(codes.execCopyBytes);
            codes.ncCodeTypes[5].Command = 'F';
            //Float Write
            codes.ncCodeTypes[6].ParseCode = new ParseCode(codes.parseCode);
            codes.ncCodeTypes[6].ExecCode = new ExecCode(codes.execByteWrite);
            codes.ncCodeTypes[6].Command = '2';
            //Find Replace
            codes.ncCodeTypes[7].ParseCode = new ParseCode(codes.parseCode);
            codes.ncCodeTypes[7].ExecCode = new ExecCode(codes.execFindReplace);
            codes.ncCodeTypes[7].Command = 'B';
            //Condensed Multiline Code
            codes.ncCodeTypes[8].ParseCode = new ParseCode(codes.parseCode);
            codes.ncCodeTypes[8].ExecCode = new ExecCode(codes.execMultilineCondensed);
            codes.ncCodeTypes[8].Command = '4';
            //Copy Paste
            codes.ncCodeTypes[9].ParseCode = new ParseCode(codes.parseCode);
            codes.ncCodeTypes[9].ExecCode = new ExecCode(codes.execCopyPasteBytes);
            codes.ncCodeTypes[9].Command = 'A';

            LoadAPIs();

            int x = 0;
            //Set the settings file and load the settings
            settFile = Application.StartupPath + ((IntPtr.Size == 8) ? "\\ncps364.ini" : "\\ncps3.ini");
            if (System.IO.File.Exists(settFile))
            {
                string[] settLines = System.IO.File.ReadAllLines(settFile);
                try
                {
                    //Read the keybinds from the array
                    for (x = 0; x < keyBinds.Length; x++)
                        keyBinds[x] = (Keys)int.Parse(settLines[x]);
                }
                catch (Exception ex)
                {
                    CrashLogger.Log("Form1.Main_Load.KeyBinds", ex);
                }
                x = keyBinds.Length;

                try
                {
                    //Read the colors and update the form
                    ncBackColor = Color.FromArgb(int.Parse(settLines[x], System.Globalization.NumberStyles.HexNumber)); BackColor = ncBackColor;
                    ncForeColor = Color.FromArgb(int.Parse(settLines[x + 1], System.Globalization.NumberStyles.HexNumber)); ForeColor = ncForeColor;
                }
                catch (Exception ex)
                {
                    CrashLogger.Log("Form1.Main_Load.Colors", ex);
                }
                x += 2;

                try
                {
                    //Read the recently opened ranges
                    string[] strRangeOrder = settLines[x].Split(';');
                    int size = 0;
                    foreach (string strTMP in strRangeOrder)
                        if (strTMP != "")
                            size++;
                    Array.Resize(ref rangeOrder, size);
                    for (int valRO = 0; valRO < rangeOrder.Length; valRO++)
                        if (strRangeOrder[valRO] != "")
                            rangeOrder[valRO] = int.Parse(strRangeOrder[valRO]);

                    size = 0;
                    rangeImports = settLines[x + 1].Split(';');
                    foreach (string strTMP in strRangeOrder)
                        if (strTMP != "")
                            size++;
                    Array.Resize(ref rangeImports, size);
                    UpdateRecRangeBox();
                }
                catch (Exception ex)
                {
                    CrashLogger.Log("Form1.Main_Load.RecentRanges", ex);
                }
                x += 2;

                try
                {
                    if (settLines[x] == "0")
                    {
                        int apiDLL = Global.APIs.AvailableAPIs.GetIndex("Target Manager API", "420.1.14.7");
                        curAPI = Global.APIs.AvailableAPIs.GetIndex(apiDLL >= 0 ? apiDLL : 0);
                        curAPI?.Instance?.Initialize();
                    }
                    else
                    {
                        apiName = settLines[x];
                        int apiC = 0;
                        foreach (Types.AvailableAPI api in Global.APIs.AvailableAPIs)
                        {
                            if ((api.Instance.Name + " (" + api.Instance.Version + ")") == apiName)
                            {
                                curAPI = api;
                                curAPI.Instance.Initialize();
                                break;
                            }
                            apiC++;
                        }

                        if (apiC == Global.APIs.AvailableAPIs.Count)
                        {
                            int apiDLL = Global.APIs.AvailableAPIs.GetIndex("Target Manager API", "420.1.14.7");
                            curAPI = Global.APIs.AvailableAPIs.GetIndex(apiDLL >= 0 ? apiDLL : 0);
                            curAPI?.Instance?.Initialize();
                        }
                    }
                }
                catch (Exception ex)
                {
                    CrashLogger.Log("Form1.Main_Load.APISelection", ex);
                }
                x++;

                try
                {
                    ncDonatePopup = bool.Parse(settLines[x]);
                }
                catch (Exception ex)
                {
                    CrashLogger.Log("Form1.Main_Load.DonationPopup", ex);
                }
                x++;
            }
            else
            {
                curAPI = Global.APIs.AvailableAPIs.GetIndex(0);
                curAPI?.Instance?.Initialize();
            }

            refPlugin_Click(null, null);

            UpdateConnectionUiState();

            //Add the first Code
            cbList.Items.Add("NEW CODE");
            //Set backcolor
            cbList.Items[0].ForeColor = ncForeColor;
            cbList.Items[0].BackColor = ncBackColor;

            CodeDB cdb = new CodeDB();
            cdb.name = "NEW CODE";
            cdb.state = false;
            Codes.Add(cdb);
            cbList.Items[0].Selected = true;
            cbList.Items[0].Selected = false;

            //Add first range
            string[] a = { "00000000", "FFFFFFFC" };
            ListViewItem b = new ListViewItem(a);
            rangeView.Items.Add(b);

            //Update range array
            UpdateMemArray();

            //Update all the controls on the form
            int ctrl = 0;
            for (ctrl = 0; ctrl < Controls.Count; ctrl++)
            {
                Controls[ctrl].BackColor = ncBackColor;
                Controls[ctrl].ForeColor = ncForeColor;
            }

            //Update all the controls on the tabs
            for (ctrl = 0; ctrl < TabCon.TabPages.Count; ctrl++)
            {
                TabCon.TabPages[ctrl].BackColor = ncBackColor;
                TabCon.TabPages[ctrl].ForeColor = ncForeColor;
                //Color each control in the tab too
                for (int tabCtrl = 0; tabCtrl < TabCon.TabPages[ctrl].Controls.Count; tabCtrl++)
                {
                    TabCon.TabPages[ctrl].Controls[tabCtrl].BackColor = ncBackColor;
                    TabCon.TabPages[ctrl].Controls[tabCtrl].ForeColor = ncForeColor;
                }
            }

            toolStripDropDownButton1.BackColor = Color.Maroon;

            FRManager = new FindReplaceManager();
            FRManager.BackColor = ncBackColor;
            FRManager.ForeColor = ncForeColor;

            HandlePluginControls(searchControl1.Controls);
            HandlePluginControls(FRManager.Controls);
            CreateProbeAddressButton();
        }
    }
}
