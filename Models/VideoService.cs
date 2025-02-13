using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System.Windows;
using System.Windows.Media.Imaging;
using Application = System.Windows.Application;
using Rect = OpenCvSharp.Rect;
using Timer = System.Timers.Timer;

namespace anti_veille.Models
{
    /// <summary>
    /// Arguments d’événement contenant l’image traitée.
    /// </summary>
    public class FrameEventArgs : EventArgs
    {
        public BitmapSource Frame { get; }
        public FrameEventArgs(BitmapSource frame)
        {
            Frame = frame;
        }
    }

    /// <summary>
    /// Service de capture et de traitement vidéo.
    /// </summary>
    public class VideoService : IDisposable
    {
        private VideoCapture capture;
        private CascadeClassifier faceCascade;
        private CascadeClassifier profileCascade;
        private Timer videoTimer;            // Pour le traitement des frames (~15 FPS)
        private Timer cameraAccessCheckTimer; // Pour vérifier la disponibilité de la caméra
        private bool isProcessingFrame = false;
        private readonly object captureLock = new object();
        private bool isCameraInUseByOtherApp = false;
        private bool isCameraPaused = false;

        /// <summary>
        /// Instant de la dernière détection de visage.
        /// </summary>
        public DateTime LastFaceDetected { get; set; } = DateTime.Now;

        /// <summary>
        /// Se déclenche à chaque fois qu’une frame est traitée.
        /// </summary>
        public event EventHandler<FrameEventArgs> FrameReady;

        // Propriété de sensibilité utilisée pour la détection.
        private int detectionSensitivity = 2;
        public int DetectionSensitivity
        {
            get => detectionSensitivity;
            set => detectionSensitivity = value;
        }

       /// <summary>
        /// Le constructeur n'initialise pas la caméra ni le classifieur pour éviter de bloquer le thread UI.
        /// Ceux‑ci seront initialisés dans Start().
        /// </summary>
        public VideoService()
        {
            // Nous déplaçons le téléchargement/chargement du cascade hors du constructeur.
            videoTimer = new Timer(66);
            videoTimer.Elapsed += async (s, e) => await ProcessVideoFrameAsync();
            cameraAccessCheckTimer = new Timer(2000);
            cameraAccessCheckTimer.Elapsed += CameraAccessCheckTimer_Elapsed;
        }

        /// <summary>
        /// Vérifie si le fichier cascade existe dans LocalApplicationData\AntiVeille.
        /// S'il n'existe pas, tente de le télécharger depuis l'URL.
        /// En cas d'erreur de téléchargement, affiche une fenêtre pop‑up pour informer l'utilisateur.
        /// Renvoie le chemin complet du fichier cascade.
        /// </summary>
        private async Task<string> EnsureCascadeFileAsync(string fileName, string url)
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AntiVeille");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            string filePath = Path.Combine(folder, fileName);
            if (!File.Exists(filePath))
            {
                using (HttpClient client = new HttpClient())
                {
                    try
                    {
                        Console.WriteLine("Téléchargement de " + fileName + "...");
                        HttpResponseMessage response = await client.GetAsync(url);
                        response.EnsureSuccessStatusCode();
                        byte[] content = await response.Content.ReadAsByteArrayAsync();
                        File.WriteAllBytes(filePath, content);
                        Console.WriteLine("Téléchargement terminé.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Erreur lors du téléchargement de " + fileName + " : " + ex.Message);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var errorWindow = new anti_veille.Views.DownloadErrorWindow(folder);
                            errorWindow.ShowDialog();
                        });
                        throw new Exception("Le téléchargement de " + fileName + " a échoué. Veuillez le télécharger manuellement.", ex);
                    }
                }
            }
            return filePath;
        }



        /// <summary>
        /// Tente d'ouvrir la caméra de façon asynchrone.
        /// En cas d'échec, l'icône est mise en rouge et la méthode réessaie toutes les 15 secondes.
        /// Ne retourne que lorsque la caméra a été ouverte avec succès.
        /// </summary>
        private async Task EnsureCaptureAsync()
        {
            while (true)
            {
                bool success = false;
                try
                {
                    var tempCapture = new VideoCapture(0, VideoCaptureAPIs.DSHOW);
                    tempCapture.Set(VideoCaptureProperties.FrameWidth, 320);
                    tempCapture.Set(VideoCaptureProperties.FrameHeight, 240);
                    if (!tempCapture.IsOpened())
                    {
                        tempCapture.Release();
                        tempCapture.Dispose();
                        throw new Exception("La caméra n'est pas ouverte.");
                    }
                    lock (captureLock)
                    {
                        capture = tempCapture;
                    }
                    success = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Erreur lors de l'initialisation de la caméra : " + ex.Message);
                }

                if (success)
                {
                    // La caméra est disponible : met à jour l'icône sur "ok"
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (Application.Current is App currentApp)
                        {
                            currentApp.UpdateTrayIconColor("ok");
                        }
                    });
                    break;
                }
                else
                {
                    // La caméra n'est pas accessible : met à jour l'icône en rouge et attend 15 secondes avant de réessayer.
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (Application.Current is App currentApp)
                        {
                            currentApp.UpdateTrayIconColor("error");
                        }
                    });
                    await Task.Delay(TimeSpan.FromSeconds(15));
                }
            }
        }

        /// <summary>
        /// Démarre les timers.
        /// </summary>
        private void StartTimers()
        {
            try { videoTimer.Start(); } catch (Exception ex) { Console.WriteLine("Erreur au démarrage du timer vidéo : " + ex.Message); }
            try { cameraAccessCheckTimer.Start(); } catch (Exception ex) { Console.WriteLine("Erreur au démarrage du timer de vérification de la caméra : " + ex.Message); }
        }

        /// <summary>
        /// Démarre le service.
        /// Vérifie d'abord que le fichier de cascade est présent et charge le classifieur,
        /// puis tente d'ouvrir la caméra et démarre les timers.
        /// </summary>
        public async void Start()
        {
            // Vérifie et télécharge le fichier cascade frontal si nécessaire.
            string frontalCascadePath = await EnsureCascadeFileAsync("haarcascade_frontalface_default.xml", "https://ftp.qoyri.fr/?dir=%2Fmnt%2Fcode&file=haarcascade_frontalface_default.xml");
            try
            {
                faceCascade = new CascadeClassifier(frontalCascadePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur lors du chargement du cascade frontal : " + ex.Message);
                throw;
            }
    
            // Vérifie et télécharge le fichier cascade pour le profil (similaire à frontal)
            string profileCascadePath = await EnsureCascadeFileAsync("haarcascade_profileface.xml", "https://ftp.qoyri.fr/?dir=%2Fmnt%2Fcode&file=haarcascade_profileface.xml");
            try
            {
                profileCascade = new CascadeClassifier(profileCascadePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur lors du chargement du cascade de profil : " + ex.Message);
                throw;
            }
    
            LastFaceDetected = DateTime.Now;
            await EnsureCaptureAsync();
            StartTimers();
        }


        public void Stop()
        {
            try { videoTimer?.Stop(); } catch (Exception ex) { Console.WriteLine("Erreur lors de l'arrêt du timer vidéo : " + ex.Message); }
            try { cameraAccessCheckTimer?.Stop(); } catch (Exception ex) { Console.WriteLine("Erreur lors de l'arrêt du timer de vérification de la caméra : " + ex.Message); }
        }

        public void PauseCamera()
        {
            if (!isCameraPaused)
            {
                Stop();
                lock (captureLock)
                {
                    try
                    {
                        if (capture != null)
                        {
                            capture.Release();
                            capture.Dispose();
                            capture = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Erreur lors de la libération de la caméra : " + ex.Message);
                    }
                }
                isCameraPaused = true;
            }
        }

        public async void ResumeCamera()
        {
            bool needRestart = false;
            lock (captureLock)
            {
                if (capture == null || !capture.IsOpened())
                {
                    needRestart = true;
                }
            }
            if (needRestart)
            {
                bool success = false;
                try
                {
                    var tempCapture = new VideoCapture(0, VideoCaptureAPIs.DSHOW);
                    tempCapture.Set(VideoCaptureProperties.FrameWidth, 320);
                    tempCapture.Set(VideoCaptureProperties.FrameHeight, 240);
                    
                    if (tempCapture.IsOpened())
                    {
                        lock (captureLock)
                        {
                            if (capture != null)
                            {
                                capture.Release();
                                capture.Dispose();
                            }
                            capture = tempCapture;
                        }
                        success = true;
                    }
                    else
                    {
                        tempCapture.Release();
                        tempCapture.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Erreur lors de la tentative d'ouverture de la caméra dans ResumeCamera : " + ex.Message);
                }

                if (success)
                {
                    StartTimers();
                    isCameraPaused = false;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (Application.Current is App currentApp)
                        {
                            currentApp.UpdateTrayIconColor("ok");
                        }
                    });
                    // Réinitialiser la dernière détection
                    LastFaceDetected = DateTime.Now;
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (Application.Current is App currentApp)
                        {
                            currentApp.UpdateTrayIconColor("error");
                        }
                    });
                    await Task.Delay(TimeSpan.FromSeconds(15));
                    ResumeCamera();
                }
            }
            else
            {
                // Si la caméra est déjà active, réinitialiser simplement le timer interne
                LastFaceDetected = DateTime.Now;
            }
        }


        private void CameraAccessCheckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                lock (captureLock)
                {
                    if (capture == null || !capture.IsOpened())
                    {
                        if (!isCameraInUseByOtherApp)
                        {
                            Console.WriteLine("La caméra n'est plus accessible !");
                            isCameraInUseByOtherApp = true;
                        }
                    }
                    else
                    {
                        if (isCameraInUseByOtherApp)
                        {
                            Console.WriteLine("La caméra est de nouveau accessible.");
                            isCameraInUseByOtherApp = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la vérification de la caméra : {ex.Message}");
                isCameraInUseByOtherApp = true;
            }
        }

        private async Task ProcessVideoFrameAsync()
        {
            lock (captureLock)
            {
                if (capture == null || !capture.IsOpened())
                {
                    isProcessingFrame = false;
                    return;
                }
            }
            if (isProcessingFrame)
                return;
            isProcessingFrame = true;
            Mat frame = null;
            try
            {
                frame = new Mat();
                lock (captureLock)
                {
                    if (capture != null)
                        capture.Read(frame);
                }
                if (frame.Empty())
                {
                    frame.Dispose();
                    return;
                }
                Mat processedFrame = await Task.Run(() =>
                {
                    using (Mat processingFrame = frame.Clone())
                    {
                        int size = Math.Min(processingFrame.Width, processingFrame.Height);
                        var roi = new Rect((processingFrame.Width - size) / 2, (processingFrame.Height - size) / 2, size, size);
                        using (Mat croppedFrame = new Mat(processingFrame, roi))
                        using (Mat gray = new Mat())
                        {
                            Cv2.CvtColor(croppedFrame, gray, ColorConversionCodes.BGR2GRAY);
                            using (Mat resizedGray = new Mat())
                            {
                                Cv2.Resize(gray, resizedGray, new OpenCvSharp.Size(gray.Width / 3, gray.Height / 3));
                                
                                // Détection frontale
                                Rect[] frontalFaces = faceCascade.DetectMultiScale(
                                    resizedGray,
                                    scaleFactor: 1.2,
                                    minNeighbors: detectionSensitivity,
                                    flags: HaarDetectionTypes.ScaleImage,
                                    minSize: new OpenCvSharp.Size(30, 30));
                                
                                // Détection de profil
                                Rect[] profileFaces = profileCascade.DetectMultiScale(
                                    resizedGray,
                                    scaleFactor: 1.2,
                                    minNeighbors: detectionSensitivity,
                                    flags: HaarDetectionTypes.ScaleImage,
                                    minSize: new OpenCvSharp.Size(30, 30));
                                
                                // Si l'un ou l'autre détecte un visage, mettre à jour LastFaceDetected.
                                if (frontalFaces.Length > 0 || profileFaces.Length > 0)
                                {
                                    LastFaceDetected = DateTime.Now;
                                }
                                
                                // Dessiner les rectangles pour le frontal
                                foreach (Rect face in frontalFaces)
                                {
                                    Rect scaledFace = new Rect(
                                        face.X * 3 + roi.X,
                                        face.Y * 3 + roi.Y,
                                        face.Width * 3,
                                        face.Height * 3);
                                    Cv2.Rectangle(processingFrame, scaledFace, Scalar.Red, 2);
                                }
                                // Dessiner les rectangles pour le profil
                                foreach (Rect face in profileFaces)
                                {
                                    Rect scaledFace = new Rect(
                                        face.X * 3 + roi.X,
                                        face.Y * 3 + roi.Y,
                                        face.Width * 3,
                                        face.Height * 3);
                                    Cv2.Rectangle(processingFrame, scaledFace, Scalar.Blue, 2);
                                }
                            }
                        }
                        return processingFrame.Clone();
                    }
                });
                frame.Dispose();
                frame = null;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        var bitmap = BitmapSourceConverter.ToBitmapSource(processedFrame);
                        FrameReady?.Invoke(this, new FrameEventArgs(bitmap));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Erreur lors de la conversion de la frame sur le thread UI : " + ex.Message);
                    }
                });
                processedFrame.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur lors du traitement de la frame : " + ex.Message);
            }
            finally
            {
                if (frame != null)
                {
                    frame.Dispose();
                    frame = null;
                }
                isProcessingFrame = false;
            }
        }


        public void Dispose()
        {
            Stop();
            videoTimer?.Dispose();
            cameraAccessCheckTimer?.Dispose();
            lock (captureLock)
            {
                try
                {
                    if (capture != null)
                    {
                        capture.Release();
                        capture.Dispose();
                        capture = null;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Erreur lors de la libération de la caméra dans Dispose : " + ex.Message);
                }
            }
            try
            {
                faceCascade?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur lors de la libération du classifieur : " + ex.Message);
            }
        }
    }
}
