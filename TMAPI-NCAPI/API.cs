using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
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

            AddressAccessLoggerSession previousSession = null;
            lock (addressAccessLoggerLock)
            {
                if (activeAddressAccessLogger != null && activeAddressAccessLogger.IsRunning)
                    previousSession = activeAddressAccessLogger;
            }

            if (previousSession != null)
                previousSession.Stop();

            lock (addressAccessLoggerLock)
            {
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
            private readonly Action<string> nativeEventDiagnosticSink;
            private Thread eventPumpWorker;
            private bool isProcessingStop;
            private bool reportedDebugThreadControlInfo;
            private bool receivedNativeCallback;
            private bool isRunning;
            private bool registeredEvents;
            private bool savedOldDabr;
            private bool dabrWasArmed;
            private bool savedAutoStatusUpdate;
            private bool previousAutoStatusUpdate;
            private ulong oldDabr;
            private ulong rawDabr;
            private string lastNativeEventDiagnostic;
            private DateTime lastNativeEventDiagnosticTime;
            private bool reportedKickSuccess;
            private bool hasLastKickFailure;
            private PS3TMAPI.SNRESULT lastKickFailure;

            public AddressAccessLoggerSession(API owner, TMAPI tmapi, ulong address, AddressAccessMode mode, Action<AddressAccessHit> hitCallback)
            {
                this.owner = owner;
                this.tmapi = tmapi;
                this.hitCallback = hitCallback;
                Address = address;
                Mode = mode;
                targetEventCallback = HandleTargetEvents;
                nativeEventDiagnosticSink = PublishNativeEventDiagnostic;

                StartWorker();
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

                JoinWorker();
            }

            public void Dispose()
            {
                Stop();
            }

            private void StartWorker()
            {
                lock (sync)
                {
                    isRunning = true;
                }

                eventPumpWorker = new Thread(RunLoggerWorker);
                eventPumpWorker.IsBackground = true;
                eventPumpWorker.Name = "TMAPI DABR callback/event pump";
                eventPumpWorker.Start();
            }

            private void JoinWorker()
            {
                Thread worker = eventPumpWorker;
                if (worker == null || worker == Thread.CurrentThread)
                    return;

                if (!worker.Join(1000))
                    PublishDiagnostic("TMAPI DABR callback/event pump did not exit before timeout.");
            }

            private void RunLoggerWorker()
            {
                try
                {
                    if (!StartOnWorkerThread())
                        return;

                    PumpTargetEventsOnWorkerThread();
                }
                catch (Exception ex)
                {
                    PublishError("TMAPI DABR logger worker failed: " + ex.Message);
                }
                finally
                {
                    CleanupOnWorkerThread();
                    lock (sync)
                    {
                        isRunning = false;
                    }

                    owner.ClearAddressAccessLoggerSession(this);
                }
            }

            private bool StartOnWorkerThread()
            {
                PS3TMAPI.SNRESULT initResult = tmapi.EnsureTargetCommsInitialized();
                PublishDiagnostic("InitTargetComms: " + initResult.ToString());

                bool previousAutoStatus;
                PS3TMAPI.SNRESULT autoStatusResult = tmapi.EnableAutoStatusUpdate(true, out previousAutoStatus);
                PublishDiagnostic("EnableAutoStatusUpdate: " + autoStatusResult.ToString() +
                    ", previous=" + previousAutoStatus.ToString() + ".");
                savedAutoStatusUpdate = PS3TMAPI.SUCCEEDED(autoStatusResult);
                previousAutoStatusUpdate = previousAutoStatus;

                if (!IsRunning)
                    return false;

                PS3TMAPI.NativeEventDiagnosticSink = nativeEventDiagnosticSink;
                PublishDiagnostic("No TMAPI native callback has been received yet.");

                PS3TMAPI.SNRESULT result = tmapi.GetDABR(out oldDabr);
                savedOldDabr = PS3TMAPI.SUCCEEDED(result);

                if (!IsRunning)
                    return false;

                object userData = null;
                result = tmapi.RegisterTargetEventHandler(targetEventCallback, ref userData);
                PublishDiagnostic("RegisterTargetEventHandler: " + result.ToString());
                if (!PS3TMAPI.SUCCEEDED(result))
                {
                    PublishError("TMAPI RegisterTargetEventHandler failed: " + result.ToString());
                    return false;
                }

                registeredEvents = true;

                if (!IsRunning)
                    return false;

                rawDabr = BuildRawDabr(Address, Mode);
                result = tmapi.SetDABR(rawDabr);
                if (!PS3TMAPI.SUCCEEDED(result))
                {
                    PublishError("TMAPI SetDABR failed: " + result.ToString());
                    return false;
                }

                dabrWasArmed = true;
                PublishInitialThreadListProbe();
                PublishDiagnostic("TMAPI DABR logger uses SNPS3Kick on the callback registration thread. Polling auto-resume remains disabled for target safety.");
                PublishDiagnostic("DABR set to 0x" + rawDabr.ToString("X16") + ". Waiting for " + Mode.ToString().ToLowerInvariant() + " hit.");
                return true;
            }

            private void CleanupOnWorkerThread()
            {
                if (!receivedNativeCallback)
                    PublishDiagnostic("No TMAPI native callback was received during this logger session.");

                if (registeredEvents)
                {
                    try
                    {
                        PS3TMAPI.SNRESULT result = tmapi.CancelTargetEvents();
                        PublishDiagnostic("CancelTargetEvents: " + result.ToString() + ".");
                    }
                    catch (Exception ex)
                    {
                        PublishError("Failed to cancel TMAPI target events: " + ex.Message);
                    }
                    finally
                    {
                        registeredEvents = false;
                    }
                }

                if (dabrWasArmed)
                {
                    try
                    {
                        PS3TMAPI.SNRESULT result = savedOldDabr
                            ? tmapi.SetDABR(oldDabr)
                            : tmapi.SetDABR(0);
                        PublishDiagnostic("Restore DABR: " + result.ToString() + ".");
                    }
                    catch (Exception ex)
                    {
                        PublishError("Failed to restore DABR: " + ex.Message);
                    }
                    finally
                    {
                        dabrWasArmed = false;
                    }
                }

                RestoreAutoStatusUpdateOnWorkerThread();

                if (Object.ReferenceEquals(PS3TMAPI.NativeEventDiagnosticSink, nativeEventDiagnosticSink))
                    PS3TMAPI.NativeEventDiagnosticSink = null;
            }

            private void RestoreAutoStatusUpdateOnWorkerThread()
            {
                if (!savedAutoStatusUpdate)
                    return;

                savedAutoStatusUpdate = false;
                try
                {
                    bool ignoredPreviousState;
                    PS3TMAPI.SNRESULT result = tmapi.EnableAutoStatusUpdate(previousAutoStatusUpdate, out ignoredPreviousState);
                    PublishDiagnostic("Restore EnableAutoStatusUpdate: " + result.ToString() +
                        ", restored=" + previousAutoStatusUpdate.ToString() + ".");
                }
                catch (Exception ex)
                {
                    PublishError("Failed to restore TMAPI auto status update: " + ex.Message);
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

            private void HandleTargetEvents(
                int target,
                PS3TMAPI.SNRESULT result,
                PS3TMAPI.TargetEvent[] targetEventList,
                object userData)
            {
                if (!IsRunning)
                    return;

                int eventCount = targetEventList == null ? 0 : targetEventList.Length;
                PublishDiagnostic("Target event received: target=" + target.ToString() +
                    " result=" + result.ToString() +
                    " events=" + eventCount.ToString("N0") + ".");

                bool handledDabrMatch = false;
                if (targetEventList != null)
                {
                    foreach (PS3TMAPI.TargetEvent targetEvent in targetEventList)
                    {
                        if (targetEvent.Type != PS3TMAPI.TargetEventType.TargetSpecific)
                            continue;

                        PS3TMAPI.TargetSpecificEvent specific = targetEvent.TargetSpecific;
                        if (specific.Data.Type != PS3TMAPI.TargetSpecificEventType.PPUExcDabrMatch)
                            continue;

                        handledDabrMatch = true;
                        HandleDabrMatch(specific);
                    }
                }

                if (!handledDabrMatch)
                    PublishError("TMAPI callback was received, but DABR event data did not include a DABR hit. No polling resume was attempted.");
            }

            private void HandleDabrMatch(PS3TMAPI.TargetSpecificEvent specific)
            {
                lock (sync)
                {
                    if (!isRunning || isProcessingStop)
                        return;

                    isProcessingStop = true;
                }

                try
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
                    PublishDiagnostic("DABR hit: thread=0x" + hit.ThreadId.ToString("X16") +
                        " PC=0x" + hit.ProgramCounter.ToString("X8") + ".");

                    ResumeAfterHit(hit.ThreadId);
                }
                finally
                {
                    lock (sync)
                    {
                        isProcessingStop = false;
                    }
                }
            }

            private void PumpTargetEventsOnWorkerThread()
            {
                while (IsRunning)
                {
                    try
                    {
                        PumpOneTargetEventSlice();
                        PublishDebugThreadControlInfoOnce();
                    }
                    catch (Exception ex)
                    {
                        PublishError("TMAPI event pump failed: " + ex.Message);
                    }

                    Thread.Sleep(15);
                }
            }

            private void PumpOneTargetEventSlice()
            {
                PS3TMAPI.SNRESULT kickResult = tmapi.Kick();
                if (PS3TMAPI.SUCCEEDED(kickResult))
                {
                    if (!reportedKickSuccess)
                    {
                        reportedKickSuccess = true;
                        PublishDiagnostic("SNPS3Kick event pump active on callback registration thread.");
                    }

                    hasLastKickFailure = false;
                    return;
                }

                if (!hasLastKickFailure || lastKickFailure != kickResult)
                {
                    hasLastKickFailure = true;
                    lastKickFailure = kickResult;
                    PublishError("SNPS3Kick failed: " + kickResult.ToString());
                }
            }

            private void PublishInitialThreadListProbe()
            {
                try
                {
                    ulong[] ppuThreadIDs;
                    ulong[] spuThreadIDs;
                    PS3TMAPI.SNRESULT listResult = tmapi.GetThreadList(TMAPI.Target, tmapi.SCE.ProcessID(), out ppuThreadIDs, out spuThreadIDs);
                    int ppuCount = ppuThreadIDs == null ? 0 : ppuThreadIDs.Length;
                    int spuCount = spuThreadIDs == null ? 0 : spuThreadIDs.Length;
                    PublishDiagnostic("GetThreadList initial probe: " + listResult.ToString() +
                        ", PPU=" + ppuCount.ToString("N0") +
                        ", SPU=" + spuCount.ToString("N0") + ".");

                    if (listResult == PS3TMAPI.SNRESULT.SN_E_DLL_NOT_INITIALISED)
                        PublishDiagnostic("InitTargetComms last result: " + tmapi.LastTargetCommsInitResult.ToString() + ".");
                }
                catch (Exception ex)
                {
                    PublishError("GetThreadList initial probe failed: " + ex.Message);
                }
            }

            private void PublishDebugThreadControlInfoOnce()
            {
                if (reportedDebugThreadControlInfo)
                    return;

                reportedDebugThreadControlInfo = true;
                PublishDiagnostic(tmapi.GetDebugThreadControlInfoDiagnostic());
            }

            private void PublishNativeEventDiagnostic(string diagnostic)
            {
                lock (sync)
                {
                    if (!isRunning)
                        return;

                    DateTime now = DateTime.UtcNow;
                    if (diagnostic == lastNativeEventDiagnostic &&
                        (now - lastNativeEventDiagnosticTime).TotalMilliseconds < 1000)
                    {
                        return;
                    }

                    lastNativeEventDiagnostic = diagnostic;
                    lastNativeEventDiagnosticTime = now;
                    receivedNativeCallback = true;
                }

                PublishDiagnostic(diagnostic);
            }

            private void ResumeAfterHit(ulong threadId)
            {
                if (threadId != 0)
                {
                    PS3TMAPI.SNRESULT cleanResult = tmapi.ThreadExceptionClean(threadId);
                    if (!PS3TMAPI.SUCCEEDED(cleanResult))
                        PublishError("ThreadExceptionClean failed for 0x" + threadId.ToString("X16") + ": " + cleanResult.ToString());

                    PS3TMAPI.SNRESULT continueResult = tmapi.ThreadContinue(threadId);
                    PublishDiagnostic("ThreadContinue returned " + continueResult.ToString() + " for 0x" + threadId.ToString("X16") + ".");

                    if (PS3TMAPI.SUCCEEDED(continueResult))
                        return;
                }

                ResumeProcessFallback("Thread resume unavailable or failed.");
            }

            private void ResumeProcessFallback(string reason)
            {
                try
                {
                    PS3TMAPI.SNRESULT continueResult = tmapi.ProcessContinue();
                    if (PS3TMAPI.SUCCEEDED(continueResult))
                        PublishDiagnostic(reason + ": " + continueResult.ToString() + ". Process resumed.");
                    else
                        PublishError(reason + ": " + continueResult.ToString() + ".");
                }
                catch (Exception ex)
                {
                    PublishError("ProcessContinue fallback threw: " + ex.Message + ". " + reason);
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

            private void PublishDiagnostic(string diagnostic)
            {
                AddressAccessHit hit = new AddressAccessHit();
                hit.WatchedAddress = Address;
                hit.Mode = Mode;
                hit.RawDabr = rawDabr;
                hit.Timestamp = DateTime.Now;
                hit.Diagnostic = diagnostic;
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
