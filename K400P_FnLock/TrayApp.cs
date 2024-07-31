
using HidApi;
using K400P_FnLock_Background.Properties;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;

namespace K400P_FnLock_Background
{
    internal class TrayApp : ApplicationContext
    {
        private UserSettings appSettings = new();
        private NotifyIcon trayIcon;
        private BackgroundWorker worker = new BackgroundWorker
        {
            WorkerSupportsCancellation = true
        };
        string appname = "K400+ Fn Lock";

        public TrayApp()
        {
            trayIcon = new NotifyIcon
            {
                Icon = SetIcon(),
                Visible = true,
                ContextMenuStrip = Initialize(),
                Text = appname
            };

            trayIcon.Click += new(UpdateUI);

            this.ThreadExit += (sender, e) => { trayIcon.Dispose(); };

            worker.DoWork += BackgroundWorkerOnDoWork;

            if (appSettings.FnLockEnabled)
                ToggleRunning();

            if (appSettings.RunOnStartup) 
                ApplyStartupSettings();
        }

        private void BackgroundWorkerOnDoWork(object? sender, DoWorkEventArgs e)
        {
            var worker = sender as BackgroundWorker;
            while (!worker.CancellationPending)
            {
                var k400p_seq_fkeys_on = new byte[] { 0x10, 0x01, 0x09, 0x19, 0x00, 0x00, 0x00 };
                var k400p_seq_fkeys_off = new byte[] { 0x10, 0x01, 0x09, 0x18, 0x01, 0x00, 0x00 };
                var k400p_vid = (ushort)0x46d;
                var k400p_pid = (ushort)0xc52b;
                string currentreport;

                Device? targetDeviceSendFnLock = null, targetDeviceReceiveWakeUpSignal = null;

                try
                {
                    while ((targetDeviceSendFnLock == null || targetDeviceReceiveWakeUpSignal == null) && !worker.CancellationPending)
                    {
                        Debug.WriteLine("searching for devices...");
                        var HidDeviceList = Hid.Enumerate(k400p_vid, k400p_pid);

                        foreach (DeviceInfo device in HidDeviceList)
                        {
                            if (device.Usage == 1 && device.UsagePage == 0xff00) 
                                targetDeviceSendFnLock = device.ConnectToDevice();

                            if (device.Usage == 2 && device.UsagePage == 0xff00) 
                                targetDeviceReceiveWakeUpSignal = device.ConnectToDevice();
                        }
                        Thread.Sleep(1000);
                    }

                    if (!worker.CancellationPending)
                    {
                        targetDeviceSendFnLock.Write(k400p_seq_fkeys_on);
                        Debug.WriteLine("sending initial keys...");
                    }

                    while (!worker.CancellationPending)
                    {
                        Debug.WriteLine("waiting for wakeup report...");
                        currentreport = String.Join("", targetDeviceReceiveWakeUpSignal.ReadTimeout(8, 1000).ToArray());

                        if (currentreport.Equals("171401110"))
                        {
                            targetDeviceSendFnLock.Write(k400p_seq_fkeys_on);
                            Debug.WriteLine("received wakeup report, sending keys...");
                        }
                    }

                    if (worker.CancellationPending && targetDeviceSendFnLock != null)
                    {
                        Debug.WriteLine("canceling fnlock");
                        targetDeviceSendFnLock.Write(k400p_seq_fkeys_off);
                        break;
                    };
                }
                catch (HidException)
                {
                    //don't worry about it
                }
            }
        }

        private ContextMenuStrip Initialize()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.RenderMode = ToolStripRenderMode.System;
            ToolStripMenuItem item;

            // Status
            item = new ToolStripMenuItem();
            item.Text = SetStatus();
            item.Enabled = false;
            item.Name = "status";
            menu.Items.Add(item);

            menu.Items.Add(new ToolStripSeparator());

            // Start toggle
            item = new ToolStripMenuItem();
            item.Text = SetToggleText();
            item.Name = "run_toggle";
            item.Click += new(ToggleEnabled);
            menu.Items.Add(item);

            // Run On Startup
            item = new ToolStripMenuItem();
            item.Checked = appSettings.RunOnStartup;
            item.CheckOnClick = true;
            item.Text = "Run on Startup";
            item.Name = "startup_toggle";
            item.Click += new(RunOnStartupClick);
            menu.Items.Add(item);

            menu.Items.Add(new ToolStripSeparator());

            // Website
            item = new ToolStripMenuItem();
            item.Text = "Website";
            item.Click += new(WebsiteClick);
            menu.Items.Add(item);

            menu.Items.Add(new ToolStripSeparator());

            // Exit
            item = new ToolStripMenuItem();
            item.Text = "Exit";
            item.Click += new(Exit_Click);
            menu.Items.Add(item);

            menu.Closed += new(UpdateUI);

            return menu;
        }

        private void Menu_Opening(object? sender, CancelEventArgs e)
        {
            throw new NotImplementedException();
        }

        private string SetStatus()
        {
                
            if (worker.IsBusy) return "Fn Lock Enabled";
            else 
                return "Fn Lock Disabled";
        }

        private string SetToggleText()
        {
            if (appSettings.FnLockEnabled) 
                return "Disable";
            else 
                return "Enable";
        }

        private Icon? SetIcon()
        {
            if (appSettings.FnLockEnabled) 
                return Resources.fnon;
            else 
                return Resources.fnoff;
        }

        private void UpdateUI(object? sender, EventArgs e)
        {
            var target = trayIcon.ContextMenuStrip;
            target.Items.Find("status", true).First().Text = SetStatus();
            target.Items.Find("run_toggle", true).First().Text = SetToggleText();
            trayIcon.Icon = SetIcon();
        }

        private void WebsiteClick(object? sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/radu-gs/K400P-FnLock-Background") { UseShellExecute = true });
        }

        void RunOnStartupClick(object? sender, EventArgs e)
        {
            appSettings.RunOnStartup = (sender as ToolStripMenuItem).Checked;
            ApplyStartupSettings();
            appSettings.Save();
        }

        void ApplyStartupSettings()
        {
            try
            {
                RegistryKey registryKeyk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true) 
                    ?? throw new UnauthorizedAccessException(message: "Couldn't access the registry, you might not have the necessary permissions.");

                if (appSettings.RunOnStartup)
                {
                    registryKeyk.SetValue(appname, Application.ExecutablePath);
                }
                else
                    if (registryKeyk.GetValue(appname) != null) 
                        registryKeyk.DeleteValue(appname);
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"{ex.Message}");
                MessageBox.Show($"{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                appSettings.RunOnStartup = false;
                (trayIcon.ContextMenuStrip.Items.Find("startup_toggle", true).First() as ToolStripMenuItem).Checked = false;
                appSettings.Save();
            }
        }

        void ToggleEnabled(object? sender, EventArgs e)
        {
            appSettings.FnLockEnabled = !appSettings.FnLockEnabled;
            appSettings.Save();
            ToggleRunning();
        }

        private void ToggleRunning()
        {
            if (appSettings.FnLockEnabled)
                worker.RunWorkerAsync();
            else
                worker.CancelAsync();
            trayIcon.Icon = SetIcon();
        }

        void Exit_Click(object? sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}