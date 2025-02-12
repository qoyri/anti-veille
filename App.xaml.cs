using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Reflection;
using anti_veille.Utilities;
using Application = System.Windows.Application;
using anti_veille.ViewModels;
using MessageBox = System.Windows.MessageBox; // Pour accéder au MainViewModel

namespace anti_veille
{
    public partial class App : Application
    {
        private NotifyIcon _notifyIcon;
        private ToolStripMenuItem _pauseResumeItem; // Élément de menu dynamique

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Vérification de l'emplacement de l'application
            try
            {
                // Utiliser Process.GetCurrentProcess().MainModule.FileName pour obtenir le chemin complet de l'exécutable
                string currentExePath = Process.GetCurrentProcess().MainModule.FileName;
                // Dossier cible : LocalApplicationData\AntiVeille
                string targetFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AntiVeille");
                string targetExePath = Path.Combine(targetFolder, Path.GetFileName(currentExePath));

                // Si l'application ne se trouve pas déjà dans le dossier cible, on la copie et on redémarre
                if (!string.Equals(currentExePath, targetExePath, StringComparison.OrdinalIgnoreCase))
                {
                    if (!Directory.Exists(targetFolder))
                    {
                        Directory.CreateDirectory(targetFolder);
                    }
                    // Copier l'exécutable (vous pouvez étendre pour copier d'autres fichiers si nécessaire)
                    File.Copy(currentExePath, targetExePath, true);
                    
                    // Lance l'exécutable dans le dossier cible
                    Process.Start(targetExePath);
                    
                    // Ferme l'instance actuelle
                    Shutdown();
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du déplacement automatique de l'application : " + ex.Message +
                    "\nVeuillez copier manuellement l'application dans le dossier : " +
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AntiVeille"),
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            // Création du raccourci de démarrage
            StartupShortcutHelper.CreateStartupShortcut();

            // Création de l'icône de notification
            var greenIcon = (Icon)anti_veille.Properties.Resources.ResourceManager.GetObject("GreenIcon")!;
            _notifyIcon = new NotifyIcon
            {
                Visible = true,
                Icon = greenIcon,
                Text = "anti veille"
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Debug", null, Debug_Click);

            // Création de l'élément Pause/Resume
            _pauseResumeItem = new ToolStripMenuItem("Pause", null, Pause_Click);
            contextMenu.Items.Add(_pauseResumeItem);

            contextMenu.Items.Add("Quit", null, Quit_Click);
            _notifyIcon.ContextMenuStrip = contextMenu;

            // Création et masquage de la fenêtre principale
            MainWindow = new MainWindow();
            MainWindow.Hide();
        }


        private void Pause_Click(object sender, EventArgs e)
        {
            // Appeler la méthode qui met en pause l'accès à la caméra via le ViewModel
            if (MainWindow is MainWindow mainWindow &&
                mainWindow.DataContext is MainViewModel viewModel)
            {
                viewModel.PauseCamera();
            }

            // Mise à jour de l'icône (ici en orange)
            UpdateTrayIconColor("pause");

            // Modification de l'élément de menu pour permettre la reprise
            _pauseResumeItem.Text = "Resume";
            _pauseResumeItem.Click -= Pause_Click;
            _pauseResumeItem.Click += Resume_Click;
        }

        private void Resume_Click(object sender, EventArgs e)
        {
            // Appeler la méthode qui reprend l'accès à la caméra via le ViewModel
            if (MainWindow is MainWindow mainWindow &&
                mainWindow.DataContext is MainViewModel viewModel)
            {
                viewModel.ResumeCamera();
            }

            // Mise à jour de l'icône (ici en rouge, indiquant que tout fonctionne)
            UpdateTrayIconColor("ok");

            // Remise à jour de l'élément de menu en "Pause"
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
                    _notifyIcon.Text = "Erreur ou caméra indisponible";
                    break;
                case "pause":
                    _notifyIcon.Icon = orangeIcon;
                    _notifyIcon.Text = "En pause";
                    break;
                case "ok":
                    _notifyIcon.Icon = greenIcon;
                    _notifyIcon.Text = "Tout fonctionne correctement";
                    break;
                default:
                    break;
            }
        }
    }
}
