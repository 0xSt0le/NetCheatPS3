using System.Windows.Forms;

namespace NetCheatPS3
{
    public partial class Form1
    {
        public static void SetMainStatusSafe(string message)
        {
            try
            {
                if (Instance != null)
                    Instance.statusLabel1.Text = message;
            }
            catch
            {
            }
        }

        public static string GetConnectionAttachErrorMessage(string actionName)
        {
            if (Instance != null)
                Instance.SynchronizeConnectionStateBeforeAction();

            if (!connected)
                return "Not connected. Connect and attach before " + actionName + ".";

            if (!attached)
                return "Not attached. Attach to the process before " + actionName + ".";

            return null;
        }

        private bool EnsureConnectedAndAttachedForMainAction(string actionName)
        {
            string message = GetConnectionAttachErrorMessage(actionName);

            if (message == null)
                return true;

            SetMainStatusSafe(message);

            MessageBox.Show(
                message,
                "NetCheatPS3",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return false;
        }
    }
}
