// ************************************************* //
//    --- Copyright (c) 2014 iMCS Productions ---    //
// ************************************************* //
//              PS3Lib v4 By FM|T iMCSx              //
//                                                   //
// Features v4.4 :                                   //
// - Set Boot Console ID                             //
// - Popup better form with icon                     //
//                                                   //
// Credits : FM|T Enstone , Buc-ShoTz                //
//                                                   //
// Follow me :                                       //
//                                                   //
// FrenchModdingTeam.com                             //
// Youtube.com/iMCSx                                 //
// Twitter.com/iMCSx                                 //
// Facebook.com/iMCSx                                //
//                                                   //
// ************************************************* //

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace TMAPI_NCAPI
{
    public class PS3TMAPI
    {
        public enum SNRESULT
        {
            SN_E_BAD_ALIGN = -28,
            SN_E_BAD_MEMSPACE = -18,
            SN_E_BAD_PARAM = -21,
            SN_E_BAD_TARGET = -3,
            SN_E_BAD_UNIT = -11,
            SN_E_BUSY = -22,
            SN_E_CHECK_TARGET_CONFIGURATION = -33,
            SN_E_COMMAND_CANCELLED = -36,
            SN_E_COMMS_ERR = -5,
            SN_E_COMMS_EVENT_MISMATCHED_ERR = -39,
            SN_E_CONNECT_TO_GAMEPORT_FAILED = -35,
            SN_E_CONNECTED = -38,
            SN_E_DATA_TOO_LONG = -26,
            SN_E_DECI_ERROR = -23,
            SN_E_DEPRECATED = -27,
            SN_E_DLL_NOT_INITIALISED = -15,
            SN_E_ERROR = -2147483648,
            SN_E_EXISTING_CALLBACK = -24,
            SN_E_FILE_ERROR = -29,
            SN_E_HOST_NOT_FOUND = -8,
            SN_E_INSUFFICIENT_DATA = -25,
            SN_E_LICENSE_ERROR = -32,
            SN_E_LOAD_ELF_FAILED = -10,
            SN_E_LOAD_MODULE_FAILED = -31,
            SN_E_MODULE_NOT_FOUND = -34,
            SN_E_NO_SEL = -20,
            SN_E_NO_TARGETS = -19,
            SN_E_NOT_CONNECTED = -4,
            SN_E_NOT_IMPL = -1,
            SN_E_NOT_LISTED = -13,
            SN_E_NOT_SUPPORTED_IN_SDK_VERSION = -30,
            SN_E_OUT_OF_MEM = -12,
            SN_E_PROTOCOL_ALREADY_REGISTERED = -37,
            SN_E_TARGET_IN_USE = -9,
            SN_E_TARGET_RUNNING = -17,
            SN_E_TIMEOUT = -7,
            SN_E_TM_COMMS_ERR = -6,
            SN_E_TM_NOT_RUNNING = -2,
            SN_E_TM_VERSION = -14,
            SN_S_NO_ACTION = 6,
            SN_S_NO_MSG = 3,
            SN_S_OK = 0,
            SN_S_PENDING = 1,
            SN_S_REPLACED = 5,
            SN_S_TARGET_STILL_REGISTERED = 7,
            SN_S_TM_VERSION = 4
        }

        public enum UnitType
        {
            PPU,
            SPU,
            SPURAW
        }

        public enum EventType
        {
            TTY = 100,
            Target = 101,
            System = 102,
            FTP = 103,
            PadCapture = 104,
            FileTrace = 105,
            PadPlayback = 106,
            Server = 107
        }

        public enum UnitStatus
        {
            Unknown = 0,
            Running = 1,
            Stopped = 2,
            Signalled = 3,
            Resetting = 4,
            Missing = 5,
            Reset = 6,
            NotConnected = 7,
            Connected = 8,
            StatusChange = 9
        }

        public enum TargetEventType : uint
        {
            UnitStatusChange = 0,
            ResetStarted = 1,
            ResetEnd = 2,
            Details = 4,
            ModuleLoad = 5,
            ModuleRunning = 6,
            ModuleDoneRemove = 7,
            ModuleDoneResident = 8,
            ModuleStopped = 9,
            ModuleStoppedRemove = 10,
            PowerStatusChange = 11,
            TTYStreamAdded = 12,
            TTYStreamDeleted = 13,
            BDIsotransferStarted = 16,
            BDIsotransferFinished = 17,
            BDFormatStarted = 18,
            BDFormatFinished = 19,
            BDMountStarted = 20,
            BDMountFinished = 21,
            BDUnmountStarted = 22,
            BDUnmountFinished = 23,
            TargetSpecific = 0x80000000
        }

        public enum TargetSpecificEventType : uint
        {
            ProcessCreate = 0,
            ProcessExit = 1,
            ProcessKill = 2,
            ProcessExitSpawn = 3,
            PPUExcTrap = 16,
            PPUExcPrevInt = 17,
            PPUExcAlignment = 18,
            PPUExcIllInst = 19,
            PPUExcTextHtabMiss = 20,
            PPUExcTextSlbMiss = 21,
            PPUExcDataHtabMiss = 22,
            PPUExcFloat = 23,
            PPUExcDataSlbMiss = 24,
            PPUExcDabrMatch = 25,
            PPUExcStop = 26,
            PPUExcStopInit = 27,
            PPUExcDataMAT = 28,
            PPUThreadCreate = 32,
            PPUThreadExit = 33,
            SPUThreadStart = 48,
            SPUThreadStop = 49,
            SPUThreadStopInit = 50,
            SPUThreadGroupDestroy = 51,
            SPUThreadStopEx = 52,
            PRXLoad = 64,
            PRXUnload = 65,
            DAInitialised = 96,
            Footswitch = 112,
            InstallPackageProgress = 128,
            InstallPackagePath = 129,
            CoreDumpComplete = 256,
            CoreDumpStart = 257,
            RawNotify = 0xF000000F
        }

        public delegate void TargetEventCallback(int target, SNRESULT res, TargetEvent[] targetEventList, object userData);

        public static Action<string> NativeEventDiagnosticSink;

        private delegate void HandleEventCallbackPriv(int target, EventType type, uint param, SNRESULT result, uint length, IntPtr data, IntPtr userData);

        public struct TargetEventData
        {
            public TGTEventUnitStatusChangeData UnitStatusChangeData;
        }

        public struct TGTEventUnitStatusChangeData
        {
            public UnitType Unit;
            public UnitStatus Status;
        }

        public struct TargetEvent
        {
            public uint TargetID;
            public uint Size;
            public TargetEventType Type;
            public TargetEventData EventData;
            public TargetSpecificEvent TargetSpecific;
        }

        public struct TargetSpecificEvent
        {
            public uint CommandID;
            public uint RequestID;
            public uint DataLength;
            public uint ProcessID;
            public uint Result;
            public uint TargetEventSize;
            public uint TargetEventTypeRaw;
            public uint PayloadOffset;
            public string RawDebugDataHex;
            public TargetSpecificData Data;
        }

        public struct TargetSpecificData
        {
            public TargetSpecificEventType Type;
            public PPUExceptionData PPUException;
            public PPUAlignmentExceptionData PPUAlignmentException;
            public PPUDataMatExceptionData PPUDataMatException;
        }

        public struct PPUExceptionData
        {
            public ulong ThreadID;
            public uint HWThreadNumber;
            public ulong PC;
            public ulong SP;
        }

        public struct PPUDataMatExceptionData
        {
            public ulong ThreadID;
            public uint HWThreadNumber;
            public ulong DSISR;
            public ulong DAR;
            public ulong PC;
            public ulong SP;
        }

        public struct PPUAlignmentExceptionData
        {
            public ulong ThreadID;
            public uint HWThreadNumber;
            public ulong DSISR;
            public ulong DAR;
            public ulong PC;
            public ulong SP;
        }

        [Flags]
        public enum ResetParameter : ulong
        {
            Hard = 1L,
            Quick = 2L,
            ResetEx = 9223372036854775808L,
            Soft = 0L
        }

        private class ScopedGlobalHeapPtr
        {
            private IntPtr m_intPtr = IntPtr.Zero;

            public ScopedGlobalHeapPtr(IntPtr intPtr)
            {
                this.m_intPtr = intPtr;
            }

            ~ScopedGlobalHeapPtr()
            {
                if (this.m_intPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(this.m_intPtr);
                }
            }

            public IntPtr Get()
            {
                return this.m_intPtr;
            }
        }

        public enum ConnectStatus
        {
            Connected,
            Connecting,
            NotConnected,
            InUse,
            Unavailable
        }

        [StructLayout(LayoutKind.Sequential)]
        public class TCPIPConnectProperties
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0xff)]
            public string IPAddress;
            public uint Port;
        }

        [Flags]
        public enum TargetInfoFlag : uint
        {
            Boot = 0x20,
            FileServingDir = 0x10,
            HomeDir = 8,
            Info = 4,
            Name = 2,
            TargetID = 1
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TargetInfoPriv
        {
            public PS3TMAPI.TargetInfoFlag Flags;
            public int Target;
            public IntPtr Name;
            public IntPtr Type;
            public IntPtr Info;
            public IntPtr HomeDir;
            public IntPtr FSDir;
            public PS3TMAPI.BootParameter Boot;
        }

        [Flags]
        public enum BootParameter : ulong
        {
            BluRayEmuOff = 4L,
            BluRayEmuUSB = 0x20L,
            DebugMode = 0x10L,
            Default = 0L,
            DualNIC = 0x80L,
            HDDSpeedBluRayEmu = 8L,
            HostFSTarget = 0x40L,
            MemSizeConsole = 2L,
            ReleaseMode = 1L,
            SystemMode = 0x11L
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TargetInfo
        {
            public PS3TMAPI.TargetInfoFlag Flags;
            public int Target;
            public string Name;
            public string Type;
            public string Info;
            public string HomeDir;
            public string FSDir;
            public PS3TMAPI.BootParameter Boot;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PPUThreadInfo
        {
            public ulong ThreadID;
            public uint Priority;
            public PPUThreadState State;
            public ulong StackAddress;
            public ulong StackSize;
            public string ThreadName;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PPUThreadInfoPriv
        {
            public ulong ThreadID;
            public uint Priority;
            public uint State;
            public ulong StackAddress;
            public ulong StackSize;
            public uint ThreadNameLen;
        }

        [Flags]
        public enum PPUThreadState : uint
        {
            Idle = 0x00000000,
            Runnable = 0x00000001,
            OnProc = 0x00000002,
            Sleep = 0x00000003,
            Suspended = 0x00000004,
            SleepSuspended = 0x00000005,
            Stop = 0x00000006,
            Zombie = 0x00000007,
            Deleted = 0x00000008
        }

        public enum ProcessStatus : uint
        {
            Creating = 1,
            Ready = 2,
            Exited = 3
        }

        public enum SPUThreadGroupState : uint
        {
            NotConfigured = 0,
            Configured = 1,
            Ready = 2,
            Waiting = 3,
            Suspended = 4,
            WaitingSuspended = 5,
            Running = 6,
            Stopped = 7
        }

        public struct PPUThreadStatus
        {
            public ulong ThreadID;
            public PPUThreadState ThreadState;
        }

        public struct SPUThreadGroupStatus
        {
            public uint ThreadGroupID;
            public SPUThreadGroupState ThreadGroupState;
        }

        public struct ProcessTreeBranch
        {
            public uint ProcessID;
            public ProcessStatus ProcessState;
            public ushort ProcessFlags;
            public ushort RawSPU;
            public PPUThreadStatus[] PPUThreadStatuses;
            public SPUThreadGroupStatus[] SPUThreadGroupStatuses;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct ProcessTreeBranchPriv
        {
            public uint ProcessId;
            public ProcessStatus ProcessState;
            public uint NumPpuThreads;
            public uint NumSpuThreadGroups;
            public ushort ProcessFlags;
            public ushort RawSPU;
            public IntPtr PpuThreadStatuses;
            public IntPtr SpuThreadGroupStatuses;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ProcessInfo
        {
            public ulong ThreadID;
            public uint Priority;
            public PPUThreadState State;
            public ulong StackAddress;
            public ulong StackSize;
            public string ThreadName;
        }

        [DllImport("PS3TMAPIX64.dll", EntryPoint = "SNPS3InitTargetComms", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT InitTargetCommsX64();
        [DllImport("PS3TMAPI.dll", EntryPoint = "SNPS3InitTargetComms", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT InitTargetCommsX86();
        [DllImport("PS3TMAPIX64.dll", EntryPoint = "SNPS3Kick", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT KickX64();
        [DllImport("PS3TMAPI.dll", EntryPoint = "SNPS3Kick", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT KickX86();
        [DllImport("PS3TMAPIX64.dll", EntryPoint = "SNPS3PowerOn", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT PowerOnX64(int target);
        [DllImport("PS3TMAPI.dll", EntryPoint = "SNPS3PowerOn", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT PowerOnX86(int target);
        [DllImport("PS3TMAPIX64.dll", EntryPoint = "SNPS3PowerOff", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT PowerOffX64(int target, uint force);
        [DllImport("PS3TMAPI.dll", EntryPoint = "SNPS3PowerOff", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT PowerOffX86(int target, uint force);
        [DllImport("PS3TMAPIX64.dll", EntryPoint = "SNPS3Connect", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT ConnectX64(int target, string application);
        [DllImport("PS3TMAPI.dll", EntryPoint = "SNPS3Connect", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT ConnectX86(int target, string application);
        [DllImport("PS3TMAPIX64.dll", EntryPoint = "SNPS3GetConnectionInfo", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT GetConnectionInfoX64(int target, IntPtr connectProperties);
        [DllImport("PS3TMAPI.dll", EntryPoint = "SNPS3GetConnectionInfo", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT GetConnectionInfoX86(int target, IntPtr connectProperties);
        [DllImport("PS3TMAPIX64.dll", EntryPoint = "SNPS3GetConnectStatus", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT GetConnectStatusX64(int target, out uint status, out IntPtr usage);
        [DllImport("PS3TMAPI.dll", EntryPoint = "SNPS3GetConnectStatus", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT GetConnectStatusX86(int target, out uint status, out IntPtr usage);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int MultiByteToWideChar(int codepage, int flags, IntPtr utf8, int utf8len, StringBuilder buffer, int buflen);
        [DllImport("PS3TMAPIX64.dll", EntryPoint = "SNPS3ProcessList", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT GetProcessListX64(int target, ref uint count, IntPtr processIdArray);
        [DllImport("PS3TMAPI.dll", EntryPoint = "SNPS3ProcessList", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT GetProcessListX86(int target, ref uint count, IntPtr processIdArray);
        [DllImport("PS3TMAPIX64.dll", EntryPoint = "SNPS3ProcessContinue", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT ProcessContinueX64(int target, uint processId);
        [DllImport("PS3TMAPI.dll", EntryPoint = "SNPS3ProcessContinue", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT ProcessContinueX86(int target, uint processId);
        [DllImport("PS3TMAPIX64.dll", EntryPoint = "SNPS3ProcessAttach", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT ProcessAttachX64(int target, uint unitId, uint processId);
        [DllImport("PS3TMAPI.dll", EntryPoint = "SNPS3ProcessAttach", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT ProcessAttachX86(int target, uint unitId, uint processId);
        [DllImport("PS3TMAPIX64.dll", EntryPoint = "SNPS3ProcessGetMemory", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT ProcessGetMemoryX64(int target, UnitType unit, uint processId, ulong threadId, ulong address, int count, byte[] buffer);
        [DllImport("PS3TMAPI.dll", EntryPoint = "SNPS3ProcessGetMemory", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT ProcessGetMemoryX86(int target, UnitType unit, uint processId, ulong threadId, ulong address, int count, byte[] buffer);
        [DllImport("PS3TMAPIX64.dll", EntryPoint = "SNPS3GetTargetFromName", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT GetTargetFromNameX64(IntPtr name, out int target);
        [DllImport("PS3TMAPI.dll", EntryPoint = "SNPS3GetTargetFromName", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT GetTargetFromNameX86(IntPtr name, out int target);
        [DllImport("PS3TMAPIX64.dll", EntryPoint = "SNPS3Reset", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT ResetX64(int target, ulong resetParameter);
        [DllImport("PS3TMAPI.dll", EntryPoint = "SNPS3Reset", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT ResetX86(int target, ulong resetParameter);
        [DllImport("PS3TMAPIX64.dll", EntryPoint = "SNPS3ProcessSetMemory", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT ProcessSetMemoryX64(int target, UnitType unit, uint processId, ulong threadId, ulong address, int count, byte[] buffer);
        [DllImport("PS3TMAPI.dll", EntryPoint = "SNPS3ProcessSetMemory", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT ProcessSetMemoryX86(int target, UnitType unit, uint processId, ulong threadId, ulong address, int count, byte[] buffer);
        [DllImport("PS3TMAPIX64.dll", EntryPoint = "SNPS3GetTargetInfo", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT GetTargetInfoX64(ref TargetInfoPriv targetInfoPriv);
        [DllImport("PS3TMAPI.dll", EntryPoint = "SNPS3GetTargetInfo", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT GetTargetInfoX86(ref TargetInfoPriv targetInfoPriv);
        [DllImport("PS3TMAPIX64.dll", EntryPoint = "SNPS3Disconnect", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT DisconnectX64(int target);
        [DllImport("PS3TMAPI.dll", EntryPoint = "SNPS3Disconnect", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT DisconnectX86(int target);
        [DllImport("PS3TMAPIX64.dll", EntryPoint = "SNPS3EnableAutoStatusUpdate", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT EnableAutoStatusUpdateX64(int target, uint enabled, out uint previousState);
        [DllImport("PS3TMAPI.dll", EntryPoint = "SNPS3EnableAutoStatusUpdate", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT EnableAutoStatusUpdateX86(int target, uint enabled, out uint previousState);
        [DllImport("PS3TMAPIX64.dll", EntryPoint = "SNPS3ThreadList", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT ThreadListX64(int target, uint processID, ref uint numPPUThreads, ulong[] ppuThreadIDs, ref uint numSPUThreadGroups, ulong[] spuThreadIDs);
        [DllImport("PS3TMAPI.dll", EntryPoint = "SNPS3ThreadList", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT ThreadListX86(int target, uint processID, ref uint numPPUThreads, ulong[] ppuThreadIDs, ref uint numSPUThreadGroups, ulong[] spuThreadIDs);
        [DllImport("PS3TMAPIX64.dll", EntryPoint = "SNPS3ThreadContinue", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT ThreadContinueX64(int target, UnitType unit, uint processId, ulong threadId);
        [DllImport("PS3TMAPI.dll", EntryPoint = "SNPS3ThreadContinue", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT ThreadContinueX86(int target, UnitType unit, uint processId, ulong threadId);
        [DllImport("PS3TMAPIX64.dll", EntryPoint = "SNPS3ThreadExceptionClean", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT ThreadExceptionCleanX64(int target, uint processId, ulong threadId);
        [DllImport("PS3TMAPI.dll", EntryPoint = "SNPS3ThreadExceptionClean", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT ThreadExceptionCleanX86(int target, uint processId, ulong threadId);
        [DllImport("PS3TMAPIX64.dll", EntryPoint = "SNPS3ThreadGetRegisters", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT ThreadGetRegistersX64(int target, UnitType unit, uint processId, ulong threadId, uint numRegisters, uint[] registerNums, ulong[] registerValues);
        [DllImport("PS3TMAPI.dll", EntryPoint = "SNPS3ThreadGetRegisters", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT ThreadGetRegistersX86(int target, UnitType unit, uint processId, ulong threadId, uint numRegisters, uint[] registerNums, ulong[] registerValues);
        [DllImport("PS3TMAPIX64.dll", EntryPoint = "SNPS3ThreadInfo", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT GetThreadInfoX64(int target, UnitType unit, uint processID, ulong threadID, ref uint bufferSize, IntPtr buffer);
        [DllImport("PS3TMAPI.dll", EntryPoint = "SNPS3ThreadInfo", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT GetThreadInfoX86(int target, UnitType unit, uint processID, ulong threadID, ref uint bufferSize, IntPtr buffer);
        [DllImport("PS3TMAPIX64.dll", EntryPoint = "SNPS3GetProcessInfo", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT GetProcessInfoX64(int target, uint processID, out ProcessInfo pInfo);
        [DllImport("PS3TMAPI.dll", EntryPoint = "SNPS3GetProcessInfo", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT GetProcessInfoX86(int target, uint processID, out ProcessInfo pInfo);
        [DllImport("PS3TMAPIX64.dll", EntryPoint = "SNPS3SetDABR", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT SetDABRX64(int target, uint processId, ulong address);
        [DllImport("PS3TMAPI.dll", EntryPoint = "SNPS3SetDABR", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT SetDABRX86(int target, uint processId, ulong address);
        [DllImport("PS3TMAPIX64.dll", EntryPoint = "SNPS3GetDABR", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT GetDABRX64(int target, uint processId, out ulong address);
        [DllImport("PS3TMAPI.dll", EntryPoint = "SNPS3GetDABR", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT GetDABRX86(int target, uint processId, out ulong address);
        [DllImport("PS3TMAPIX64.dll", EntryPoint = "SNPS3RegisterTargetEventHandler", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT RegisterTargetEventHandlerX64(int target, HandleEventCallbackPriv callback, IntPtr userData);
        [DllImport("PS3TMAPI.dll", EntryPoint = "SNPS3RegisterTargetEventHandler", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT RegisterTargetEventHandlerX86(int target, HandleEventCallbackPriv callback, IntPtr userData);
        [DllImport("PS3TMAPIX64.dll", EntryPoint = "SNPS3CancelTargetEvents", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT CancelTargetEventsX64(int target);
        [DllImport("PS3TMAPI.dll", EntryPoint = "SNPS3CancelTargetEvents", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT CancelTargetEventsX86(int target);
        [DllImport("PS3TMAPIX64.dll", EntryPoint = "SNPS3GetProcessTree", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT GetProcessTreeX64(int target, ref uint numProcesses, IntPtr buffer);
        [DllImport("PS3TMAPI.dll", EntryPoint = "SNPS3GetProcessTree", CallingConvention = CallingConvention.Cdecl)]
        private static extern SNRESULT GetProcessTreeX86(int target, ref uint numProcesses, IntPtr buffer);

        private static readonly HandleEventCallbackPriv ms_eventHandlerWrapper = EventHandlerWrapper;
        [ThreadStatic]
        private static Dictionary<int, TargetCallbackAndUserData> ms_userTargetCallbacks;

        private class TargetCallbackAndUserData
        {
            public TargetEventCallback m_callback;
            public object m_userData;
        }

        private static Dictionary<int, TargetCallbackAndUserData> UserTargetCallbacks
        {
            get
            {
                if (ms_userTargetCallbacks == null)
                    ms_userTargetCallbacks = new Dictionary<int, TargetCallbackAndUserData>();

                return ms_userTargetCallbacks;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TargetEventHdrPriv
        {
            public uint Size;
            public uint TargetID;
            public uint EventType;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DebugEventHdrPriv
        {
            public uint CommandID;
            public uint RequestID;
            public uint DataLength;
            public uint ProcessID;
            public uint Result;
        }

        private static bool Is32Bit()
        {
            return (IntPtr.Size == 4);
        }

        public static bool FAILED(SNRESULT res)
        {
            return !SUCCEEDED(res);
        }

        public static bool SUCCEEDED(SNRESULT res)
        {
            return (res >= SNRESULT.SN_S_OK);
        }

        private static IntPtr AllocUtf8FromString(string wcharString)
        {
            if (wcharString == null)
            {
                return IntPtr.Zero;
            }
            byte[] bytes = Encoding.UTF8.GetBytes(wcharString);
            IntPtr destination = Marshal.AllocHGlobal((int)(bytes.Length + 1));
            Marshal.Copy(bytes, 0, destination, bytes.Length);
            Marshal.WriteByte((IntPtr)(destination.ToInt64() + bytes.Length), 0);
            return destination;
        }

        public static string Utf8ToString(IntPtr utf8, uint maxLength)
        {
            int len = MultiByteToWideChar(65001, 0, utf8, -1, null, 0);
            if (len == 0) throw new System.ComponentModel.Win32Exception();
            var buf = new StringBuilder(len);
            len = MultiByteToWideChar(65001, 0, utf8, -1, buf, len);
            return buf.ToString();
        }

        private static IntPtr ReadDataFromUnmanagedIncPtr<T>(IntPtr unmanagedBuf, ref T storage)
        {
            storage = (T)Marshal.PtrToStructure(unmanagedBuf, typeof(T));
            return new IntPtr(unmanagedBuf.ToInt64() + Marshal.SizeOf((T)storage));
        }

        public static SNRESULT InitTargetComms()
        {
            if (!Is32Bit())
            {
                return InitTargetCommsX64();
            }
            return InitTargetCommsX86();
        }

        public static SNRESULT Kick()
        {
            if (!Is32Bit())
            {
                return KickX64();
            }
            return KickX86();
        }

        public static SNRESULT Connect(int target, string application)
        {
            if (!Is32Bit())
            {
                return ConnectX64(target, application);
            }
            return ConnectX86(target, application);
        }

        public static SNRESULT PowerOn(int target)
        {
            if (!Is32Bit())
            {
                return PowerOnX64(target);
            }
            return PowerOnX86(target);
        }

        public static SNRESULT PowerOff(int target, bool bForce)
        {
            uint force = bForce ? (uint)1 : 0;
            if (!Is32Bit())
            {
                return PowerOffX64(target, force);
            }
            return PowerOffX86(target, force);
        }

        public static SNRESULT GetProcessList(int target, out uint[] processIDs)
        {
            processIDs = null;
            uint count = 0;
            SNRESULT res = Is32Bit() ? GetProcessListX86(target, ref count, IntPtr.Zero) : GetProcessListX64(target, ref count, IntPtr.Zero);
            if (!FAILED(res))
            {
                ScopedGlobalHeapPtr ptr = new ScopedGlobalHeapPtr(Marshal.AllocHGlobal((int)(4 * count)));
                res = Is32Bit() ? GetProcessListX86(target, ref count, ptr.Get()) : GetProcessListX64(target, ref count, ptr.Get());
                if (FAILED(res))
                {
                    return res;
                }
                IntPtr unmanagedBuf = ptr.Get();
                processIDs = new uint[count];
                for (uint i = 0; i < count; i++)
                {
                    unmanagedBuf = ReadDataFromUnmanagedIncPtr<uint>(unmanagedBuf, ref processIDs[i]);
                }
            }
            return res;
        }

        public static SNRESULT ProcessAttach(int target, UnitType unit, uint processID)
        {
            if (!Is32Bit())
            {
                return ProcessAttachX64(target, (uint)unit, processID);
            }
            return ProcessAttachX86(target, (uint)unit, processID);
        }

        public static SNRESULT ProcessContinue(int target, uint processID)
        {
            if (!Is32Bit())
            {
                return ProcessContinueX64(target, processID);
            }
            return ProcessContinueX86(target, processID);
        }

        public static SNRESULT ThreadContinue(int target, UnitType unit, uint processID, ulong threadID)
        {
            if (!Is32Bit())
            {
                return ThreadContinueX64(target, unit, processID, threadID);
            }
            return ThreadContinueX86(target, unit, processID, threadID);
        }

        public static SNRESULT ThreadExceptionClean(int target, uint processID, ulong threadID)
        {
            if (!Is32Bit())
            {
                return ThreadExceptionCleanX64(target, processID, threadID);
            }
            return ThreadExceptionCleanX86(target, processID, threadID);
        }

        public static SNRESULT ThreadGetRegisters(int target, UnitType unit, uint processID, ulong threadID, uint[] registerNums, out ulong[] registerValues)
        {
            if (registerNums == null)
                registerNums = new uint[0];

            registerValues = new ulong[registerNums.Length];
            if (!Is32Bit())
            {
                return ThreadGetRegistersX64(target, unit, processID, threadID, (uint)registerNums.Length, registerNums, registerValues);
            }
            return ThreadGetRegistersX86(target, unit, processID, threadID, (uint)registerNums.Length, registerNums, registerValues);
        }

        public static SNRESULT GetTargetInfo(ref TargetInfo targetInfo)
        {
            TargetInfoPriv targetInfoPriv = new TargetInfoPriv
            {
                Flags = targetInfo.Flags,
                Target = targetInfo.Target
            };
            SNRESULT res = Is32Bit() ? GetTargetInfoX86(ref targetInfoPriv) : GetTargetInfoX64(ref targetInfoPriv);
            if (!FAILED(res))
            {
                targetInfo.Flags = targetInfoPriv.Flags;
                targetInfo.Target = targetInfoPriv.Target;
                targetInfo.Name = Utf8ToString(targetInfoPriv.Name, uint.MaxValue);
                targetInfo.Type = Utf8ToString(targetInfoPriv.Type, uint.MaxValue);
                targetInfo.Info = Utf8ToString(targetInfoPriv.Info, uint.MaxValue);
                targetInfo.HomeDir = Utf8ToString(targetInfoPriv.HomeDir, uint.MaxValue);
                targetInfo.FSDir = Utf8ToString(targetInfoPriv.FSDir, uint.MaxValue);
                targetInfo.Boot = targetInfoPriv.Boot;
            }
            return res;
        }

        public static SNRESULT GetTargetFromName(string name, out int target)
        {
            ScopedGlobalHeapPtr ptr = new ScopedGlobalHeapPtr(AllocUtf8FromString(name));
            if (!Is32Bit())
            {
                return GetTargetFromNameX64(ptr.Get(), out target);
            }
            return GetTargetFromNameX86(ptr.Get(), out target);
        }

        public static SNRESULT GetConnectionInfo(int target, out TCPIPConnectProperties connectProperties)
        {
            connectProperties = null;
            ScopedGlobalHeapPtr ptr = new ScopedGlobalHeapPtr(Marshal.AllocHGlobal(Marshal.SizeOf(typeof(TCPIPConnectProperties))));
            SNRESULT res = Is32Bit() ? GetConnectionInfoX86(target, ptr.Get()) : GetConnectionInfoX64(target, ptr.Get());
            if (SUCCEEDED(res))
            {
                connectProperties = new TCPIPConnectProperties();
                Marshal.PtrToStructure(ptr.Get(), connectProperties);
            }
            return res;
        }

        public static SNRESULT GetConnectStatus(int target, out ConnectStatus status, out string usage)
        {
            IntPtr ptr;
            uint num;
            SNRESULT snresult = Is32Bit() ? GetConnectStatusX86(target, out num, out ptr) : GetConnectStatusX64(target, out num, out ptr);
            status = (ConnectStatus)num;
            usage = Utf8ToString(ptr, uint.MaxValue);
            return snresult;
        }

        public static SNRESULT Reset(int target, ResetParameter resetParameter)
        {
            if (!Is32Bit())
            {
                return ResetX64(target, (ulong)resetParameter);
            }
            return ResetX86(target, (ulong)resetParameter);
        }

        public static SNRESULT ProcessGetMemory(int target, UnitType unit, uint processID, ulong threadID, ulong address, ref byte[] buffer)
        {
            if (!Is32Bit())
            {
                return ProcessGetMemoryX64(target, unit, processID, threadID, address, buffer.Length, buffer);
            }
            return ProcessGetMemoryX86(target, unit, processID, threadID, address, buffer.Length, buffer);
        }

        public static SNRESULT ProcessSetMemory(int target, UnitType unit, uint processID, ulong threadID, ulong address, byte[] buffer)
        {
            if (!Is32Bit())
            {
                return ProcessSetMemoryX64(target, unit, processID, threadID, address, buffer.Length, buffer);
            }
            return ProcessSetMemoryX86(target, unit, processID, threadID, address, buffer.Length, buffer);
        }

        public static SNRESULT SetDABR(int target, uint processID, ulong address)
        {
            if (!Is32Bit())
            {
                return SetDABRX64(target, processID, address);
            }
            return SetDABRX86(target, processID, address);
        }

        public static SNRESULT GetDABR(int target, uint processID, out ulong address)
        {
            if (!Is32Bit())
            {
                return GetDABRX64(target, processID, out address);
            }
            return GetDABRX86(target, processID, out address);
        }

        public static SNRESULT RegisterTargetEventHandler(int target, TargetEventCallback callback, ref object userData)
        {
            SNRESULT result = !Is32Bit()
                ? RegisterTargetEventHandlerX64(target, ms_eventHandlerWrapper, IntPtr.Zero)
                : RegisterTargetEventHandlerX86(target, ms_eventHandlerWrapper, IntPtr.Zero);

            if (SUCCEEDED(result))
            {
                Dictionary<int, TargetCallbackAndUserData> callbacks = UserTargetCallbacks;
                lock (callbacks)
                {
                    callbacks[target] = new TargetCallbackAndUserData
                    {
                        m_callback = callback,
                        m_userData = userData
                    };
                }
            }

            return result;
        }

        public static SNRESULT CancelTargetEvents(int target)
        {
            SNRESULT result = !Is32Bit() ? CancelTargetEventsX64(target) : CancelTargetEventsX86(target);
            if (SUCCEEDED(result))
            {
                Dictionary<int, TargetCallbackAndUserData> callbacks = UserTargetCallbacks;
                lock (callbacks)
                {
                    callbacks.Remove(target);
                }
            }

            return result;
        }

        public static SNRESULT Disconnect(int target)
        {
            if (!Is32Bit())
            {
                return DisconnectX64(target);
            }
            return DisconnectX86(target);
        }

        public static SNRESULT EnableAutoStatusUpdate(int target, bool enabled, out bool previousState)
        {
            uint previousStateValue;
            SNRESULT result = !Is32Bit()
                ? EnableAutoStatusUpdateX64(target, enabled ? 1U : 0U, out previousStateValue)
                : EnableAutoStatusUpdateX86(target, enabled ? 1U : 0U, out previousStateValue);

            previousState = previousStateValue != 0;
            return result;
        }

        public static SNRESULT GetThreadList(int target, uint processID, out ulong[] ppuThreadIDs, out ulong[] spuThreadIDs)
        {
            ppuThreadIDs = new ulong[0];
            spuThreadIDs = new ulong[0];

            uint numPpu = 0;
            uint numSpu = 0;
            SNRESULT result = !Is32Bit()
                ? ThreadListX64(target, processID, ref numPpu, null, ref numSpu, null)
                : ThreadListX86(target, processID, ref numPpu, null, ref numSpu, null);

            if (!SUCCEEDED(result))
                return result;

            ppuThreadIDs = new ulong[numPpu];
            spuThreadIDs = new ulong[numSpu];
            return !Is32Bit()
                ? ThreadListX64(target, processID, ref numPpu, ppuThreadIDs, ref numSpu, spuThreadIDs)
                : ThreadListX86(target, processID, ref numPpu, ppuThreadIDs, ref numSpu, spuThreadIDs);
        }

        public static SNRESULT GetPPUThreadInfo(int target, uint processID, ulong threadID, out PPUThreadInfo threadInfo)
        {
            threadInfo = new PPUThreadInfo();
            uint bufferSize = 0;
            SNRESULT result = !Is32Bit()
                ? GetThreadInfoX64(target, UnitType.PPU, processID, threadID, ref bufferSize, IntPtr.Zero)
                : GetThreadInfoX86(target, UnitType.PPU, processID, threadID, ref bufferSize, IntPtr.Zero);

            if (FAILED(result) && result != SNRESULT.SN_E_INSUFFICIENT_DATA)
                return result;

            int headerSize = Marshal.SizeOf(typeof(PPUThreadInfoPriv));
            if (bufferSize < headerSize)
                bufferSize = (uint)headerSize;

            IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);
            try
            {
                result = !Is32Bit()
                    ? GetThreadInfoX64(target, UnitType.PPU, processID, threadID, ref bufferSize, buffer)
                    : GetThreadInfoX86(target, UnitType.PPU, processID, threadID, ref bufferSize, buffer);

                if (FAILED(result))
                    return result;

                PPUThreadInfoPriv threadInfoPriv = (PPUThreadInfoPriv)Marshal.PtrToStructure(buffer, typeof(PPUThreadInfoPriv));
                threadInfo.ThreadID = threadInfoPriv.ThreadID;
                threadInfo.Priority = threadInfoPriv.Priority;
                threadInfo.State = (PPUThreadState)threadInfoPriv.State;
                threadInfo.StackAddress = threadInfoPriv.StackAddress;
                threadInfo.StackSize = threadInfoPriv.StackSize;
                threadInfo.ThreadName = "";

                if (threadInfoPriv.ThreadNameLen > 0 && bufferSize > headerSize)
                    threadInfo.ThreadName = Utf8ToString(new IntPtr(buffer.ToInt64() + headerSize), threadInfoPriv.ThreadNameLen);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }

            return result;
        }

        public static SNRESULT GetProcessTree(int target, out ProcessTreeBranch[] processTree)
        {
            processTree = new ProcessTreeBranch[0];
            uint numProcesses = 0;
            SNRESULT result = !Is32Bit()
                ? GetProcessTreeX64(target, ref numProcesses, IntPtr.Zero)
                : GetProcessTreeX86(target, ref numProcesses, IntPtr.Zero);

            if (FAILED(result) && result != SNRESULT.SN_E_INSUFFICIENT_DATA)
                return result;

            if (numProcesses == 0)
                return result;

            int branchSize = Marshal.SizeOf(typeof(ProcessTreeBranchPriv));
            IntPtr buffer = Marshal.AllocHGlobal((int)(branchSize * numProcesses));
            try
            {
                result = !Is32Bit()
                    ? GetProcessTreeX64(target, ref numProcesses, buffer)
                    : GetProcessTreeX86(target, ref numProcesses, buffer);

                if (FAILED(result))
                    return result;

                processTree = new ProcessTreeBranch[numProcesses];
                for (int index = 0; index < numProcesses; index++)
                {
                    IntPtr branchPtr = new IntPtr(buffer.ToInt64() + branchSize * index);
                    ProcessTreeBranchPriv branchPriv = (ProcessTreeBranchPriv)Marshal.PtrToStructure(branchPtr, typeof(ProcessTreeBranchPriv));
                    processTree[index] = MarshalProcessTreeBranch(branchPriv);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }

            return result;
        }

        private static ProcessTreeBranch MarshalProcessTreeBranch(ProcessTreeBranchPriv branchPriv)
        {
            ProcessTreeBranch branch = new ProcessTreeBranch();
            branch.ProcessID = branchPriv.ProcessId;
            branch.ProcessState = branchPriv.ProcessState;
            branch.ProcessFlags = branchPriv.ProcessFlags;
            branch.RawSPU = branchPriv.RawSPU;
            branch.PPUThreadStatuses = new PPUThreadStatus[branchPriv.NumPpuThreads];
            branch.SPUThreadGroupStatuses = new SPUThreadGroupStatus[branchPriv.NumSpuThreadGroups];

            int ppuStatusSize = Marshal.SizeOf(typeof(PPUThreadStatus));
            for (int index = 0; branchPriv.PpuThreadStatuses != IntPtr.Zero && index < branch.PPUThreadStatuses.Length; index++)
            {
                IntPtr statusPtr = new IntPtr(branchPriv.PpuThreadStatuses.ToInt64() + ppuStatusSize * index);
                branch.PPUThreadStatuses[index] = (PPUThreadStatus)Marshal.PtrToStructure(statusPtr, typeof(PPUThreadStatus));
            }

            int spuStatusSize = Marshal.SizeOf(typeof(SPUThreadGroupStatus));
            for (int index = 0; branchPriv.SpuThreadGroupStatuses != IntPtr.Zero && index < branch.SPUThreadGroupStatuses.Length; index++)
            {
                IntPtr statusPtr = new IntPtr(branchPriv.SpuThreadGroupStatuses.ToInt64() + spuStatusSize * index);
                branch.SPUThreadGroupStatuses[index] = (SPUThreadGroupStatus)Marshal.PtrToStructure(statusPtr, typeof(SPUThreadGroupStatus));
            }

            return branch;
        }

        private static void EventHandlerWrapper(int target, EventType type, uint param, SNRESULT result, uint length, IntPtr data, IntPtr userData)
        {
            PublishNativeEventDiagnostic(target, type, param, result, length);

            if (type == EventType.Target)
                MarshalTargetEvent(target, param, result, length, data);
        }

        private static void PublishNativeEventDiagnostic(int target, EventType type, uint param, SNRESULT result, uint length)
        {
            Action<string> sink = NativeEventDiagnosticSink;
            if (sink == null)
                return;

            try
            {
                sink("Native TMAPI callback: target=" + target.ToString() +
                    " type=" + type.ToString() +
                    " param=0x" + param.ToString("X8") +
                    " result=" + result.ToString() +
                    " length=" + length.ToString("N0") + ".");
            }
            catch
            {
            }
        }

        private static void MarshalTargetEvent(int target, uint param, SNRESULT result, uint length, IntPtr data)
        {
            TargetCallbackAndUserData callbackAndUserData;
            Dictionary<int, TargetCallbackAndUserData> callbacks = UserTargetCallbacks;
            lock (callbacks)
            {
                if (!callbacks.TryGetValue(target, out callbackAndUserData))
                    return;
            }

            TargetEvent[] events = ParseTargetEvents(length, data);
            callbackAndUserData.m_callback(target, result, events, callbackAndUserData.m_userData);
        }

        private static TargetEvent[] ParseTargetEvents(uint length, IntPtr data)
        {
            if (data == IntPtr.Zero || length == 0)
                return new TargetEvent[0];

            List<TargetEvent> events = new List<TargetEvent>();
            int offset = 0;
            int totalLength = length > Int32.MaxValue ? Int32.MaxValue : (int)length;
            int headerSize = Marshal.SizeOf(typeof(TargetEventHdrPriv));

            while (offset + headerSize <= totalLength)
            {
                TargetEventHdrPriv eventHeader = (TargetEventHdrPriv)Marshal.PtrToStructure(new IntPtr(data.ToInt64() + offset), typeof(TargetEventHdrPriv));

                if (eventHeader.Size < headerSize || offset + eventHeader.Size > totalLength)
                    break;

                TargetEvent targetEvent = new TargetEvent();
                targetEvent.TargetID = eventHeader.TargetID;
                targetEvent.Size = eventHeader.Size;
                targetEvent.Type = (TargetEventType)eventHeader.EventType;

                IntPtr eventData = new IntPtr(data.ToInt64() + offset + headerSize);
                uint eventDataSize = eventHeader.Size - (uint)headerSize;
                if (targetEvent.Type == TargetEventType.TargetSpecific)
                {
                    targetEvent.TargetSpecific = MarshalTargetSpecificEvent(eventDataSize, eventData);
                    targetEvent.TargetSpecific.TargetEventSize = eventHeader.Size;
                    targetEvent.TargetSpecific.TargetEventTypeRaw = eventHeader.EventType;
                }
                else if (targetEvent.Type == TargetEventType.UnitStatusChange)
                    targetEvent.EventData.UnitStatusChangeData = MarshalUnitStatusChangeEvent(eventDataSize, eventData);

                events.Add(targetEvent);
                offset += (int)eventHeader.Size;
            }

            return events.ToArray();
        }

        private static TGTEventUnitStatusChangeData MarshalUnitStatusChangeEvent(uint eventSize, IntPtr data)
        {
            TGTEventUnitStatusChangeData unitStatusChangeData = new TGTEventUnitStatusChangeData();
            if (eventSize < 8)
                return unitStatusChangeData;

            unitStatusChangeData.Unit = (UnitType)Marshal.ReadInt32(data, 0);
            unitStatusChangeData.Status = (UnitStatus)(uint)Marshal.ReadInt32(data, 4);
            return unitStatusChangeData;
        }

        private static TargetSpecificEvent MarshalTargetSpecificEvent(uint eventSize, IntPtr data)
        {
            TargetSpecificEvent targetSpecific = new TargetSpecificEvent();
            int debugHeaderSize = Marshal.SizeOf(typeof(DebugEventHdrPriv));
            if (data == IntPtr.Zero || eventSize < debugHeaderSize + 4)
                return targetSpecific;

            DebugEventHdrPriv debugHeader = (DebugEventHdrPriv)Marshal.PtrToStructure(data, typeof(DebugEventHdrPriv));
            targetSpecific.CommandID = debugHeader.CommandID;
            targetSpecific.RequestID = debugHeader.RequestID;
            targetSpecific.DataLength = debugHeader.DataLength;
            targetSpecific.ProcessID = debugHeader.ProcessID;
            targetSpecific.Result = debugHeader.Result;

            uint availableDebugData = eventSize - (uint)debugHeaderSize;
            uint debugDataLength = debugHeader.DataLength == 0
                ? availableDebugData
                : Math.Min(debugHeader.DataLength, availableDebugData);

            if (debugDataLength < 4)
                return targetSpecific;

            IntPtr debugData = new IntPtr(data.ToInt64() + debugHeaderSize);
            targetSpecific.RawDebugDataHex = BytesToHex(debugData, Math.Min(debugDataLength, 64));
            targetSpecific.Data.Type = (TargetSpecificEventType)(uint)Marshal.ReadInt32(debugData, 0);

            IntPtr payload = new IntPtr(debugData.ToInt64() + 4);
            uint payloadSize = debugDataLength - 4;
            targetSpecific.PayloadOffset = (uint)debugHeaderSize + 4;
            switch (targetSpecific.Data.Type)
            {
                case TargetSpecificEventType.PPUExcAlignment:
                    targetSpecific.Data.PPUAlignmentException = ReadPPUAlignmentExceptionData(payload, payloadSize);
                    break;

                case TargetSpecificEventType.PPUExcDataMAT:
                    targetSpecific.Data.PPUDataMatException = ReadPPUDataMatExceptionData(payload, payloadSize);
                    break;

                default:
                    if (IsPPUExceptionEvent(targetSpecific.Data.Type))
                        targetSpecific.Data.PPUException = ReadPPUExceptionData(payload, payloadSize);
                    break;
            }

            return targetSpecific;
        }

        private static string BytesToHex(IntPtr data, uint count)
        {
            if (data == IntPtr.Zero || count == 0)
                return String.Empty;

            int byteCount = count > Int32.MaxValue ? Int32.MaxValue : (int)count;
            byte[] bytes = new byte[byteCount];
            Marshal.Copy(data, bytes, 0, byteCount);

            StringBuilder builder = new StringBuilder(byteCount * 2);
            for (int index = 0; index < bytes.Length; index++)
                builder.Append(bytes[index].ToString("X2"));

            return builder.ToString();
        }

        private static bool IsPPUExceptionEvent(TargetSpecificEventType eventType)
        {
            return eventType >= TargetSpecificEventType.PPUExcTrap &&
                eventType <= TargetSpecificEventType.PPUExcStopInit;
        }

        private static PPUExceptionData ReadPPUExceptionData(IntPtr data, uint available)
        {
            PPUExceptionData exceptionData = new PPUExceptionData();
            if (available < 32)
                return exceptionData;

            exceptionData.ThreadID = (ulong)Marshal.ReadInt64(data, 0);
            exceptionData.HWThreadNumber = (uint)Marshal.ReadInt32(data, 8);
            exceptionData.PC = (ulong)Marshal.ReadInt64(data, 16);
            exceptionData.SP = (ulong)Marshal.ReadInt64(data, 24);
            return exceptionData;
        }

        private static PPUAlignmentExceptionData ReadPPUAlignmentExceptionData(IntPtr data, uint available)
        {
            PPUAlignmentExceptionData exceptionData = new PPUAlignmentExceptionData();
            if (available < 48)
                return exceptionData;

            exceptionData.ThreadID = (ulong)Marshal.ReadInt64(data, 0);
            exceptionData.HWThreadNumber = (uint)Marshal.ReadInt32(data, 8);
            exceptionData.DSISR = (ulong)Marshal.ReadInt64(data, 16);
            exceptionData.DAR = (ulong)Marshal.ReadInt64(data, 24);
            exceptionData.PC = (ulong)Marshal.ReadInt64(data, 32);
            exceptionData.SP = (ulong)Marshal.ReadInt64(data, 40);
            return exceptionData;
        }

        private static PPUDataMatExceptionData ReadPPUDataMatExceptionData(IntPtr data, uint available)
        {
            PPUDataMatExceptionData exceptionData = new PPUDataMatExceptionData();
            if (available < 48)
                return exceptionData;

            exceptionData.ThreadID = (ulong)Marshal.ReadInt64(data, 0);
            exceptionData.HWThreadNumber = (uint)Marshal.ReadInt32(data, 8);
            exceptionData.DSISR = (ulong)Marshal.ReadInt64(data, 16);
            exceptionData.DAR = (ulong)Marshal.ReadInt64(data, 24);
            exceptionData.PC = (ulong)Marshal.ReadInt64(data, 32);
            exceptionData.SP = (ulong)Marshal.ReadInt64(data, 40);
            return exceptionData;
        }

    }
}
