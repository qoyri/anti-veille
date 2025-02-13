using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Win32;
using Application = System.Windows.Application;
using anti_veille.ViewModels;
using anti_veille.Utilities;
using MessageBox = System.Windows.MessageBox;

namespace anti_veille
{
    public partial class App : Application
    {
        private NotifyIcon _notifyIcon;
        private ToolStripMenuItem _pauseResumeItem; // Dynamic menu item

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Subscribe to power mode changes to handle resume from sleep.
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
            SystemEvents.SessionSwitch += OnSessionSwitch;

            /*// Verify the application location and move if necessary.
            try
            {
                // Use Process.GetCurrentProcess().MainModule.FileName to get the full path of the exe.
                string currentExePath = Process.GetCurrentProcess().MainModule.FileName;
                // Target folder: LocalApplicationData\AntiVeille
                string targetFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AntiVeille");
                string targetExePath = Path.Combine(targetFolder, Path.GetFileName(currentExePath));

                // If the app isn't already in the target folder, copy it there and restart.
                if (!string.Equals(currentExePath, targetExePath, StringComparison.OrdinalIgnoreCase))
                {
                    if (!Directory.Exists(targetFolder))
                    {
                        Directory.CreateDirectory(targetFolder);
                    }
                    // Copy the exe (extend to copy other necessary files if needed)
                    File.Copy(currentExePath, targetExePath, true);

                    // Start the copied executable
                    Process.Start(targetExePath);

                    // Shut down the current instance
                    Shutdown();
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error during automatic move of the application: " + ex.Message +
                    "\nPlease manually copy the app to: " +
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AntiVeille"),
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            // Create the startup shortcut so the app launches on Windows startup.
            StartupShortcutHelper.CreateStartupShortcut();*/

            // Create the tray icon
            var greenIcon = (Icon)anti_veille.Properties.Resources.ResourceManager.GetObject("GreenIcon")!;
            _notifyIcon = new NotifyIcon
            {
                Visible = true,
                Icon = greenIcon,
                Text = "anti veille"
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Debug", null, Debug_Click);

            // Create Pause/Resume menu item.
            _pauseResumeItem = new ToolStripMenuItem("Pause", null, Pause_Click);
            contextMenu.Items.Add(_pauseResumeItem);

            contextMenu.Items.Add("Quit", null, Quit_Click);
            _notifyIcon.ContextMenuStrip = contextMenu;

            // Create and hide the main window.
            MainWindow = new MainWindow();
            MainWindow.Hide();
        }

        /// <summary>
        /// Handles power mode changes.
        /// When resuming from sleep, resumes the camera.
        /// </summary>
        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (MainWindow is MainWindow mainWindow &&
                        mainWindow.DataContext is MainViewModel viewModel)
                    {
                        // Resume camera and reset timers as needed.
                        viewModel.ResumeCamera();
                        // (Optionally, you could also restart any idle timers here.)
                    }
                });
            }
        }

        /// <summary>
        /// Gère les changements de session (verrouillage/déverrouillage).
        /// Lors du déverrouillage, on redémarre le service vidéo et le timer.
        /// </summary>
        private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (MainWindow is MainWindow mainWindow &&
                        mainWindow.DataContext is MainViewModel viewModel)
                    {
                        // Au déverrouillage, on force la réinitialisation de la dernière activité.
                        viewModel.ResumeCamera();
                    }
                });
            }
        }


        private void Pause_Click(object sender, EventArgs e)
        {
            if (MainWindow is MainWindow mainWindow &&
                mainWindow.DataContext is MainViewModel viewModel)
            {
                viewModel.PauseCamera();
            }

            UpdateTrayIconColor("pause");
            _pauseResumeItem.Text = "Resume";
            _pauseResumeItem.Click -= Pause_Click;
            _pauseResumeItem.Click += Resume_Click;
        }

        private void Resume_Click(object sender, EventArgs e)
        {
            if (MainWindow is MainWindow mainWindow &&
                mainWindow.DataContext is MainViewModel viewModel)
            {
                viewModel.ResumeCamera();
            }

            UpdateTrayIconColor("ok");
            _pauseResumeItem.Text = "Pause";
            _pauseResumeItem.Click -= Resume_Click;
            _pauseResumeItem.Click += Pause_Click;
        }

        private void Debug_Click(object sender, EventArgs e)
        {
            if (MainWindow == null)
            {
                MainWindow = new MainWindow();
            }

            MainWindow.Show();
            MainWindow.Activate();
        }

        private void Quit_Click(object sender, EventArgs e)
        {
            _notifyIcon.Visible = false;
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Désabonnement des événements système.
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            SystemEvents.SessionSwitch -= OnSessionSwitch;
            _notifyIcon?.Dispose();
            base.OnExit(e);
        }

        public void UpdateTrayIconColor(string state)
        {
            var greenIcon = (Icon)anti_veille.Properties.Resources.ResourceManager.GetObject("GreenIcon")!;
            var orangeIcon = (Icon)anti_veille.Properties.Resources.ResourceManager.GetObject("OrangeIcon")!;
            var redIcon = (Icon)anti_veille.Properties.Resources.ResourceManager.GetObject("RedIcon")!;

            switch (state)
            {
                case "error":
                    _notifyIcon.Icon = redIcon;
                    _notifyIcon.Text = "Error or camera unavailable";
                    break;
                case "pause":
                    _notifyIcon.Icon = orangeIcon;
                    _notifyIcon.Text = "Paused";
                    break;
                case "ok":
                    _notifyIcon.Icon = greenIcon;
                    _notifyIcon.Text = "Everything is running correctly";
                    break;
                default:
                    break;
            }
        }
    }
}
