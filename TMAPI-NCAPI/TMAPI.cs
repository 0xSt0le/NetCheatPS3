// ************************************************* //
//    --- Copyright (c) 2014 iMCS Productions ---    //
// ************************************************* //
//              PS3Lib v4 By FM|T iMCSx              //
//                                                   //
// Features v4.4 :                                   //
// - Support CCAPI v2.6 C# by iMCSx                  //
// - Set Boot Console ID                             //
// - Popup better form with icon                     //
// - CCAPI Consoles List Popup French/English        //
// - CCAPI Get Console Info                          //
// - CCAPI Get Console List                          //
// - CCAPI Get Number Of Consoles                    //
// - Get Console Name TMAPI/CCAPI                    //
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace TMAPI_NCAPI
{
    public class TMAPI
    {
        public static int Target = 0xFF;
        public static bool AssemblyLoaded = true;
        public static PS3TMAPI.ResetParameter resetParameter;

        public TMAPI()
        {
            PS3TMAPI_NET();
        }

        public class SCECMD
        {
            /// <summary>Get the target status and return the string value.</summary>
            public string SNRESULT()
            {
                return Parameters.snresult;
            }

            /// <summary>Get the target name.</summary>
            public string GetTargetName()
            {
                if (Parameters.ConsoleName == null || Parameters.ConsoleName == String.Empty)
                {
                    PS3TMAPI.InitTargetComms();
                    PS3TMAPI.TargetInfo TargetInfo = new PS3TMAPI.TargetInfo();
                    TargetInfo.Flags = PS3TMAPI.TargetInfoFlag.TargetID;
                    TargetInfo.Target = TMAPI.Target;
                    PS3TMAPI.GetTargetInfo(ref TargetInfo);
                    Parameters.ConsoleName = TargetInfo.Name;
                }
                return Parameters.ConsoleName;
            }

            /// <summary>Get the target status and return the string value.</summary>
            public string GetStatus()
            {
                if (TMAPI.AssemblyLoaded)
                    return "NotConnected";
                Parameters.connectStatus = new PS3TMAPI.ConnectStatus();
                PS3TMAPI.GetConnectStatus(Target, out Parameters.connectStatus, out Parameters.usage);
                Parameters.Status = Parameters.connectStatus.ToString();
                return Parameters.Status;
            }

            /// <summary>Get the ProcessID by the current process.</summary>
            public uint ProcessID()
            {
                return Parameters.ProcessID;
            }

            /// <summary>Get an array of processID's.</summary>
            public uint[] ProcessIDs()
            {
                return Parameters.processIDs;
            }

            /// <summary>Get some details from your target.</summary>
            public PS3TMAPI.ConnectStatus DetailStatus()
            {
                return Parameters.connectStatus;
            }
        }

        public SCECMD SCE
        {
            get { return new SCECMD(); }
        }

        public class Parameters
        {
            public static string
                usage,
                info,
                snresult,
                Status,
                MemStatus,
                ConsoleName;
            public static uint
                ProcessID;
            public static uint[]
                processIDs;
            public static byte[]
                Retour;
            public static PS3TMAPI.ConnectStatus
                connectStatus;
        }

        /// <summary>Enum of flag reset.</summary>
        public enum ResetTarget
        {
            Hard,
            Quick,
            ResetEx,
            Soft
        }

        public void InitComms()
        {
            PS3TMAPI.InitTargetComms();
        }

        /// <summary>Connect the default target and initialize the dll. Possible to put an int as arugment for determine which target to connect.</summary>
        public bool ConnectTarget(int TargetIndex = 0)
        {
            bool result = false;
            if (AssemblyLoaded)
                PS3TMAPI_NET();
            AssemblyLoaded = false;
            Target = TargetIndex;
            result = PS3TMAPI.SUCCEEDED(PS3TMAPI.InitTargetComms());
            result = PS3TMAPI.SUCCEEDED(PS3TMAPI.Connect(TargetIndex, null));
            return result;
        }

        /// <summary>Connect the target by is name.</summary>
        public bool ConnectTarget(string TargetName)
        {
            bool result = false;
            if (AssemblyLoaded)
                PS3TMAPI_NET();
            AssemblyLoaded = false;
            result = PS3TMAPI.SUCCEEDED(PS3TMAPI.InitTargetComms());
            if (result)
            {
                result = PS3TMAPI.SUCCEEDED(PS3TMAPI.GetTargetFromName(TargetName, out Target));
                result = PS3TMAPI.SUCCEEDED(PS3TMAPI.Connect(Target, null));
            }
            return result;
        }

        /// <summary>Disconnect the target.</summary>
        public void DisconnectTarget()
        {
            PS3TMAPI.Disconnect(Target);
        }

        /// <summary>Get thread list.</summary>
        public PS3TMAPI.SNRESULT GetThreadList(int target, uint processID, out ulong[] ppuThreadIDs, out ulong[] spuThreadIDs)
        {
            return PS3TMAPI.GetThreadList(target, processID, out ppuThreadIDs, out spuThreadIDs);
        }

        /// <summary>Get thread list.</summary>
        public PS3TMAPI.SNRESULT GetPPUThreadInfo(int target, uint processID, ulong threadID, out PS3TMAPI.PPUThreadInfo threadInfo)
        {
            return PS3TMAPI.GetPPUThreadInfo(target, processID, threadID, out threadInfo);
        }

        /// <summary>Power on selected target.</summary>
        public void PowerOn(int numTarget = 0)
        {
            if (Target != 0xFF)
                numTarget = Target;
            PS3TMAPI.PowerOn(numTarget);
        }

        /// <summary>Power off selected target.</summary>
        public void PowerOff(bool Force)
        {
            PS3TMAPI.PowerOff(Target, Force);
        }

        /// <summary>Attach and continue the current process from the target.</summary>
        public bool AttachProcess()
        {
            bool isOK = false;
            PS3TMAPI.GetProcessList(Target, out Parameters.processIDs);
            if (Parameters.processIDs.Length > 0)
                isOK = true;
            else isOK = false;
            if (isOK)
            {
                ulong uProcess = Parameters.processIDs[0];
                Parameters.ProcessID = Convert.ToUInt32(uProcess);
                PS3TMAPI.ProcessAttach(Target, PS3TMAPI.UnitType.PPU, Parameters.ProcessID);
                PS3TMAPI.ProcessContinue(Target, Parameters.ProcessID);
                Parameters.info = "The Process 0x" + Parameters.ProcessID.ToString("X8") + " Has Been Attached !";
            }
            return isOK;
        }

        /// <summary>Attach the current process from the target.</summary>
        public bool AttachProcOnly()
        {
            bool isOK = false;
            PS3TMAPI.GetProcessList(Target, out Parameters.processIDs);
            if (Parameters.processIDs.Length > 0)
                isOK = true;
            else isOK = false;
            if (isOK)
            {
                ulong uProcess = Parameters.processIDs[0];
                Parameters.ProcessID = Convert.ToUInt32(uProcess);
                PS3TMAPI.ProcessAttach(Target, PS3TMAPI.UnitType.PPU, Parameters.ProcessID);
                Parameters.info = "The Process 0x" + Parameters.ProcessID.ToString("X8") + " Has Been Attached !";
            }
            return isOK;
        }

        public void ContinueProcess()
        {
            PS3TMAPI.ProcessContinue(Target, Parameters.ProcessID);
        }

        /// <summary>Set memory to the target (byte[]).</summary>
        public void SetMemory(uint Address, byte[] Bytes)
        {
            PS3TMAPI.ProcessSetMemory(Target, PS3TMAPI.UnitType.PPU, Parameters.ProcessID, 0, Address, Bytes);
        }

        /// <summary>Set memory to the address (byte[]).</summary>
        public void SetMemory(uint Address, ulong value)
        {
            byte[] b = BitConverter.GetBytes(value);
            Array.Reverse(b);
            PS3TMAPI.ProcessSetMemory(Target, PS3TMAPI.UnitType.PPU, Parameters.ProcessID, 0, Address, b);
        }

        /// <summary>Set memory with value as string hexadecimal to the address (string).</summary>
        public void SetMemory(uint Address, string hexadecimal)
        {
            byte[] Entry = StringToByteArray(hexadecimal);
            Array.Reverse(Entry);
            PS3TMAPI.ProcessSetMemory(Target, PS3TMAPI.UnitType.PPU, Parameters.ProcessID, 0, Address, Entry);
        }

        /// <summary>Get memory from the address.</summary>
        public PS3TMAPI.SNRESULT GetMemory(uint Address, byte[] Bytes)
        {
            return PS3TMAPI.ProcessGetMemory(Target, PS3TMAPI.UnitType.PPU, Parameters.ProcessID, 0, Address, ref Bytes);
        }

        /// <summary>Get a bytes array with the length input.</summary>
        public byte[] GetBytes(uint Address, uint lengthByte)
        {
            byte[] Longueur = new byte[lengthByte];
            PS3TMAPI.ProcessGetMemory(Target, PS3TMAPI.UnitType.PPU, Parameters.ProcessID, 0, Address, ref Longueur);
            return Longueur;
        }

        /// <summary>Get a string with the length input.</summary>
        public string GetString(uint Address, uint lengthString)
        {
            byte[] Longueur = new byte[lengthString];
            PS3TMAPI.ProcessGetMemory(Target, PS3TMAPI.UnitType.PPU, Parameters.ProcessID, 0, Address, ref Longueur);
            string StringResult = Hex2Ascii(ReplaceString(Longueur));
            return StringResult;
        }

        internal static string Hex2Ascii(string iMCSxString)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i <= (iMCSxString.Length - 2); i += 2)
            {
                builder.Append(Convert.ToString(Convert.ToChar(int.Parse(iMCSxString.Substring(i, 2), NumberStyles.HexNumber))));
            }
            return builder.ToString();
        }

        internal static string ByteArrayToString(byte[] buffer, int startIndex, int maxLength = 0)
        {
            int max = startIndex + maxLength;
            if (max == startIndex)
                max = buffer.Length;
            string ret = "";

            for (int x = startIndex; x < max; x++)
            {
                if (buffer[x] == 0)
                    break;
                ret += ((char)buffer[x]).ToString();
            }
            return ret;
        }

        internal static byte[] StringToByteArray(string hex)
        {
            string replace = hex.Replace("0x", "");
            string Stringz = replace.Insert(replace.Length - 1, "0");

            int Odd = replace.Length;
            bool Nombre;
            if (Odd % 2 == 0)
                Nombre = true;
            else
                Nombre = false;
            try
            {
                if (Nombre == true)
                {
                    return Enumerable.Range(0, replace.Length)
                 .Where(x => x % 2 == 0)
                 .Select(x => Convert.ToByte(replace.Substring(x, 2), 16))
                 .ToArray();
                }
                else
                {
                    return Enumerable.Range(0, replace.Length)
                 .Where(x => x % 2 == 0)
                 .Select(x => Convert.ToByte(Stringz.Substring(x, 2), 16))
                 .ToArray();
                }
            }
            catch { throw new System.ArgumentException("Value not possible.", "Byte Array"); }
        }

        internal static string ReplaceString(byte[] bytes)
        {
            string PSNString = BitConverter.ToString(bytes);
            PSNString = PSNString.Replace("00", string.Empty);
            PSNString = PSNString.Replace("-", string.Empty);
            for (int i = 0; i < 10; i++)
                PSNString = PSNString.Replace("^" + i.ToString(), string.Empty);
            return PSNString;
        }

        /// <summary>Reset target to XMB , Sometimes the target restart quickly.</summary>
        public void ResetToXMB(ResetTarget flag)
        {
            if (flag == ResetTarget.Hard)
                resetParameter = PS3TMAPI.ResetParameter.Hard;
            else if (flag == ResetTarget.Quick)
                resetParameter = PS3TMAPI.ResetParameter.Quick;
            else if (flag == ResetTarget.ResetEx)
                resetParameter = PS3TMAPI.ResetParameter.ResetEx;
            else if (flag == ResetTarget.Soft)
                resetParameter = PS3TMAPI.ResetParameter.Soft;
            PS3TMAPI.Reset(Target, resetParameter);
        }

        internal static Assembly LoadApi;
        private static bool _ps3TmApiResolverRegistered = false;
        private static bool _ps3TmApiResolverErrorShown = false;
        private static bool _assemblyResolverRegistered = false;
        private static bool _assemblyResolverErrorShown = false;
        private static readonly object _assemblyResolverLock = new object();
        ///<summary>Load the PS3 API for use with your Application .NET.</summary>
                                        public Assembly PS3TMAPI_NET()
        {
            if (LoadApi != null)
                return LoadApi;

            if (!_ps3TmApiResolverRegistered)
            {
                AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
                {
                    try
                    {
                        string requestedName = new AssemblyName(e.Name).Name;
                        if (!String.Equals(requestedName, "ps3tmapi_net", StringComparison.OrdinalIgnoreCase))
                            return null;

                        if (LoadApi != null)
                            return LoadApi;

                        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                        string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                        string pfx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                        string pfw6432 = Environment.GetEnvironmentVariable("ProgramW6432");

                        List<string> candidates = new List<string>();

                        if (!String.IsNullOrEmpty(baseDir))
                        {
                            candidates.Add(Path.Combine(baseDir, "ps3tmapi_net.dll"));
                            candidates.Add(Path.Combine(baseDir, "APIs", "ps3tmapi_net.dll"));
                            candidates.Add(Path.Combine(baseDir, "APIs", "TMAPI-NCAPI", "ps3tmapi_net.dll"));
                        }

                        if (!String.IsNullOrEmpty(pf))
                            candidates.Add(Path.Combine(pf, "SN Systems", "PS3", "bin", "ps3tmapi_net.dll"));

                        if (!String.IsNullOrEmpty(pfx86))
                            candidates.Add(Path.Combine(pfx86, "SN Systems", "PS3", "bin", "ps3tmapi_net.dll"));

                        if (!String.IsNullOrEmpty(pfw6432))
                            candidates.Add(Path.Combine(pfw6432, "SN Systems", "PS3", "bin", "ps3tmapi_net.dll"));

                        List<string> loadErrors = new List<string>();

                        foreach (string candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
                        {
                            if (!File.Exists(candidate))
                                continue;

                            try
                            {
                                LoadApi = Assembly.LoadFile(candidate);
                                return LoadApi;
                            }
                            catch (Exception ex)
                            {
                                loadErrors.Add(candidate + " -> " + ex.GetType().Name + ": " + ex.Message);
                            }
                        }

                        if (!_ps3TmApiResolverErrorShown)
                        {
                            _ps3TmApiResolverErrorShown = true;

                            string checkedPaths = String.Join("\r\n", candidates.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
                            string errors = loadErrors.Count > 0 ? ("\r\n\r\nLoad errors:\r\n" + String.Join("\r\n", loadErrors.ToArray())) : "";

                            MessageBox.Show(
                                "Target Manager API could not load ps3tmapi_net.dll.\r\n\r\n" +
                                "Checked paths:\r\n" + checkedPaths + errors,
                                "Error with PS3 API!",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                        }

                        return null;
                    }
                    catch
                    {
                        return null;
                    }
                };

                _ps3TmApiResolverRegistered = true;
            }

            return LoadApi;
        }

        private static Assembly ResolvePS3TMAPIAssembly(object sender, ResolveEventArgs e)
        {
            string filename = new AssemblyName(e.Name).Name;

            if (!String.Equals(filename, "ps3tmapi_net", StringComparison.OrdinalIgnoreCase))
                return null;

            return ResolvePS3TMAPIAssemblyByName(filename);
        }

        private static Assembly ResolvePS3TMAPIAssemblyByName(string filename)
        {
            if (LoadApi != null)
                return LoadApi;

            if (String.IsNullOrEmpty(filename))
                filename = "ps3tmapi_net";

            string dllName = filename.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                ? filename
                : filename + ".dll";

            List<string> candidatePaths = new List<string>();

            Action<string> addCandidate = delegate(string path)
            {
                if (!String.IsNullOrEmpty(path) && !candidatePaths.Contains(path, StringComparer.OrdinalIgnoreCase))
                    candidatePaths.Add(path);
            };

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string currentDir = Directory.GetCurrentDirectory();

            addCandidate(Path.Combine(baseDir, dllName));
            addCandidate(Path.Combine(baseDir, "APIs", dllName));
            addCandidate(Path.Combine(baseDir, "APIs", "TMAPI-NCAPI", dllName));

            addCandidate(Path.Combine(currentDir, dllName));
            addCandidate(Path.Combine(currentDir, "APIs", dllName));
            addCandidate(Path.Combine(currentDir, "APIs", "TMAPI-NCAPI", dllName));

            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string programW6432 = Environment.GetEnvironmentVariable("ProgramW6432");

            addCandidate(Path.Combine(programFiles, "SN Systems", "PS3", "bin", dllName));
            addCandidate(Path.Combine(programFilesX86, "SN Systems", "PS3", "bin", dllName));

            if (!String.IsNullOrEmpty(programW6432))
                addCandidate(Path.Combine(programW6432, "SN Systems", "PS3", "bin", dllName));

            // Some older ProDG installs use these exact paths. Keep them explicit.
            addCandidate(@"C:\Program Files\SN Systems\PS3\bin\" + dllName);
            addCandidate(@"C:\Program Files (x86)\SN Systems\PS3\bin\" + dllName);

            Exception lastError = null;

            foreach (string candidate in candidatePaths)
            {
                try
                {
                    if (File.Exists(candidate))
                    {
                        LoadApi = Assembly.LoadFrom(candidate);
                        return LoadApi;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex;
                }
            }

            if (!_assemblyResolverErrorShown)
            {
                _assemblyResolverErrorShown = true;

                string msg =
                    "Target Manager API DLL could not be found or loaded.\r\n\r\n" +
                    "Missing DLL: " + dllName + "\r\n\r\n" +
                    "Checked paths:\r\n" +
                    String.Join("\r\n", candidatePaths.ToArray());

                if (lastError != null)
                    msg += "\r\n\r\nLast load error:\r\n" + lastError.Message;

                MessageBox.Show(msg, "Error with PS3 API!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return null;
        }
    }
}

