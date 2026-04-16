using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace NetCheatPS3
{
    internal static class CrashLogger
    {
        private static readonly object _sync = new object();

        public static void Log(string source, Exception ex)
        {
            try
            {
                if (ex == null)
                    return;

                string path = Path.Combine(Application.StartupPath, "NetCheatPS3_crash.log");
                StringBuilder sb = new StringBuilder();

                sb.AppendLine("==================================================");
                sb.AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                sb.AppendLine("Source: " + source);
                sb.AppendLine("Thread: " + Thread.CurrentThread.ManagedThreadId);
                sb.AppendLine("Type: " + ex.GetType().FullName);
                sb.AppendLine("Message: " + ex.Message);
                sb.AppendLine("Stack:");
                sb.AppendLine(ex.ToString());
                sb.AppendLine();

                lock (_sync)
                {
                    File.AppendAllText(path, sb.ToString());
                }
            }
            catch
            {
            }
        }

        public static void LogUnhandled(string source, object error)
        {
            try
            {
                Exception ex = error as Exception;
                if (ex == null)
                    ex = new Exception(error != null ? error.ToString() : "Unknown unhandled error");

                Log(source, ex);
            }
            catch
            {
            }
        }
    }
}