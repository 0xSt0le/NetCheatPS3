using System;
using System.Collections.Generic;

namespace NetCheatPS3
{
    public partial class Form1
    {
        #region Interface Functions

        [ThreadStatic]
        private static List<MemoryWriteVerificationResult> memoryWriteVerificationResults;

        [ThreadStatic]
        private static bool memoryWriteVerificationActive;

        public sealed class MemoryWriteVerificationResult
        {
            public ulong Address;
            public byte[] Expected;
            public byte[] Actual;
            public bool ReadSucceeded;
            public bool Matches;
            public string ErrorMessage;
        }

        public static void BeginMemoryWriteVerification()
        {
            memoryWriteVerificationResults = new List<MemoryWriteVerificationResult>();
            memoryWriteVerificationActive = true;
        }

        public static MemoryWriteVerificationResult[] EndMemoryWriteVerification()
        {
            MemoryWriteVerificationResult[] results = memoryWriteVerificationResults != null
                ? memoryWriteVerificationResults.ToArray()
                : new MemoryWriteVerificationResult[0];

            memoryWriteVerificationActive = false;
            memoryWriteVerificationResults = null;

            return results;
        }

        public static void apiSetMem(ulong addr, byte[] val)
        {
            if (val != null && connected)
            {
                byte[] newV = new byte[val.Length];
                Array.Copy(val, 0, newV, 0, val.Length);
                newV = misc.notrevif(newV);

                try
                {
                    curAPI.Instance.SetBytes(addr, newV);
                    RecordMemoryWriteVerification(addr, newV);
                }
                catch
                {
                    if (connected && attached)
                        ValidateAttachedMemoryStateAfterAccessFailure();
                }
            }
        }

        private static void RecordMemoryWriteVerification(ulong addr, byte[] expected)
        {
            if (!memoryWriteVerificationActive || memoryWriteVerificationResults == null)
                return;

            MemoryWriteVerificationResult result = new MemoryWriteVerificationResult();
            result.Address = addr;
            result.Expected = CloneByteArray(expected);
            result.Actual = new byte[expected.Length];

            try
            {
                byte[] actual = new byte[expected.Length];
                result.ReadSucceeded = curAPI != null && curAPI.Instance != null && curAPI.Instance.GetBytes(addr, ref actual);
                result.Actual = CloneByteArray(actual);
                result.Matches = result.ReadSucceeded && ByteArraysEqual(expected, actual);
                if (!result.ReadSucceeded)
                {
                    result.ErrorMessage = "Read-back failed.";
                    ValidateAttachedMemoryStateAfterAccessFailure();
                }
            }
            catch (Exception ex)
            {
                result.ReadSucceeded = false;
                result.Matches = false;
                result.ErrorMessage = ex.Message;
                ValidateAttachedMemoryStateAfterAccessFailure();
            }

            memoryWriteVerificationResults.Add(result);
        }

        private static byte[] CloneByteArray(byte[] value)
        {
            if (value == null)
                return new byte[0];

            byte[] clone = new byte[value.Length];
            Array.Copy(value, 0, clone, 0, value.Length);
            return clone;
        }

        private static bool ByteArraysEqual(byte[] expected, byte[] actual)
        {
            if (expected == null || actual == null || expected.Length != actual.Length)
                return false;

            for (int x = 0; x < expected.Length; x++)
                if (expected[x] != actual[x])
                    return false;

            return true;
        }

        public static bool apiGetMem(ulong addr, ref byte[] val)
        {
            bool ret = false;
            if (val != null && connected)
            {
                try
                {
                    ret = curAPI.Instance.GetBytes(addr, ref val);
                    if (!ret && connected && attached)
                        ValidateAttachedMemoryStateAfterAccessFailure();
                }
                catch
                {
                    if (connected && attached)
                        ValidateAttachedMemoryStateAfterAccessFailure();

                    ret = false;
                }
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
    }
}
