using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using anti_veille.Models;
using anti_veille.Utilities;
using System.Windows.Input;
using MessageBox = System.Windows.MessageBox;

namespace anti_veille.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private VideoService videoService;
        private DispatcherTimer idleTimer;
        private int sleepTimeout;
        private string sleepCountdown;
        private ImageSource cameraImage;
        private int detectionSensitivity;

        public event PropertyChangedEventHandler PropertyChanged;

        public MainViewModel()
        {
            // Récupération du délai de mise en veille via PowerHelper (en secondes)
            sleepTimeout = PowerHelper.GetSleepTimeoutAC();
            if (sleepTimeout <= 0)
                sleepTimeout = 600; // Exemple : 600 secondes (10 minutes)

            SleepCountdown = $"{sleepTimeout} s avant la mise en veille";

            // Charger la sensibilité depuis le registre (si inexistante, on utilise 2 par défaut)
            detectionSensitivity = AppSettingsHelper.GetDetectionSensitivity();
            if (detectionSensitivity < 1)
                detectionSensitivity = 2;

            // Instanciation du service vidéo et affectation de la sensibilité
            videoService = new VideoService();
            videoService.DetectionSensitivity = detectionSensitivity;
            videoService.FrameReady += VideoService_FrameReady;
            videoService.Start();

            // Timer pour surveiller l’inactivité (toutes les secondes)
            idleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            idleTimer.Tick += IdleTimer_Tick;
            idleTimer.Start();
        }

        private void VideoService_FrameReady(object sender, FrameEventArgs e)
        {
            // Lorsque la caméra détecte un visage, elle met à jour son LastFaceDetected.
            // Nous mettons ici à jour l'image affichée.
            CameraImage = e.Frame;
        }

        private void IdleTimer_Tick(object sender, EventArgs e)
        {
            // Obtenir l'heure du dernier événement d'entrée utilisateur.
            DateTime lastUserInput = UserActivityMonitor.GetLastInputTime();
            // Comparer avec le dernier instant de détection par caméra.
            DateTime lastActivity = videoService.LastFaceDetected > lastUserInput ? videoService.LastFaceDetected : lastUserInput;
            double idleSeconds = (DateTime.Now - lastActivity).TotalSeconds;
            int remaining = sleepTimeout - (int)idleSeconds;
            SleepCountdown = $"{remaining} s avant la mise en veille";

            if (idleSeconds >= sleepTimeout)
            {
                idleTimer.Stop();
                videoService.Stop();

                try
                {
                    // Déclenche la mise en veille via PowerHelper.
                    PowerHelper.TriggerSleep();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    MessageBox.Show(ex.Message);
                }
            }
        }

        public ImageSource CameraImage
        {
            get => cameraImage;
            set
            {
                if (cameraImage != value)
                {
                    cameraImage = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SleepCountdown
        {
            get => sleepCountdown;
            set
            {
                if (sleepCountdown != value)
                {
                    sleepCountdown = value;
                    OnPropertyChanged();
                }
            }
        }

        public int DetectionSensitivity
        {
            get => detectionSensitivity;
            set
            {
                if (detectionSensitivity != value)
                {
                    detectionSensitivity = value;
                    OnPropertyChanged();
                    if (videoService != null)
                        videoService.DetectionSensitivity = detectionSensitivity;
                    AppSettingsHelper.SaveDetectionSensitivity(detectionSensitivity);
                }
            }
        }

        // Commandes optionnelles pour pause/resume de la caméra
        private ICommand pauseCameraCommand;
        public ICommand PauseCameraCommand => pauseCameraCommand ?? (pauseCameraCommand = new RelayCommand(param => PauseCamera(), param => true));

        private ICommand resumeCameraCommand;
        public ICommand ResumeCameraCommand => resumeCameraCommand ?? (resumeCameraCommand = new RelayCommand(param => ResumeCamera(), param => true));

        public void PauseCamera()
        {
            videoService.PauseCamera();
        }

        public void ResumeCamera()
        {
            videoService.ResumeCamera();
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            idleTimer.Tick -= IdleTimer_Tick;
            idleTimer?.Stop();
            videoService.FrameReady -= VideoService_FrameReady;
            videoService?.Dispose();
        }
    }
}
