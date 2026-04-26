using System;

namespace NetCheatPS3
{
    public partial class Form1
    {
        #region Interface Functions

        public static void apiSetMem(ulong addr, byte[] val)
        {
            if (val != null && connected)
            {
                byte[] newV = new byte[val.Length];
                Array.Copy(val, 0, newV, 0, val.Length);
                newV = misc.notrevif(newV);
                curAPI.Instance.SetBytes(addr, newV);
            }
        }

        public static bool apiGetMem(ulong addr, ref byte[] val)
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
    }
}
