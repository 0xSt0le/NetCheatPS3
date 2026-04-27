using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace NetCheatPS3
{
    public partial class Form1
    {
        private void LoadAPIs()
        {
            apiList.Items.Clear();

            if (System.IO.Directory.Exists(Application.StartupPath + @"\APIs") == false)
                return;

            //Delete any excess NCAppInterface.dll's (result of a build and not a copy)
            foreach (string file in System.IO.Directory.GetFiles(Application.StartupPath + @"\APIs", "NCAppInterface.dll", System.IO.SearchOption.AllDirectories))
                System.IO.File.Delete(file);

            //Call the find apis routine, to search in our APIs Folder
            Global.APIs.FindAPIs(Application.StartupPath + @"\APIs");

            //Load apis
            foreach (Types.AvailableAPI apiOn in Global.APIs.AvailableAPIs)
            {
                apiList.Items.Add(apiOn.Instance.Name + " (" + apiOn.Instance.Version + ")");
            }

            if (apiList.Items.Count > 0)
                apiList.SelectedIndex = 0;
        }

        private void refPlugin_Click(object sender, EventArgs e)
        {
            if (refPlugin.Text == "Close Plugins")
            {
                foreach (PluginForm pF in pluginForm)
                {
                    pF.Close();
                }

                Global.Plugins.ClosePlugins();

                pluginList.Items.Clear();

                refPlugin.Text = "Load Plugins";
                codes.ConstCodes = new List<codes.ConstCode>();
            }
            else
            {
                int x = 0;

                //Close any open plugins
                Global.Plugins.ClosePlugins();
                pluginList.Items.Clear();

                if (System.IO.Directory.Exists(Application.StartupPath + @"\Plugins") == false)
                    return;

                //Delete any excess PluginInterface.dll's (result of a build and not a copy)
                foreach (string file in System.IO.Directory.GetFiles(Application.StartupPath + @"\Plugins", "PluginInterface.dll", System.IO.SearchOption.AllDirectories))
                    System.IO.File.Delete(file);

                //Call the find plugins routine, to search in our Plugins Folder
                Global.Plugins.FindPlugins(Application.StartupPath + @"\Plugins");

                //Load plugins
                pluginForm = Global.Plugins.GetPlugin(ncBackColor, ncForeColor);
                Array.Resize(ref pluginForm, pluginForm.Length + 1);
                pluginForm[pluginForm.Length - 1] = new PluginForm();
                pluginForm[pluginForm.Length - 1].plugAuth = snapshot.author;
                pluginForm[pluginForm.Length - 1].plugDesc = snapshot.desc;
                pluginForm[pluginForm.Length - 1].plugName = snapshot.name;
                pluginForm[pluginForm.Length - 1].plugText = snapshot.tabName;
                pluginForm[pluginForm.Length - 1].plugVers = snapshot.version;

                if (pluginForm != null)
                {
                    Array.Resize(ref pluginFormActive, pluginForm.Length);
                    for (x = 0; x < pluginForm.Length; x++)
                    {
                        pluginForm[x].Tag = x;
                        pluginForm[x].FormClosing += new FormClosingEventHandler(HandlePlugin_Closing);
                        pluginList.Items.Add(pluginForm[x].plugText);
                    }
                }

                //Fixes a bug that causes the BackColor to be white after adding another TabPage
                RangeTab.BackColor = ncForeColor;
                SearchTab.BackColor = ncForeColor;
                CodesTab.BackColor = ncForeColor;
                Application.DoEvents();
                RangeTab.BackColor = ncBackColor;
                SearchTab.BackColor = ncBackColor;
                CodesTab.BackColor = ncBackColor;

                if (pluginForm.Length != 0)
                    pluginList.SelectedIndex = 0;

                refPlugin.Text = "Close Plugins";
            }

            int index = toolStripDropDownButton1.DropDownItems.IndexOfKey("loadPluginsToolStripMenuItem");
            toolStripDropDownButton1.DropDownItems[index].Text = refPlugin.Text;
        }

        private void HandlePlugin_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            int ind = (int)((PluginForm)sender).Tag;
            if (pluginFormActive[ind])
            {
                e.Cancel = true;
                pluginList.SelectedIndex = ind;
                pluginList_DoubleClick(null, null);
            }
        }

        private void pluginList_DoubleClick(object sender, EventArgs e)
        {
            int ind = pluginList.SelectedIndex;
            if (pluginFormActive[ind]) //Already on
            {
                pluginForm[ind].WindowState = FormWindowState.Normal;
                pluginForm[ind].Visible = false;
                pluginForm[ind].WindowState = FormWindowState.Minimized;
                pluginList.Items[ind] = pluginForm[ind].plugText;
                pluginFormActive[pluginList.SelectedIndex] = false;
            }
            else //Turn on
            {
                pluginFormActive[pluginList.SelectedIndex] = true;
                pluginForm[ind].WindowState = FormWindowState.Normal;
                pluginList.Items[ind] = "+ " + pluginForm[ind].plugText;
                pluginForm[pluginList.SelectedIndex].Show();
                pluginForm[ind].Visible = true;
            }
        }

        snapshot snapShotPlugin = new snapshot();
        private void pluginList_SelectedIndexChanged(object sender, EventArgs e)
        {
            int ind = pluginList.SelectedIndex;
            if (ind < 0)
                return;

            if (pluginList.Items[ind].ToString().IndexOf(snapshot.tabName) >= 0)
            {
                descPlugAuth.Text = "by " + pluginForm[ind].plugAuth;
                descPlugName.Text = pluginForm[ind].plugName;
                descPlugVer.Text = pluginForm[ind].plugVers;
                descPlugDesc.Text = pluginForm[ind].plugDesc;
                pluginForm[ind].Text = pluginForm[ind].plugName + " by " + pluginForm[ind].plugAuth;
                plugIcon.Image = (Bitmap)plugIcon.InitialImage.Clone();

                pluginForm[ind].Controls.Clear();
                pluginForm[ind].Controls.Add(snapShotPlugin);
                pluginForm[ind].Controls[0].Resize += new EventHandler(pluginForm[ind].Plugin_Resize);
                pluginForm[ind].Resize += new EventHandler(snapShotPlugin.snapshot_Resize);

                if (pluginForm[ind].allowColoring)
                {
                    HandlePluginControls(pluginForm[ind].Controls[0].Controls);
                    pluginForm[ind].Controls[0].BackColor = ncBackColor;
                    pluginForm[ind].Controls[0].ForeColor = ncForeColor;
                }
            }
            if (ind >= 0 && pluginForm[ind] != null)
            {
                //Get the selected Plugin
                Types.AvailablePlugin selectedPlugin = Global.Plugins.AvailablePlugins.GetIndex(ind);

                if (selectedPlugin != null && pluginForm[ind].Controls.Count == 0)
                {
                    pluginForm[ind].Controls.Clear();

                    //Set the dockstyle of the plugin to fill, to fill up the space provided
                    selectedPlugin.Instance.MainInterface.Dock = DockStyle.Fill;

                    pluginForm[ind].Controls.Add(selectedPlugin.Instance.MainInterface);
                    pluginForm[ind].Controls[0].Resize += new EventHandler(pluginForm[ind].Plugin_Resize);

                    if (pluginForm[ind].allowColoring)
                    {
                        pluginForm[ind].Controls[0].BackColor = ncBackColor;
                        pluginForm[ind].Controls[0].ForeColor = ncForeColor;
                        HandlePluginControls(pluginForm[ind].Controls[0].Controls);
                    }
                }

                if (pluginForm[ind].Controls.Count > 0 && pluginList.Items[ind].ToString().IndexOf(snapshot.tabName) < 0)
                {
                    descPlugAuth.Text = "by " + selectedPlugin.Instance.Author;
                    descPlugName.Text = selectedPlugin.Instance.Name;
                    descPlugVer.Text = selectedPlugin.Instance.Version;
                    descPlugDesc.Text = selectedPlugin.Instance.Description;
                    if (selectedPlugin.Instance.MainIcon != null && selectedPlugin.Instance.MainIcon.BackgroundImage != null)
                        plugIcon.Image = (Bitmap)selectedPlugin.Instance.MainIcon.BackgroundImage.Clone();
                    else
                        plugIcon.Image = (Bitmap)plugIcon.InitialImage.Clone();
                }
            }
        }

        private void loadPluginsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            refPlugin_Click(null, null);
        }

        Types.AvailableAPI selAPI;
        private void apiList_SelectedIndexChanged(object sender, EventArgs e)
        {
            int ind = apiList.SelectedIndex;
            if (ind < 0)
                return;

            Types.AvailableAPI api = Global.APIs.AvailableAPIs.GetIndex(ind);
            descAPIAuth.Text = "by " + api.Instance.Author;
            descAPIName.Text = api.Instance.Name;
            descAPIVer.Text = api.Instance.Version;
            descAPIDesc.Text = api.Instance.Description;
            if (api.Instance.Icon != null)
                apiIcon.Image = (Bitmap)api.Instance.Icon.Clone();
            else
                apiIcon.Image = (Bitmap)apiIcon.InitialImage.Clone();

            string[] parts = apiList.Items[ind].ToString().Split(new char[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
            int apiIndex = Global.APIs.AvailableAPIs.GetIndex(parts[0].Trim(), parts[1]);
            selAPI = Global.APIs.AvailableAPIs.GetIndex(apiIndex);
        }

        private void apiList_DoubleClick(object sender, EventArgs e)
        {
            int ind = apiList.SelectedIndex;
            if (ind < 0)
                return;

            if (apiName == apiList.Items[ind].ToString())
                return;
            else
            {
                if (MessageBox.Show("Are you sure you'd like to switch the API to " + apiList.Items[ind].ToString() + "?", "Current API: " + apiName, MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                {
                    DisconnectFromTarget("Disconnected. Cleared code backups.");
                    string[] parts = apiList.Items[ind].ToString().Split(new char[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
                    int apiDLL = Global.APIs.AvailableAPIs.GetIndex(parts[0].Trim(), parts[1]);
                    curAPI = Global.APIs.AvailableAPIs.GetIndex(apiDLL);
                    curAPI.Instance.Initialize();

                    SaveOptions();
                }
            }
        }

        private void apiIcon_MouseLeave(object sender, EventArgs e)
        {
            int ind = apiList.SelectedIndex;
            if (ind < 0)
                return;

            if (selAPI == null)
                return;

            if (selAPI.Instance.ContactLink != null && selAPI.Instance.ContactLink != "")
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private void apiIcon_Click(object sender, EventArgs e)
        {
            int ind = apiList.SelectedIndex;
            if (ind < 0)
                return;

            if (selAPI == null)
                return;

            if (selAPI.Instance.ContactLink != null && selAPI.Instance.ContactLink != "")
            {
                System.Diagnostics.Process.Start(selAPI.Instance.ContactLink);
            }
        }

        private void apiIcon_MouseEnter(object sender, EventArgs e)
        {
            int ind = apiList.SelectedIndex;
            if (ind < 0)
                return;

            if (selAPI == null)
                return;

            if (selAPI.Instance.ContactLink != null && selAPI.Instance.ContactLink != "")
            {
                Cursor.Current = Cursors.Hand;
            }
        }

        private void apiIcon_MouseMove(object sender, MouseEventArgs e)
        {
            if (selAPI == null)
                return;

            if (selAPI.Instance.ContactLink != null && selAPI.Instance.ContactLink != "")
            {
                Cursor.Current = Cursors.Hand;
            }
        }

        private void apiIcon_MouseHover(object sender, EventArgs e)
        {
            if (selAPI == null)
                return;

            if (selAPI.Instance.ContactLink != null && selAPI.Instance.ContactLink != "")
            {
                Cursor.Current = Cursors.Hand;
            }
        }

        private void configureAPIToolStripMenuItem_Click(object sender, EventArgs e)
        {
            curAPI.Instance.Configure();
        }
    }
}
