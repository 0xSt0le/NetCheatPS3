using System;
using System.Windows.Forms;

namespace NetCheatPS3
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.ThreadException += delegate (object sender, System.Threading.ThreadExceptionEventArgs e)
            {
                CrashLogger.Log("Application.ThreadException", e.Exception);
                MessageBox.Show(
                    "NetCheat hit an unhandled UI exception.\r\n\r\nSee NetCheatPS3_crash.log in the program folder.",
                    "NetCheatPS3",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            };

            AppDomain.CurrentDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs e)
            {
                CrashLogger.LogUnhandled("AppDomain.CurrentDomain.UnhandledException", e.ExceptionObject);
            };

            try
            {
                codes.ExitConstWriter = false;

                Form1 mainForm = new Form1();

                Form1.tConstWrite.IsBackground = true;
                if (!Form1.tConstWrite.IsAlive)
                    Form1.tConstWrite.Start();

                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                CrashLogger.Log("Program.Main", ex);
                MessageBox.Show(
                    "NetCheat crashed during startup or shutdown.\r\n\r\nSee NetCheatPS3_crash.log in the program folder.",
                    "NetCheatPS3",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                try
                {
                    Form1.ConstantLoop = 2;
                    codes.ExitConstWriter = true;

                    if (Form1.tConstWrite != null && Form1.tConstWrite.IsAlive)
                        Form1.tConstWrite.Join(1500);
                }
                catch (Exception ex)
                {
                    CrashLogger.Log("Program.Main.Finally", ex);
                }
            }
        }
    }
}