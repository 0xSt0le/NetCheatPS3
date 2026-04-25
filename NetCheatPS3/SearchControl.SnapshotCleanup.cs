using System;
using System.IO;
using System.Windows.Forms;

namespace NetCheatPS3
{
    public partial class SearchControl
    {
        private bool snapshotCleanupFormCloseHooked = false;
        private bool snapshotCleanupHookRetryQueued = false;

        private void EnsureSnapshotCleanupOnFormClose()
        {
            if (snapshotCleanupFormCloseHooked)
                return;

            Form parentForm = FindForm();

            if (parentForm == null)
            {
                QueueSnapshotCleanupHookRetry();
                return;
            }

            parentForm.FormClosing -= ParentForm_SnapshotCleanupFormClosing;
            parentForm.FormClosing += ParentForm_SnapshotCleanupFormClosing;
            snapshotCleanupFormCloseHooked = true;
        }

        private void QueueSnapshotCleanupHookRetry()
        {
            if (snapshotCleanupHookRetryQueued)
                return;

            snapshotCleanupHookRetryQueued = true;

            try
            {
                if (IsHandleCreated)
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        snapshotCleanupHookRetryQueued = false;
                        EnsureSnapshotCleanupOnFormClose();
                    });
                }
                else
                {
                    HandleCreated += delegate
                    {
                        if (!snapshotCleanupFormCloseHooked)
                            BeginInvoke((MethodInvoker)delegate { EnsureSnapshotCleanupOnFormClose(); });
                    };
                }
            }
            catch
            {
                snapshotCleanupHookRetryQueued = false;
            }
        }

        private void ParentForm_SnapshotCleanupFormClosing(object sender, FormClosingEventArgs e)
        {
            ClearSnapshotStateAndDeleteTempFiles();
        }

        private void ClearSnapshotStateAndDeleteTempFiles()
        {
            DeleteFirstSnapshotFile();
            DeleteAllSnapshotTempFiles();
            ResetActiveSnapshotStateOnly();
        }

        private void ResetActiveSnapshotStateOnly()
        {
            activeSnapshotPath = null;
            activeSnapshotTypeIndex = -1;
            activeSnapshotByteSize = 0;
            activeSnapshotResultCount = 0;
            activeScanUsesNewEngine = false;
            firstSnapshotPath = null;
        }

        private void DeleteAllSnapshotTempFiles()
        {
            try
            {
                string currentPath = activeSnapshotPath;
                if (!String.IsNullOrEmpty(currentPath) && File.Exists(currentPath))
                    File.Delete(currentPath);
            }
            catch
            {
            }

            try
            {
                string dir = Path.Combine(Path.GetTempPath(), "NetCheatPS3");
                if (!Directory.Exists(dir))
                    return;

                string[] snapshotFiles = Directory.GetFiles(dir, "*.ncs", SearchOption.TopDirectoryOnly);
                for (int i = 0; i < snapshotFiles.Length; i++)
                {
                    try
                    {
                        File.Delete(snapshotFiles[i]);
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }
    }
}