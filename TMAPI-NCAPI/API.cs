using System;
using System.Collections.Generic;
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
            private Thread stoppedThreadPoller;
            private bool isProcessingStop;
            private ulong lastStoppedThreadId;
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

                StopPollingWorker();

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
                    try
                    {
                        tmapi.CancelTargetEvents();
                    }
                    catch (Exception ex)
                    {
                        PublishError("Failed to cancel TMAPI target events after SetDABR failure: " + ex.Message);
                    }

                    registeredEvents = false;

                    throw new InvalidOperationException("TMAPI SetDABR failed: " + result.ToString());
                }

                lock (sync)
                {
                    isRunning = true;
                }

                StartPollingWorker();
                PublishDiagnostic("DABR set to 0x" + rawDabr.ToString("X16") + ". Waiting for " + Mode.ToString().ToLowerInvariant() + " hit.");
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
                PS3TMAPI.EventType callbackType,
                uint callbackParam,
                PS3TMAPI.SNRESULT result,
                uint rawLength,
                byte[] rawData,
                PS3TMAPI.TargetEvent[] targetEventList,
                object userData)
            {
                if (!IsRunning)
                    return;

                PublishDiagnostic("TMAPI callback: target=" + target.ToString() +
                    " event=" + callbackType.ToString() +
                    " param=0x" + callbackParam.ToString("X8") +
                    " result=" + result.ToString() +
                    " length=" + rawLength.ToString() +
                    " raw=" + BytesToHex(rawData, 128));

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
                    RecoverLogAndResumeFromRawCallback();
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

                    lastStoppedThreadId = hit.ThreadId;
                    hit.InstructionBytes = ReadInstructionBytes(hit.ProgramCounter);
                    PublishHit(hit);

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

            private void RecoverLogAndResumeFromRawCallback()
            {
                StoppedThreadInfo stoppedThread;
                string recoverError;
                if (TryRecoverStoppedThread(out stoppedThread, out recoverError))
                {
                    ProcessStoppedThreadCandidate(stoppedThread, "raw target callback fallback");
                    return;
                }

                PublishError("TMAPI callback was received, but DABR event data could not be parsed. " + recoverError);
                ResumeProcessFallback("No stopped PPU thread was recovered from raw callback.");
            }

            private bool TryRecoverStoppedThread(out StoppedThreadInfo stoppedThread, out string error)
            {
                stoppedThread = new StoppedThreadInfo();
                error = "";

                try
                {
                    ulong[] ppuThreadIDs;
                    ulong[] spuThreadIDs;
                    PS3TMAPI.SNRESULT listResult = tmapi.GetThreadList(TMAPI.Target, tmapi.SCE.ProcessID(), out ppuThreadIDs, out spuThreadIDs);
                    if (!PS3TMAPI.SUCCEEDED(listResult))
                    {
                        error = "GetThreadList failed: " + listResult.ToString();
                        return false;
                    }

                    foreach (ulong candidateThreadId in ppuThreadIDs)
                    {
                        PS3TMAPI.PPUThreadInfo threadInfo;
                        PS3TMAPI.SNRESULT infoResult = tmapi.GetPPUThreadInfo(TMAPI.Target, tmapi.SCE.ProcessID(), candidateThreadId, out threadInfo);
                        if (!PS3TMAPI.SUCCEEDED(infoResult))
                            continue;

                        if (threadInfo.State == PS3TMAPI.PPUThreadState.Stop)
                        {
                            stoppedThread.ThreadId = candidateThreadId;
                            return true;
                        }
                    }

                    error = "No stopped PPU thread found.";
                    return false;
                }
                catch (Exception ex)
                {
                    error = "Stopped-thread recovery failed: " + ex.Message;
                    return false;
                }
            }

            private void StartPollingWorker()
            {
                stoppedThreadPoller = new Thread(PollStoppedThreads);
                stoppedThreadPoller.IsBackground = true;
                stoppedThreadPoller.Name = "TMAPI DABR stopped-thread poller";
                stoppedThreadPoller.Start();
                PublishDiagnostic("Stopped-thread polling fallback active.");
            }

            private void StopPollingWorker()
            {
                Thread worker = stoppedThreadPoller;
                stoppedThreadPoller = null;
                if (worker == null || worker == Thread.CurrentThread)
                    return;

                if (!worker.Join(250))
                    PublishDiagnostic("Stopped-thread polling fallback did not exit before timeout.");
            }

            private void PollStoppedThreads()
            {
                while (IsRunning)
                {
                    try
                    {
                        StoppedThreadInfo stoppedThread;
                        string error;
                        if (TryRecoverStoppedThread(out stoppedThread, out error))
                        {
                            if (stoppedThread.ThreadId != lastStoppedThreadId)
                                ProcessStoppedThreadCandidate(stoppedThread, "polling fallback");
                        }
                        else
                        {
                            lastStoppedThreadId = 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        PublishError("Stopped-thread polling fallback failed: " + ex.Message);
                    }

                    Thread.Sleep(35);
                }
            }

            private void ProcessStoppedThreadCandidate(StoppedThreadInfo stoppedThread, string source)
            {
                lock (sync)
                {
                    if (!isRunning || isProcessingStop)
                        return;

                    isProcessingStop = true;
                    lastStoppedThreadId = stoppedThread.ThreadId;
                }

                try
                {
                    PublishDiagnostic("Stopped thread detected by " + source + ": 0x" + stoppedThread.ThreadId.ToString("X16") + ".");

                    AddressAccessHit hit = new AddressAccessHit();
                    hit.WatchedAddress = Address;
                    hit.Mode = Mode;
                    hit.RawDabr = rawDabr;
                    hit.Timestamp = DateTime.Now;
                    hit.ThreadId = stoppedThread.ThreadId;
                    hit.InstructionBytes = ReadInstructionBytes(hit.ProgramCounter);

                    if (hit.ProgramCounter == 0)
                        hit.Diagnostic = "Stopped thread recovered, but PC/SP are unavailable without a TMAPI exception event.";

                    PublishHit(hit);
                    if (hit.ProgramCounter != 0)
                        PublishDiagnostic("Hit logged at PC " + hit.ProgramCounter.ToString("X8") + ".");

                    ResumeAfterHit(stoppedThread.ThreadId);
                }
                finally
                {
                    lock (sync)
                    {
                        isProcessingStop = false;
                    }
                }
            }

            private void ResumeAfterHit(ulong threadId)
            {
                if (threadId != 0)
                {
                    PS3TMAPI.SNRESULT cleanResult = tmapi.ThreadExceptionClean(threadId);
                    if (!PS3TMAPI.SUCCEEDED(cleanResult))
                        PublishError("ThreadExceptionClean failed for 0x" + threadId.ToString("X16") + ": " + cleanResult.ToString());

                    PS3TMAPI.SNRESULT continueResult = tmapi.ThreadContinue(threadId);
                    if (PS3TMAPI.SUCCEEDED(continueResult))
                    {
                        PublishDiagnostic("Resumed PPU thread 0x" + threadId.ToString("X16") + ".");
                        return;
                    }

                    PublishError("ThreadContinue failed for 0x" + threadId.ToString("X16") + ": " + continueResult.ToString());
                }

                ResumeProcessFallback("Thread resume unavailable or failed.");
            }

            private void ResumeProcessFallback(string reason)
            {
                try
                {
                    PS3TMAPI.SNRESULT continueResult = tmapi.ProcessContinue();
                    if (PS3TMAPI.SUCCEEDED(continueResult))
                        PublishDiagnostic("ProcessContinue fallback used. " + reason);
                    else
                        PublishError("ProcessContinue fallback failed: " + continueResult.ToString() + ". " + reason);
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

            private static string BytesToHex(byte[] bytes, int maxBytes)
            {
                if (bytes == null || bytes.Length == 0 || maxBytes <= 0)
                    return "";

                int count = Math.Min(bytes.Length, maxBytes);
                char[] chars = new char[count * 2];
                const string hex = "0123456789ABCDEF";
                for (int index = 0; index < count; index++)
                {
                    chars[index * 2] = hex[bytes[index] >> 4];
                    chars[index * 2 + 1] = hex[bytes[index] & 0xF];
                }

                return new string(chars);
            }

            private struct StoppedThreadInfo
            {
                public ulong ThreadId;
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
