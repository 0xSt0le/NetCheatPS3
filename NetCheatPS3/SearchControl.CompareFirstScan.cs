using System;
using System.IO;

namespace NetCheatPS3
{
    public partial class SearchControl
    {
        private string firstSnapshotPath = null;

        private bool HasFirstSnapshot()
        {
            return !String.IsNullOrEmpty(firstSnapshotPath) && File.Exists(firstSnapshotPath);
        }

        private string GetFirstSnapshotPath()
        {
            return firstSnapshotPath;
        }

        private void CaptureFirstSnapshotAfterInitialScan(ncSearcher searcher)
        {
            DeleteFirstSnapshotFile();

            if (!HasActiveSnapshot())
                return;

            if (String.IsNullOrEmpty(activeSnapshotPath) || !File.Exists(activeSnapshotPath))
                return;

            try
            {
                firstSnapshotPath = CreateSnapshotTempPath();
                File.Copy(activeSnapshotPath, firstSnapshotPath, true);
            }
            catch (Exception ex)
            {
                firstSnapshotPath = null;
                CrashLogger.Log("SearchControl.CaptureFirstSnapshotAfterInitialScan", ex);
            }
        }

        private void DeleteFirstSnapshotFile()
        {
            try
            {
                if (!String.IsNullOrEmpty(firstSnapshotPath) && File.Exists(firstSnapshotPath))
                    File.Delete(firstSnapshotPath);
            }
            catch
            {
            }

            firstSnapshotPath = null;
        }
    }
}