using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Speech.Recognition;

namespace NetCheatPS3
{

    public partial class Form1 : Form
    {

        public static Form1 Instance = null;
        public static FindReplaceManager FRManager = null;

        public static string versionNum = "4.53";
        public static string apiName = "Target Manager API (420.1.14.7)";

        private static Types.AvailableAPI _curapi;
        public static Types.AvailableAPI curAPI
        {
            get { return _curapi; }
            set
            {
                _curapi = value;
                if (_curapi == null)
                {
                    apiName = "None";
                    Form1.Instance.Text = "NetCheat " + versionNum + " by Dnawrkshp" + ((IntPtr.Size == 8) ? " (64 Bit)" : " (32 Bit)");
                    Form1.Instance.statusLabel2.Text = "API: None";

                    // disable parts of the ui
                    foreach (Control tabPage in Form1.Instance.TabCon.TabPages)
                    {
                        if (tabPage.Name == "apiTab")
                        {
                            Form1.Instance.TabCon.SelectedTab = (TabPage)tabPage;
                            tabPage.Enabled = true;
                        }
                        else
                        {
                            tabPage.Enabled = false;
                        }
                    }
                }
                else
                {
                    Form1.Instance.CurrentEndian = _curapi.Instance.isPlatformLittleEndian ? Endian.Little : Endian.Big;
                    apiName = _curapi.Instance.Name + " (" + _curapi.Instance.Version + ")";
                    Form1.Instance.Text = "NetCheat " + _curapi.Instance.Platform + " " + versionNum + " by Dnawrkshp" + ((IntPtr.Size == 8) ? " (64 Bit)" : " (32 Bit)");
                    Form1.Instance.statusLabel2.Text = "API: " + _curapi.Instance.Name;
                }
            }
        }

        #region NetCheat PS3 Global Variables

        //public static AppDomain pluginDomain = AppDomain.CreateDomain("NC Plugin Domain");

        public static bool PluginAllowColoring = false;
        public static bool DefaultPluginAllowColoring = true;

        bool isRecognizing = false;
        public static bool isClosing = false;
        SpeechRecognitionEngine sRecognize = new SpeechRecognitionEngine();
        public static bool connected = false;
        public static bool attached = false;
        public static int CodesCount = 0; /* Number of codes */
        public static int ConstantLoop = 0; /* 0 = loop but don't exec, 1 = loop and exec, 2 = exit */
        public static bool bComment = false; // If true, all code lines will be skipped until a "*/" is reached
        public static int cbListIndex = 0; /* Current Code open */

        public const int MaxCodes = 1000; /* Max number of codes */

        /* Search variables */
        public const int MaxRes = 10000; /* Max number of results */
        public static ulong SchResCnt = 0; /* Number of results */
        public static int NextSAlign = 0; /* Initial Search sets this to the current alignment */
        public static bool ValHex = false; /* Determines whether the value is in hex or not */
        public static bool NewSearch = true; /* When true, Initial Scan will show */
        public static int CancelSearch = 0; /* When 1 the search will cancel, 2 = stop */
        public static ulong GlobAlign = 0; /* Alignment */
        public static int compMode = 0; /* Comparison type */

        /* Pre-parsed code struct */
        public struct ncCode
        {
            public char codeType;
            public byte[] codeArg2;
            public ulong codeArg1;
            public byte[] codeArg1_BA;
            public uint codeArg0;
        }

        public delegate ncCode ParseCode(string code);
        public delegate int ExecCode(int cnt, ref CodeDB cDB, bool isCWrite);
        public struct ncCodeType
        {
            public ParseCode ParseCode;         //Function that parses the code
            public ExecCode ExecCode;           //Function that executes the code
            public char Command;                //What defines the code type
        }

        /* Code struct */
        public struct CodeDB
        {          /* Structure for a single code */
            public bool state;              /* Determines whether to write constantly or not */
            public string name;             /* Name of Code */
            public string codes;            /* Holds codes string */
            public ncCode[] CData;          /* Holds codes in parsed format */
            public string filename;         /* For use with the 'Save' button */
            public ncCode[] backUp;         /* Holds what the memory originally held before writing */
        };


        /* List result struct */
        public struct ListRes
        {
            public string Addr;
            public string HexVal;
            public string DecVal;
            public string AlignStr;
        }

        /* Codes Array */
        public static List<CodeDB> Codes = new List<CodeDB>();

        /* Search Types */
        public const int compEq = 0;        /* Equal To */
        public const int compNEq = 1;       /* Not Equal To */
        public const int compLT = 2;        /* Less Than Signed */
        public const int compLTE = 3;       /* Less Than Or Equal To Signed */
        public const int compGT = 4;        /* Greater Than Signed */
        public const int compGTE = 5;       /* Greater Than Or Equal To Signed */
        public const int compVBet = 6;      /* Value Between */
        public const int compINC = 7;       /* Increased Value Signed */
        public const int compDEC = 8;       /* Decreased Value Signed */
        public const int compChg = 9;       /* Changed Value */
        public const int compUChg = 10;     /* Unchanged Value */
        public const int compLTU = 11;      /* Less Than Unsigned */
        public const int compLTEU = 12;     /* Less Than Or Equal To Unsigned */
        public const int compGTU = 13;      /* Greater Than Unsigned */
        public const int compGTEU = 14;     /* Greater Than Or Equal To Unsigned */


        public const int compANEq = 20;     /* And Equal (used with E joker type) */

        /* Input Box Argument Structure */
        public struct IBArg
        {
            public string label;
            public string defStr;
            public string retStr;
        };

        /* Little and Big Endian */
        public enum Endian
        {
            Little,
            Big
        }

        private static Endian _curEndian = Endian.Big;
        public static bool doFlipArray = false;
        public Endian CurrentEndian
        {
            get { return _curEndian; }
            set
            {
                _curEndian = value;

                if (_curEndian == Endian.Big)
                    endianStripMenuItem.Checked = true;
                else
                    endianStripMenuItem.Checked = false;


                doFlipArray = false;
                if (CurrentEndian == Endian.Big && BitConverter.IsLittleEndian)
                    doFlipArray = true;
                else if (CurrentEndian == Endian.Little && !BitConverter.IsLittleEndian)
                    doFlipArray = true;
            }
        }

        /* ForeColor and BackColor */
        public static Color ncBackColor = Color.Black;
        public static Color ncForeColor = Color.FromArgb(0, 130, 210);
        /* Keybind arrays */
        public static Keys[] keyBinds = new Keys[8];
        public static string[] keyNames = new string[] {
            "Connect And Attach", "Disconnect",
            "Initial Scan", "Next Scan", "Stop", "Refresh Results",
            "Toggle Constant Write", "Write"
        };
        /* Whether to have the donations form pop up or not */
        public static bool ncDonatePopup = true;

        /* Settings file path */
        public static string settFile = "";
        /* String array that holds each range import */
        public static string[] rangeImports = new string[0];
        /* Int array that defines the order of the recent ranges */
        public static int[] rangeOrder = new int[0];

        /* Plugin form related arrays */
        public static PluginForm[] pluginForm = new PluginForm[0];
        public static bool[] pluginFormActive = new bool[0];
        public static int setplugWindow = -1;

        /* Delete block struct */
        public struct deleteArr
        {
            public int start;
            public int size;
        }

        public struct OnlineCode
        {
            public string id;
            public int ver;
        }

        /* Constant writing thread */
        public static System.Threading.Thread tConstWrite = new System.Threading.Thread(new System.Threading.ThreadStart(codes.BeginConstWriting));
        #endregion

        #region Interface Functions

        /* API related functions */
        public static void apiSetMem(ulong addr, byte[] val) //Set the memory
        {
            if (val != null && connected)
            {

                byte[] newV = new byte[val.Length];
                Array.Copy(val, 0, newV, 0, val.Length);
                newV = misc.notrevif(newV);
                curAPI.Instance.SetBytes(addr, newV);
            }
            //PS3TMAPI.ProcessSetMemory(Target, PS3TMAPI.UnitType.PPU, Form1.ProcessID, 0, addr, val);
        }

        public static bool apiGetMem(ulong addr, ref byte[] val) //Gets the memory as a byte array
        {
            bool ret = false;
            if (val != null && connected)
            {
                ret = curAPI.Instance.GetBytes(addr, ref val);
            }
            return ret;
        }

        public enum ValueType
        {
            CHAR,
            SHORT,
            INT,
            LONG,
            USHORT,
            UINT,
            ULONG,
            STRING,
            FLOAT,
            DOUBLE
        }

        public static object getVal(uint addr, ValueType type)
        {
            return getVal((ulong)addr, type);
        }

        public static object getVal(ulong addr, ValueType type)
        {
            byte[] b;

            switch (type)
            {
                case ValueType.CHAR:
                    b = new byte[1];
                    apiGetMem(addr, ref b);
                    return (char)b[0];
                case ValueType.DOUBLE:
                    b = new byte[8];
                    apiGetMem(addr, ref b);
                    if (doFlipArray)
                        Array.Reverse(b);
                    return BitConverter.ToDouble(b, 0);
                case ValueType.FLOAT:
                    b = new byte[4];
                    apiGetMem(addr, ref b);
                    if (doFlipArray)
                        Array.Reverse(b);
                    return BitConverter.ToSingle(b, 0);
                case ValueType.INT:
                    b = new byte[4];
                    apiGetMem(addr, ref b);
                    if (doFlipArray)
                        Array.Reverse(b);
                    return BitConverter.ToInt32(b, 0);
                case ValueType.LONG:
                    b = new byte[4];
                    apiGetMem(addr, ref b);
                    if (doFlipArray)
                        Array.Reverse(b);
                    return BitConverter.ToInt64(b, 0);
                case ValueType.SHORT:
                    b = new byte[4];
                    apiGetMem(addr, ref b);
                    if (doFlipArray)
                        Array.Reverse(b);
                    return BitConverter.ToInt16(b, 0);
                case ValueType.STRING:
                    b = new byte[256];
                    apiGetMem(addr, ref b);
                    if (doFlipArray)
                        Array.Reverse(b);
                    string valStringRet = "";
                    for (int str = 0; str < 256; str++)
                    {
                        if (b[str] == 0)
                            break;
                        valStringRet += ((char)b[str]).ToString();
                    }
                    return valStringRet;
                case ValueType.UINT:
                    b = new byte[4];
                    apiGetMem(addr, ref b);
                    if (doFlipArray)
                        Array.Reverse(b);
                    return BitConverter.ToUInt32(b, 0);
                case ValueType.ULONG:
                    b = new byte[4];
                    apiGetMem(addr, ref b);
                    if (doFlipArray)
                        Array.Reverse(b);
                    return BitConverter.ToUInt64(b, 0);
                case ValueType.USHORT:
                    b = new byte[4];
                    apiGetMem(addr, ref b);
                    if (doFlipArray)
                        Array.Reverse(b);
                    return BitConverter.ToUInt16(b, 0);
            }

            return 0;
        }

        #endregion

        public Form1()
        {
            InitializeComponent();

            
            InitializeModernRangeUi();Instance = this;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_Closing);
            this.GotFocus += new EventHandler(Form1_Focused);
            HandleFocusControls(this.Controls);

            /* Related to the keybindings */
            connectButton.KeyUp += new KeyEventHandler(Form1_KeyUp);
            attachProcessButton.KeyUp += new KeyEventHandler(Form1_KeyUp);
            ps3Disc.KeyUp += new KeyEventHandler(Form1_KeyUp);
            refPlugin.KeyUp += new KeyEventHandler(Form1_KeyUp);
            optButton.KeyUp += new KeyEventHandler(Form1_KeyUp);
            TabCon.KeyUp += new KeyEventHandler(Form1_KeyUp);

            connectButton.KeyDown += new KeyEventHandler(Form1_KeyDown);
            attachProcessButton.KeyDown += new KeyEventHandler(Form1_KeyDown);
            ps3Disc.KeyDown += new KeyEventHandler(Form1_KeyDown);
            refPlugin.KeyDown += new KeyEventHandler(Form1_KeyDown);
            optButton.KeyDown += new KeyEventHandler(Form1_KeyDown);
            TabCon.KeyDown += new KeyEventHandler(Form1_KeyDown);

            CurrentEndian = Endian.Little;
            CurrentEndian = Endian.Big;
        }

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
                catch { }
                x = keyBinds.Length;

                try
                {
                    //Read the colors and update the form
                    ncBackColor = Color.FromArgb(int.Parse(settLines[x], System.Globalization.NumberStyles.HexNumber)); BackColor = ncBackColor;
                    ncForeColor = Color.FromArgb(int.Parse(settLines[x + 1], System.Globalization.NumberStyles.HexNumber)); ForeColor = ncForeColor;
                }
                catch { }
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
                catch { }
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
                                //apiDLL = apiC;
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
                catch { }
                x++;

                try
                {
                    ncDonatePopup = bool.Parse(settLines[x]);
                }
                catch { }
                x++;
            }
            else
            {
                //apiDLL = 1;
                curAPI = Global.APIs.AvailableAPIs.GetIndex(0);
                curAPI?.Instance?.Initialize();
            }

            refPlugin_Click(null, null);

            attachProcessButton.Enabled = false;

            //Add the first Code
            cbList.Items.Add("NEW CODE");
            //Set backcolor
            cbList.Items[0].ForeColor = ncForeColor;
            cbList.Items[0].BackColor = ncBackColor;

            CodeDB cdb = new CodeDB();
            cdb.name = "NEW CODE";
            cdb.state = false;
            //Codes[CodesCount].name = "NEW CODE";
            //Codes[CodesCount].state = false;
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

        /* Brings up the Input Box with the arguments of a */
        public IBArg[] CallIBox(IBArg[] a)
        {
            InputBox ib = new InputBox();

            ib.Arg = a;
            ib.fmHeight = this.Height;
            ib.fmWidth = this.Width;
            ib.fmLeft = this.Left;
            ib.fmTop = this.Top;
            ib.TopMost = true;
            ib.fmForeColor = ForeColor;
            ib.fmBackColor = BackColor;
            ib.Show();

            while (ib.ret == 0)
            {
                a = ib.Arg;
                Application.DoEvents();
            }
            a = ib.Arg;

            if (ib.ret == 1)
                return a;
            else if (ib.ret == 2)
                return null;

            return null;
        }


        public static void HandlePluginControls(Control.ControlCollection plgCtrl)
        {
            foreach (Control ctrl in plgCtrl)
            {
                //if (ctrl is GroupBox || ctrl is Panel || ctrl is TabControl || ctrl is TabPage||
                //    ctrl is UserControl || ctrl is ListBox || ctrl is ListView)
                if (ctrl.Controls != null && ctrl.Controls.Count > 0)
                {
                    HandlePluginControls(ctrl.Controls);
                }
                if (ctrl is ListView)
                {
                    foreach (ListViewItem ctrlLVI in (ctrl as ListView).Items)
                    {
                        ctrlLVI.BackColor = ncBackColor;
                        ctrlLVI.ForeColor = ncForeColor;
                    }
                }
                
                ctrl.BackColor = ncBackColor;
                ctrl.ForeColor = ncForeColor;
            }
        }

        /*
         * Holy
         * Fucking
         * Shit
         */
        private void Form1_Resize(object sender, EventArgs e)
        {
            int ratio = 0;

            //Tab Control
            {
                TabCon.Size = new Size(this.Width - 40, this.Height - (501 - 393));
                int tHeight = TabCon.SelectedTab.Height, tWidth = TabCon.SelectedTab.Width;
                if (tHeight == 0 || tWidth == 0)
                    return;
                //Tab Page: Codes
                {
                    ratio = (tHeight - (367 - 298)) - cbList.Height;
                    cbList.Height = (tHeight - (367 - 298));
                    cbAdd.Top += ratio;
                    cbRemove.Top += ratio;
                    cbImport.Top += ratio;
                    cbSave.Top += ratio;
                    cbSaveAll.Top += ratio;
                    cbSaveAs.Top += ratio;

                    label5.Width = (tWidth - (453 - 225));
                    cbName.Width = (tWidth - (453 - 225));
                    cbState.Width = (tWidth - (453 - 225));
                    ratio = (tHeight - (367 - 225)) - cbCodes.Height;
                    cbCodes.Height = (tHeight - (367 - 225));
                    cbCodes.Width = (tWidth - (453 - 225));
                    cbWrite.Top += ratio;
                    cbWrite.Width = (tWidth - (453 - 225));
                    cbBManager.Top += ratio;
                    cbResetWrite.Top += ratio;
                    cbBackupWrite.Top += ratio;

                    ratio = (tWidth - cbBManager.Left - 18) / 3;
                    cbBManager.Width = ratio;
                    cbResetWrite.Width = ratio;
                    cbBackupWrite.Width = ratio;
                    cbResetWrite.Left = cbBManager.Left + cbBManager.Width + 5;
                    cbBackupWrite.Left = cbResetWrite.Left + cbResetWrite.Width + 5;
                }
                //Tab Page: Search
                {
                    searchControl1.Size = new Size(tWidth - (461 - 444), tHeight - (393 - 383));
                }
                //Tab Page: Range
                {
                    label1.Width = tWidth - 20;
                    label2.Width = tWidth - 20;
                    rangeView.Height = tHeight - 40;

                    ratio = tWidth - 235 - 8;
                    label3.Width = ratio;
                    recRangeBox.Width = ratio;
                    if (probeAddressButton != null)
                    {
                        int probeWidth = 110;
                        findRangeProgBar.Width = ratio - findRanges.Width - probeWidth - 12;
                        if (findRangeProgBar.Width < 50)
                            findRangeProgBar.Width = 50;

                        findRanges.Left = findRangeProgBar.Left + findRangeProgBar.Width + 6;

                        probeAddressButton.Width = probeWidth;
                        probeAddressButton.Height = findRanges.Height;
                        probeAddressButton.Top = findRanges.Top;
                        probeAddressButton.Left = findRanges.Left + findRanges.Width + 6;
                    }
                    else
                    {
                        findRangeProgBar.Width = ratio - 91 - 6;
                        findRanges.Left = findRangeProgBar.Left + findRangeProgBar.Width + 6;
                    }

                    ratio -= 30;
                    ratio /= 2;

                    RangeUp.Width = ratio;
                    RangeDown.Width = ratio;
                    RangeDown.Left = RangeUp.Left + ratio + 30;
                    ImportRange.Width = ratio;
                    SaveRange.Width = ratio;
                    SaveRange.Left = ImportRange.Left + ratio + 30;
                    AddRange.Width = ratio;
                    RemoveRange.Width = ratio;
                    RemoveRange.Left = AddRange.Left + ratio + 30;

                    ratio = tHeight - 200 - recRangeBox.Height;
                    recRangeBox.Height = tHeight - 200;
                    RangeUp.Top += ratio;
                    RangeDown.Top += ratio;
                    ImportRange.Top += ratio;
                    SaveRange.Top += ratio;
                    AddRange.Top += ratio;
                    RemoveRange.Top += ratio;
                }
                //Tab Page: Plugins
                {
                    pluginList.Height = tHeight - 40;
                    int pWidth = tWidth - (461 - 174);
                    if (pWidth > 250)
                        pWidth = 250;
                    pluginList.Width = pWidth;

                    descPlugName.Left = pluginList.Left + pluginList.Width + 6;
                    descPlugAuth.Left = descPlugName.Left + 22;
                    descPlugVer.Left = descPlugAuth.Left;
                    descPlugDesc.Left = descPlugName.Left + 3;
                    plugIcon.Left = descPlugName.Left;

                    int plugIconHeight = tHeight - (393 - 210);
                    plugIcon.Height = (int)(210f * ((float)plugIconHeight / 210f));
                    plugIcon.Width = (int)(266f * ((float)plugIconHeight / 210f));

                    descPlugDesc.Width = tWidth - descPlugDesc.Left;
                }
                //Tab Page: APIs
                {
                    apiList.Height = tHeight - 40;
                    int pWidth = tWidth - (461 - 174);
                    if (pWidth > 250)
                        pWidth = 250;
                    apiList.Width = pWidth;

                    descAPIName.Left = apiList.Left + apiList.Width + 6;
                    descAPIAuth.Left = descAPIName.Left + 22;
                    descAPIVer.Left = descAPIAuth.Left;
                    descAPIDesc.Left = descAPIName.Left + 3;
                    apiIcon.Left = descAPIName.Left;

                    int APIIconHeight = tHeight - (393 - 210);
                    apiIcon.Height = (int)(210f * ((float)APIIconHeight / 210f));
                    apiIcon.Width = (int)(266f * ((float)APIIconHeight / 210f));

                    descAPIDesc.Width = tWidth - descAPIDesc.Left;
                }
            }

            //Buttons on main form
            {

            }

            this.HScroll = false;
            this.VScroll = false;
            this.AutoScroll = false;
        }


    }
}
