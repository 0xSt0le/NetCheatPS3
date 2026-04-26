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

        private void Form1_Focused(object sender, EventArgs e)
        {
            if (Form1.Instance.ContainsFocus)
            {
                if (ProgressBar.progTaskBarError || (searchControl1.searchMemory.Text != "Stop" && searchControl1.nextSearchMem.Text != "Stop"))
                {
                    TaskbarProgress.SetState(this.Handle, TaskbarProgress.TaskbarStates.NoProgress);
                    ProgressBar.progTaskBarError = false;
                }
            }
        }

        /* Everything else because I have no organization skills... */
        private void Form1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            isClosing = true;
            ConstantLoop = 2;
            codes.ExitConstWriter = true;
            this.statusLabel1.Text = "Disconnected";

            //Close all plugins
            foreach (PluginForm a in pluginForm)
            {
                try
                {
                    a.Dispose();
                    a.Close();
                }
                catch { }
            }

            if (refPlugin.Text == "Close Plugins")
                Global.Plugins.ClosePlugins();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            if (IntPtr.Size == 8)
            {
                //MessageBox.Show("This is the 64 bit version of NetCheatPS3.\nThis version DOES NOT work with CCAPI 2.5! It is not my fault, if you want CCAPI to support 64 bit applications then please bug Enstone about it.\nThanks.");
            }

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
                        curAPI = Global.APIs.AvailableAPIs.GetIndex(apiDLL);
                        curAPI.Instance.Initialize();
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
                            apiC = 0;
                            apiName = "Control Console API (2.60)";
                            foreach (Types.AvailableAPI api in Global.APIs.AvailableAPIs)
                            {
                                if ((api.Instance.Name + " (" + api.Instance.Version + ")") == apiName)
                                {
                                    //apiDLL = apiC;
                                    curAPI = api;
                                    break;
                                }
                                apiC++;
                            }
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

        public void HandleFocusControls(Control.ControlCollection focCtrl)
        {
            foreach (Control ctrl in focCtrl)
            {
                if (ctrl.Controls != null && ctrl.Controls.Count > 0)
                    HandleFocusControls(ctrl.Controls);

                ctrl.GotFocus += new EventHandler(Form1_Focused);
            }
        }

        private void LoadAPIs()
        {
            apiList.Items.Clear();

            if (System.IO.Directory.Exists(Application.StartupPath + @"\APIs") == false)
                return;

            //Delete any excess NCAppInterface.dll's (result of a build and not a copy)
            foreach (string file in System.IO.Directory.GetFiles(Application.StartupPath + @"\APIs", "NCAppInterface.dll", System.IO.SearchOption.AllDirectories))
                System.IO.File.Delete(file);

            //Call the find apis routine, to search in our APIs Folder
            Global.APIs.FindAPIs(Application.StartupPath + @"\APIs");

            //Load apis
            foreach (Types.AvailableAPI apiOn in Global.APIs.AvailableAPIs)
            {
                apiList.Items.Add(apiOn.Instance.Name + " (" + apiOn.Instance.Version + ")");
            }

            if (apiList.Items.Count > 0)
                apiList.SelectedIndex = 0;
        }

        private void refPlugin_Click(object sender, EventArgs e)
        {
            if (refPlugin.Text == "Close Plugins")
            {
                foreach (PluginForm pF in pluginForm)
                {
                    pF.Close();
                    //pF = null;
                }

                Global.Plugins.ClosePlugins();
                //TabCon.SelectedIndex = 0;

                pluginList.Items.Clear();

                refPlugin.Text = "Load Plugins";
                codes.ConstCodes = new List<codes.ConstCode>();
            }
            else
            {
                int x = 0;

                //Close any open plugins
                Global.Plugins.ClosePlugins();
                pluginList.Items.Clear();

                if (System.IO.Directory.Exists(Application.StartupPath + @"\Plugins") == false)
                    return;

                //Delete any excess PluginInterface.dll's (result of a build and not a copy)
                foreach (string file in System.IO.Directory.GetFiles(Application.StartupPath + @"\Plugins", "PluginInterface.dll", System.IO.SearchOption.AllDirectories))
                    System.IO.File.Delete(file);

                //Call the find plugins routine, to search in our Plugins Folder
                Global.Plugins.FindPlugins(Application.StartupPath + @"\Plugins");

                //Load plugins
                pluginForm = Global.Plugins.GetPlugin(ncBackColor, ncForeColor);
                Array.Resize(ref pluginForm, pluginForm.Length + 1);
                pluginForm[pluginForm.Length - 1] = new PluginForm();
                pluginForm[pluginForm.Length - 1].plugAuth = snapshot.author;
                pluginForm[pluginForm.Length - 1].plugDesc = snapshot.desc;
                pluginForm[pluginForm.Length - 1].plugName = snapshot.name;
                pluginForm[pluginForm.Length - 1].plugText = snapshot.tabName;
                pluginForm[pluginForm.Length - 1].plugVers = snapshot.version;

                if (pluginForm != null)
                {
                    Array.Resize(ref pluginFormActive, pluginForm.Length);
                    for (x = 0; x < pluginForm.Length; x++)
                    {
                        pluginForm[x].Tag = x;
                        pluginForm[x].FormClosing += new FormClosingEventHandler(HandlePlugin_Closing);
                        pluginList.Items.Add(pluginForm[x].plugText);
                    }
                }


                //Fixes a bug that causes the BackColor to be white after adding another TabPage
                RangeTab.BackColor = ncForeColor;
                SearchTab.BackColor = ncForeColor;
                CodesTab.BackColor = ncForeColor;
                Application.DoEvents();
                RangeTab.BackColor = ncBackColor;
                SearchTab.BackColor = ncBackColor;
                CodesTab.BackColor = ncBackColor;

                if (pluginForm.Length != 0)
                    pluginList.SelectedIndex = 0;

                refPlugin.Text = "Close Plugins";
            }

            //loadPluginsToolStripMenuItem
            int index = toolStripDropDownButton1.DropDownItems.IndexOfKey("loadPluginsToolStripMenuItem");
            toolStripDropDownButton1.DropDownItems[index].Text = refPlugin.Text;
        }

        private void optButton_Click(object sender, EventArgs e)
        {
            OptionForm oForm = new OptionForm();
            oForm.Show();
        }

        private void TabCon_KeyUp(object sender, KeyEventArgs e)
        {
            if (ProcessKeyBinds(e.KeyData))
                e.SuppressKeyPress = true;
        }

        private bool ProcessKeyBinds(Keys data)
        {
            int match = -1;

            for (int x = 0; x < keyBinds.Length; x++)
                if (keyBinds[x].Equals(data))
                    match = x;
            
            if (match < 0)
                return false;

            switch (match)
            {
                case 0: //Connect And Attach
                    //Connect
                    connectButton_Click(null, null);
                    //Attach if connected
                    if (connected)
                        attachProcessButton_Click(null, null);
                    break;
                case 1: //Disconnect
                    ps3Disc_Click(null, null);
                    break;
                case 6: //Toggle Constant Write
                    if (cbListIndex >= 0 && cbListIndex < cbList.Items.Count)
                    {
                        cbList_DoubleClick(null, null);
                    }
                    break;
                case 7: //Write
                    if (cbListIndex >= 0 && cbListIndex < cbList.Items.Count)
                    {
                        cbWrite_Click(null, null);
                    }
                    break;
            }
            return true;
        }

        
        /*
        public void TabCon_DoubleClick(object sender, EventArgs e)
        {
            if (TabCon.SelectedIndex < 3)
                return;

            int ind = 0;
            for (ind = 0; ind < tabs.Length; ind++)
                if (TabCon.TabPages[TabCon.SelectedIndex].Name == tabs[ind].Name)
                    break;
            // Move tab to form and remove the tab
            pluginFormActive[ind] = true;

            //Make new form
            pluginForm[ind] = new PluginForm();
            pluginForm[ind].Controls.Add(TabCon.TabPages[TabCon.SelectedIndex].Controls[0]);
            pluginForm[ind].Size = new Size(470, 410);
            pluginForm[ind].BackColor = ncBackColor;
            pluginForm[ind].ForeColor = ncForeColor;
            pluginForm[ind].tabIndex = ind;
            pluginForm[ind].Dock = DockStyle.Fill;
            pluginForm[ind].tabMax = TabCon.TabPages.Count;
            pluginForm[ind].Text = tabs[ind].Text;
            pluginForm[ind].FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Plugin_Closing);
            TabCon.TabPages.Remove(tabs[ind]);
            pluginForm[ind].Show();
        }
        */

        /* When the plugin closes, tell NetCheat to load the controls back into the tab */
        /*
        private void Plugin_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            int tInd = ((PluginForm)sender).tabIndex;
            int tMax = ((PluginForm)sender).tabMax;
            tabs[tInd].Controls.Clear();
            tabs[tInd].Controls.Add(pluginForm[tInd].Controls[0]);
            //TabCon.TabPages.Add(tabs[tInd]);
            int insVal = (int)tabs[tInd].Tag;
            int x = insVal;
            while (x > 0)
            {
                x--;
                if (pluginFormActive[x] == true)
                    insVal--;
            }

            insVal += 3;
            pluginFormActive[tInd] = false;
            TabCon.TabPages.Insert(insVal, tabs[tInd]);
            TabCon.SelectedIndex = insVal;
        }
        */

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Oemtilde)
            {
                try
                {
                    sRecognize.RecognizeAsyncStop();
                    isRecognizing = false;
                }
                catch { }
            }
            if (ProcessKeyBinds(e.KeyData))
                e.SuppressKeyPress = true;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Oemtilde && !isRecognizing)
            {
                try
                {
                    sRecognize.RecognizeAsync(RecognizeMode.Multiple);
                    //sRecognize.Recognize();
                    isRecognizing = true;
                }
                catch { }
            }
        }

        private void HandlePlugin_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            int ind = (int)((PluginForm)sender).Tag;
            if (pluginFormActive[ind])
            {
                e.Cancel = true;
                pluginList.SelectedIndex = ind;
                pluginList_DoubleClick(null, null);
            }
        }

        private void pluginList_DoubleClick(object sender, EventArgs e)
        {
            int ind = pluginList.SelectedIndex;
            if (pluginFormActive[ind]) //Already on
            {
                pluginForm[ind].WindowState = FormWindowState.Normal;
                pluginForm[ind].Visible = false;
                pluginForm[ind].WindowState = FormWindowState.Minimized;
                pluginList.Items[ind] = pluginForm[ind].plugText;
                pluginFormActive[pluginList.SelectedIndex] = false;
            }
            else //Turn on
            {
                pluginFormActive[pluginList.SelectedIndex] = true;
                pluginForm[ind].WindowState = FormWindowState.Normal;
                pluginList.Items[ind] = "+ " + pluginForm[ind].plugText;
                pluginForm[pluginList.SelectedIndex].Show();
                pluginForm[ind].Visible = true;
            }
        }

        snapshot snapShotPlugin = new snapshot();
        private void pluginList_SelectedIndexChanged(object sender, EventArgs e)
        {
            int ind = pluginList.SelectedIndex;
            if (ind < 0)
                return;

            if (pluginList.Items[ind].ToString().IndexOf(snapshot.tabName) >= 0)
            {
                descPlugAuth.Text = "by " + pluginForm[ind].plugAuth;
                descPlugName.Text = pluginForm[ind].plugName;
                descPlugVer.Text = pluginForm[ind].plugVers;
                descPlugDesc.Text = pluginForm[ind].plugDesc;
                pluginForm[ind].Text = pluginForm[ind].plugName + " by " + pluginForm[ind].plugAuth;
                plugIcon.Image = (Bitmap)plugIcon.InitialImage.Clone();

                pluginForm[ind].Controls.Clear();
                pluginForm[ind].Controls.Add(snapShotPlugin);
                pluginForm[ind].Controls[0].Resize += new EventHandler(pluginForm[ind].Plugin_Resize);
                pluginForm[ind].Resize += new EventHandler(snapShotPlugin.snapshot_Resize);

                if (pluginForm[ind].allowColoring)
                {
                    HandlePluginControls(pluginForm[ind].Controls[0].Controls);
                    pluginForm[ind].Controls[0].BackColor = ncBackColor;
                    pluginForm[ind].Controls[0].ForeColor = ncForeColor;
                }
            }
            if (ind >= 0 && pluginForm[ind] != null)
            {
                //Get the selected Plugin
                Types.AvailablePlugin selectedPlugin = Global.Plugins.AvailablePlugins.GetIndex(ind);

                if (selectedPlugin != null && pluginForm[ind].Controls.Count == 0)
                {
                    //Again, if the plugin is found, do some work...

                    //This part adds the plugin's info to the 'Plugin Information:' Frame
                    //this.lblPluginName.Text = selectedPlugin.Instance.Name;
                    //this.lblPluginVersion.Text = "(" + selectedPlugin.Instance.Version + ")";
                    //this.lblPluginAuthor.Text = "By: " + selectedPlugin.Instance.Author;
                    //this.lblPluginDesc.Text = selectedPlugin.Instance.Description;

                    //Clear the current panel of any other plugin controls... 
                    //Note: this only affects visuals.. doesn't close the instance of the plugin
                    //this.pnlPlugin.Controls.Clear();
                    pluginForm[ind].Controls.Clear();

                    //Set the dockstyle of the plugin to fill, to fill up the space provided
                    selectedPlugin.Instance.MainInterface.Dock = DockStyle.Fill;

                    //Finally, add the usercontrol to the tab... Tadah!
                    pluginForm[ind].Controls.Add(selectedPlugin.Instance.MainInterface);
                    pluginForm[ind].Controls[0].Resize += new EventHandler(pluginForm[ind].Plugin_Resize);

                    //TabCon.TabPages[TabCon.SelectedIndex].Controls[0].Dock = DockStyle.None;

                    //Color each control in the tab too
                    //for (int tabCtrl = 0; tabCtrl < TabCon.TabPages[TabCon.SelectedIndex].Controls[0].Controls.Count; tabCtrl++)

                    if (pluginForm[ind].allowColoring)
                    {
                        pluginForm[ind].Controls[0].BackColor = ncBackColor;
                        pluginForm[ind].Controls[0].ForeColor = ncForeColor;
                        HandlePluginControls(pluginForm[ind].Controls[0].Controls);
                    }
                }

                if (pluginForm[ind].Controls.Count > 0 && pluginList.Items[ind].ToString().IndexOf(snapshot.tabName) < 0)
                {
                    descPlugAuth.Text = "by " + selectedPlugin.Instance.Author;
                    descPlugName.Text = selectedPlugin.Instance.Name;
                    descPlugVer.Text = selectedPlugin.Instance.Version;
                    descPlugDesc.Text = selectedPlugin.Instance.Description;
                    if (selectedPlugin.Instance.MainIcon != null && selectedPlugin.Instance.MainIcon.BackgroundImage != null)
                        plugIcon.Image = (Bitmap)selectedPlugin.Instance.MainIcon.BackgroundImage.Clone();
                    else
                        plugIcon.Image = (Bitmap)plugIcon.InitialImage.Clone();
                }
            }
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //CopyResults();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //DeleteSearchResult();
        }

        private void refreshFromPS3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //RefreshSearchResults(1);
        }


        private void shutdownPS3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            curAPI.Instance.Shutdown();
        }

        private void loadPluginsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            refPlugin_Click(null, null);
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            optButton_Click(null, null);
        }

        private void gameStatusStripMenuItem1_Click(object sender, EventArgs e)
        {
            /*
            if (connected)
            {
                PS3TMAPI.UnitStatus ret;
                PS3TMAPI.GetStatus(Target, PS3TMAPI.UnitType.PPU, out ret);
                if (ret == PS3TMAPI.UnitStatus.Stopped)
                {
                    PS3TMAPI.ProcessContinue(Target, ProcessID);
                    gameStatusStripMenuItem1.Text = "Pause Game";
                }
                else
                {
                    PS3TMAPI.ProcessStop(Target, ProcessID);
                    gameStatusStripMenuItem1.Text = "Continue Game";
                }
            }
            else
                MessageBox.Show("Not yet connected!");
            */
        }

private void button1_Click(object sender, EventArgs e)
        {

        }

        string ParseValFromStr(string val)
        {
            val = val.ToLower();
            val = val.Replace(" ", "");
            val = val.Replace("zero", "0");
            val = val.Replace("one", "1");
            val = val.Replace("two", "2");
            val = val.Replace("three", "3");
            val = val.Replace("four", "4");
            val = val.Replace("five", "5");
            val = val.Replace("six", "6");
            val = val.Replace("seven", "7");
            val = val.Replace("eight", "8");
            val = val.Replace("nine", "9");
            val = val.Replace("see", "C");
            val = val.Replace("be", "B");
            return val;
        }

        private void startGameButt_Click(object sender, EventArgs e)
        {
            if (!curAPI.Instance.ContinueProcess())
                MessageBox.Show("Feature not supported with this API!");
        }

        private void pauseGameButt_Click(object sender, EventArgs e)
        {
            if (!curAPI.Instance.PauseProcess())
                MessageBox.Show("Feature not supported with this API!");
        }


        private void pauseGameButt_BackColorChanged(object sender, EventArgs e)
        {
            if (pauseGameButt.BackColor != Color.White)
                pauseGameButt.BackColor = Color.White;
        }

        private void pauseGameButt_ForeColorChanged(object sender, EventArgs e)
        {
            if (pauseGameButt.ForeColor != Color.FromArgb(0, 130, 210))
                pauseGameButt.ForeColor = Color.FromArgb(0, 130, 210);
        }

        private void startGameButt_BackColorChanged(object sender, EventArgs e)
        {
            if (startGameButt.BackColor != Color.White)
                startGameButt.BackColor = Color.White;
        }

        private void startGameButt_ForeColorChanged(object sender, EventArgs e)
        {
            if (startGameButt.ForeColor != Color.FromArgb(0, 130, 210))
                startGameButt.ForeColor = Color.FromArgb(0, 130, 210);
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


        private void endianStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentEndian == Endian.Big)
                CurrentEndian = Endian.Little;
            else
                CurrentEndian = Endian.Big;
        }

        Types.AvailableAPI selAPI;
        private void apiList_SelectedIndexChanged(object sender, EventArgs e)
        {
            int ind = apiList.SelectedIndex;
            if (ind < 0)
                return;

            Types.AvailableAPI api = Global.APIs.AvailableAPIs.GetIndex(ind);
            descAPIAuth.Text = "by " + api.Instance.Author;
            descAPIName.Text = api.Instance.Name;
            descAPIVer.Text = api.Instance.Version;
            descAPIDesc.Text = api.Instance.Description;
            if (api.Instance.Icon != null)
                apiIcon.Image = (Bitmap)api.Instance.Icon.Clone();
            else
                apiIcon.Image = (Bitmap)apiIcon.InitialImage.Clone();

            string[] parts = apiList.Items[ind].ToString().Split(new char[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
            int apiIndex = Global.APIs.AvailableAPIs.GetIndex(parts[0].Trim(), parts[1]);
            selAPI = Global.APIs.AvailableAPIs.GetIndex(apiIndex);
        }

        private void apiList_DoubleClick(object sender, EventArgs e)
        {
            int ind = apiList.SelectedIndex;
            if (ind < 0)
                return;

            if (apiName == apiList.Items[ind].ToString())
                return;
            else
            {
                if (MessageBox.Show("Are you sure you'd like to switch the API to " + apiList.Items[ind].ToString() + "?", "Current API: " + apiName, MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                {
                    //curAPI.Instance.Disconnect();
                    ps3Disc_Click(null, null);
                    string[] parts = apiList.Items[ind].ToString().Split(new char[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
                    int apiDLL = Global.APIs.AvailableAPIs.GetIndex(parts[0].Trim(), parts[1]);
                    curAPI = Global.APIs.AvailableAPIs.GetIndex(apiDLL);
                    curAPI.Instance.Initialize();

                    SaveOptions();
                }
            }
        }

        private void apiIcon_MouseLeave(object sender, EventArgs e)
        {
            int ind = apiList.SelectedIndex;
            if (ind < 0)
                return;

            if (selAPI == null)
                return;

            if (selAPI.Instance.ContactLink != null && selAPI.Instance.ContactLink != "")
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private void apiIcon_Click(object sender, EventArgs e)
        {
            int ind = apiList.SelectedIndex;
            if (ind < 0)
                return;

            if (selAPI == null)
                return;

            if (selAPI.Instance.ContactLink != null && selAPI.Instance.ContactLink != "")
            {
                System.Diagnostics.Process.Start(selAPI.Instance.ContactLink);
            }
        }

        private void apiIcon_MouseEnter(object sender, EventArgs e)
        {
            int ind = apiList.SelectedIndex;
            if (ind < 0)
                return;

            if (selAPI == null)
                return;

            if (selAPI.Instance.ContactLink != null && selAPI.Instance.ContactLink != "")
            {
                Cursor.Current = Cursors.Hand;
            }
        }

        private void apiIcon_MouseMove(object sender, MouseEventArgs e)
        {
            if (selAPI == null)
                return;

            if (selAPI.Instance.ContactLink != null && selAPI.Instance.ContactLink != "")
            {
                Cursor.Current = Cursors.Hand;
            }
        }

        private void apiIcon_MouseHover(object sender, EventArgs e)
        {
            if (selAPI == null)
                return;

            if (selAPI.Instance.ContactLink != null && selAPI.Instance.ContactLink != "")
            {
                Cursor.Current = Cursors.Hand;
            }
        }

        private void configureAPIToolStripMenuItem_Click(object sender, EventArgs e)
        {
            curAPI.Instance.Configure();
        }

    }
}


