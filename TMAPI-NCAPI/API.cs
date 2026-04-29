using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NCAppInterface;

namespace TMAPI_NCAPI
{
    public class API : IAPI, IAddressAccessLoggerApi
    {

        [DllImport("user32.dll")]
        internal static extern IntPtr SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public API()
		{

		}
		
		//Declarations of all our internal API variables
        string myName = "Target Manager API";
        string myDescription = "NetCheat API for the Target Manager API (PS3).\n\nDEX only!\nRequires ProDG Target Manager to be installed on PC.";
        string myAuthor = "Dnawrkshp and iMCSx";
        string myVersion = "420.1.14.7";
        string myPlatform = "PS3";
        string myContactLink = "";
        System.Drawing.Image myIcon = null;

        /// <summary>
        /// Website link to contact info or download (leave "" if no link)
        /// </summary>
        public string ContactLink
        {
            get { return myContactLink; }
        }

        /// <summary>
        /// Name of the API (displayed on title bar of NetCheat)
        /// </summary>
        public string Name
        {
            get { return myName; }
        }

		/// <summary>
		/// Description of the API's purpose
		/// </summary>
		public string Description
		{
			get {return myDescription;}
		}

		/// <summary>
        /// Author(s) of the API
        /// </summary>
        public string Author
        {
            get { return myAuthor; }

        }

        /// <summary>
        /// Current version of the API
        /// </summary>
		public string Version
		{
			get	{return myVersion;}
		}

        /// <summary>
        /// Name of platform (abbreviated, i.e. PC, PS3, XBOX, iOS)
        /// </summary>
        public string Platform
        {
            get { return myPlatform; }
        }

        /// <summary>
        /// Returns whether the platform is little endian by default
        /// </summary>
        public bool isPlatformLittleEndian
        {
            get { return false; }
        }

        /// <summary>
        /// Icon displayed along with the other data in the API tab, if null NetCheat icon is displayed
        /// </summary>
        public System.Drawing.Image Icon
        {
            get { return myIcon; }
        }

        /// <summary>
        /// Read bytes from memory of target process.
        /// Returns read bytes into bytes array.
        /// Returns false if failed.
        /// </summary>
        public bool GetBytes(ulong address, ref byte[] bytes)
        {
            if (_tmapi == null)
                _tmapi = new TMAPI();

            return _tmapi.GetMemory((uint)address, bytes) == PS3TMAPI.SNRESULT.SN_S_OK;
        }

        /// <summary>
        /// Write bytes to the memory of target process.
        /// </summary>
        public void SetBytes(ulong address, byte[] bytes)
        {
            if (_tmapi == null)
                _tmapi = new TMAPI();

            _tmapi.SetMemory((uint)address, bytes);
        }

        /// <summary>
        /// Shutdown game or platform
        /// </summary>
        public void Shutdown()
        {
            if (_tmapi == null)
                _tmapi = new TMAPI();

            _tmapi.PowerOff(true);
        }

        private TMAPI _tmapi;
        private readonly object addressAccessLoggerLock = new object();
        private AddressAccessLoggerSession activeAddressAccessLogger;

        public bool SupportsAddressAccessLogging
        {
            get { return true; }
        }

        public IAddressAccessLoggerSession StartAddressAccessLogger(ulong address, AddressAccessMode mode, Action<AddressAccessHit> hitCallback)
        {
            if (_tmapi == null)
                _tmapi = new TMAPI();

            lock (addressAccessLoggerLock)
            {
                if (activeAddressAccessLogger != null && activeAddressAccessLogger.IsRunning)
                    activeAddressAccessLogger.Stop();

                activeAddressAccessLogger = new AddressAccessLoggerSession(this, _tmapi, address, mode, hitCallback);
                return activeAddressAccessLogger;
            }
        }

        private void ClearAddressAccessLoggerSession(AddressAccessLoggerSession session)
        {
            lock (addressAccessLoggerLock)
            {
                if (Object.ReferenceEquals(activeAddressAccessLogger, session))
                    activeAddressAccessLogger = null;
            }
        }

        private sealed class AddressAccessLoggerSession : IAddressAccessLoggerSession
        {
            private readonly API owner;
            private readonly TMAPI tmapi;
            private readonly Action<AddressAccessHit> hitCallback;
            private readonly object sync = new object();
            private readonly PS3TMAPI.TargetEventCallback targetEventCallback;
            private bool isRunning;
            private bool registeredEvents;
            private bool savedOldDabr;
            private ulong oldDabr;
            private ulong rawDabr;

            public AddressAccessLoggerSession(API owner, TMAPI tmapi, ulong address, AddressAccessMode mode, Action<AddressAccessHit> hitCallback)
            {
                this.owner = owner;
                this.tmapi = tmapi;
                this.hitCallback = hitCallback;
                Address = address;
                Mode = mode;
                targetEventCallback = HandleTargetEvents;

                Start();
            }

            public ulong Address { get; private set; }

            public AddressAccessMode Mode { get; private set; }

            public bool IsRunning
            {
                get
                {
                    lock (sync)
                    {
                        return isRunning;
                    }
                }
            }

            public void Stop()
            {
                bool shouldStop;
                lock (sync)
                {
                    shouldStop = isRunning;
                    isRunning = false;
                }

                if (!shouldStop)
                    return;

                try
                {
                    if (savedOldDabr)
                        tmapi.SetDABR(oldDabr);
                    else
                        tmapi.SetDABR(0);
                }
                catch (Exception ex)
                {
                    PublishError("Failed to restore DABR: " + ex.Message);
                }

                if (registeredEvents)
                {
                    try
                    {
                        tmapi.CancelTargetEvents();
                    }
                    catch (Exception ex)
                    {
                        PublishError("Failed to cancel TMAPI target events: " + ex.Message);
                    }
                }

                owner.ClearAddressAccessLoggerSession(this);
            }

            public void Dispose()
            {
                Stop();
            }

            private void Start()
            {
                PS3TMAPI.SNRESULT result = tmapi.GetDABR(out oldDabr);
                savedOldDabr = PS3TMAPI.SUCCEEDED(result);

                object userData = null;
                result = tmapi.RegisterTargetEventHandler(targetEventCallback, ref userData);
                if (!PS3TMAPI.SUCCEEDED(result))
                    throw new InvalidOperationException("TMAPI RegisterTargetEventHandler failed: " + result.ToString());

                registeredEvents = true;

                rawDabr = BuildRawDabr(Address, Mode);
                result = tmapi.SetDABR(rawDabr);
                if (!PS3TMAPI.SUCCEEDED(result))
                {
                    registeredEvents = false;
                    try
                    {
                        tmapi.CancelTargetEvents();
                    }
                    catch
                    {
                    }

                    throw new InvalidOperationException("TMAPI SetDABR failed: " + result.ToString());
                }

                lock (sync)
                {
                    isRunning = true;
                }
            }

            private static ulong BuildRawDabr(ulong address, AddressAccessMode mode)
            {
                ulong aligned = address & ~0x7UL;

                // Experimental until runtime-tested: if read/write separation fires the
                // same way for both modes, SNPS3SetHWBreakPointData may be needed later.
                if (mode == AddressAccessMode.Write)
                    return aligned | 0x4UL | 0x2UL;

                return aligned | 0x4UL | 0x1UL;
            }

            private void HandleTargetEvents(int target, PS3TMAPI.SNRESULT result, PS3TMAPI.TargetEvent[] targetEventList, object userData)
            {
                if (!IsRunning || targetEventList == null)
                    return;

                foreach (PS3TMAPI.TargetEvent targetEvent in targetEventList)
                {
                    if (targetEvent.Type != PS3TMAPI.TargetEventType.TargetSpecific)
                        continue;

                    PS3TMAPI.TargetSpecificEvent specific = targetEvent.TargetSpecific;
                    if (specific.Data.Type != PS3TMAPI.TargetSpecificEventType.PPUExcDabrMatch)
                        continue;

                    HandleDabrMatch(specific);
                }
            }

            private void HandleDabrMatch(PS3TMAPI.TargetSpecificEvent specific)
            {
                AddressAccessHit hit = new AddressAccessHit();
                hit.WatchedAddress = Address;
                hit.Mode = Mode;
                hit.RawDabr = rawDabr;
                hit.Timestamp = DateTime.Now;

                PS3TMAPI.PPUExceptionData exceptionData = specific.Data.PPUException;
                hit.ThreadId = exceptionData.ThreadID;
                hit.ProgramCounter = exceptionData.PC;
                hit.StackPointer = exceptionData.SP;

                if ((hit.ProgramCounter == 0 || hit.ThreadId == 0) && specific.Data.PPUDataMatException.ThreadID != 0)
                {
                    hit.ThreadId = specific.Data.PPUDataMatException.ThreadID;
                    hit.ProgramCounter = specific.Data.PPUDataMatException.PC;
                    hit.StackPointer = specific.Data.PPUDataMatException.SP;
                }

                hit.InstructionBytes = ReadInstructionBytes(hit.ProgramCounter);
                PublishHit(hit);

                if (hit.ThreadId != 0)
                {
                    PS3TMAPI.SNRESULT cleanResult = tmapi.ThreadExceptionClean(hit.ThreadId);
                    if (!PS3TMAPI.SUCCEEDED(cleanResult))
                        PublishError("ThreadExceptionClean failed: " + cleanResult.ToString());

                    PS3TMAPI.SNRESULT continueResult = tmapi.ThreadContinue(hit.ThreadId);
                    if (!PS3TMAPI.SUCCEEDED(continueResult))
                        PublishError("ThreadContinue failed: " + continueResult.ToString());
                }
            }

            private byte[] ReadInstructionBytes(ulong programCounter)
            {
                if (programCounter == 0)
                    return new byte[0];

                byte[] bytes = new byte[4];
                try
                {
                    PS3TMAPI.SNRESULT result = tmapi.GetMemory((uint)programCounter, bytes);
                    if (PS3TMAPI.SUCCEEDED(result))
                        return bytes;

                    PublishError("Instruction read failed: " + result.ToString());
                }
                catch (Exception ex)
                {
                    PublishError("Instruction read failed: " + ex.Message);
                }

                return new byte[0];
            }

            private void PublishError(string error)
            {
                AddressAccessHit hit = new AddressAccessHit();
                hit.WatchedAddress = Address;
                hit.Mode = Mode;
                hit.RawDabr = rawDabr;
                hit.Timestamp = DateTime.Now;
                hit.Error = error;
                PublishHit(hit);
            }

            private void PublishHit(AddressAccessHit hit)
            {
                try
                {
                    if (hitCallback != null)
                        hitCallback(hit);
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Connects to target.
        /// If platform doesn't require connection, just return true.
        /// </summary>
        public bool Connect()
        {
            if (_tmapi == null)
                _tmapi = new TMAPI();

            return _tmapi.ConnectTarget();
        }

        /// <summary>
        /// Disconnects from target.
        /// </summary>
        public void Disconnect()
        {
            if (_tmapi == null)
                _tmapi = new TMAPI();

            _tmapi.DisconnectTarget();

            _tmapi = new TMAPI();
        }

        /// <summary>
        /// Attaches to target process.
        /// This should automatically continue the process if it is stopped.
        /// </summary>
        public bool Attach()
        {
            if (_tmapi == null)
                _tmapi = new TMAPI();

            return _tmapi.AttachProcess();
        }

        /// <summary>
        /// Pauses the attached process (return false if not available feature)
        /// </summary>
        public bool PauseProcess()
        {
            if (_tmapi == null)
                _tmapi = new TMAPI();

            return _tmapi.AttachProcOnly();
        }

        /// <summary>
        /// Continues the attached process (return false if not available feature)
        /// </summary>
        public bool ContinueProcess()
        {
            if (_tmapi == null)
                _tmapi = new TMAPI();

            _tmapi.ContinueProcess();
            return true;
        }

        /// <summary>
        /// Tells NetCheat if the process is currently stopped (return false if not available feature)
        /// </summary>
        public bool isProcessStopped()
        {
            if (_tmapi == null)
                _tmapi = new TMAPI();

            ulong[] ppu, spu;
            _tmapi.GetThreadList(0, _tmapi.SCE.ProcessID(), out ppu, out spu);

            PS3TMAPI.PPUThreadInfo ppuTI;
            foreach (ulong tID in ppu)
            {
                _tmapi.GetPPUThreadInfo(0, _tmapi.SCE.ProcessID(), tID, out ppuTI);
                if (ppuTI.State != PS3TMAPI.PPUThreadState.OnProc &&
                    ppuTI.State != PS3TMAPI.PPUThreadState.Sleep &&
                    ppuTI.State != PS3TMAPI.PPUThreadState.Runnable)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Called by user.
        /// Should display options for the API.
        /// Can be used for other things.
        /// </summary>
        public void Configure()
        {
            string path1 = @"C:\Program Files (x86)\SN Systems\PS3\bin";
            string path2 = @"C:\Program Files\SN Systems\PS3\bin";

            string file1 = path1 + "\\ps3tm.exe";
            string file2 = path2 + "\\ps3tm.exe";


            if (Directory.Exists(path1) && File.Exists(file1))
            {
                OpenFileEXE(file1);
            }
            else if (Directory.Exists(path2) && File.Exists(file2))
            {
                OpenFileEXE(file2);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("PS3TMAPI not installed! Failed to open Target Manager.");
            }

        }

        private void OpenFileEXE(string path)
        {
            Process.Start(path);
        }

        /// <summary>
        /// Called on initialization
        /// </summary>
		public void Initialize()
		{

		}

        /// <summary>
        /// Called when disposed
        /// </summary>
		public void Dispose()
		{
			//Put any cleanup code in here for when the program is stopped
		}

    }
}
