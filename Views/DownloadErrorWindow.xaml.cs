using System.Windows;

namespace anti_veille.Views
{
    public partial class DownloadErrorWindow : Window
    {
        public DownloadErrorWindow(string targetFolder)
        {
            InitializeComponent();
            // Formate le message d'erreur en indiquant à l'utilisateur où copier le fichier.
            InstructionTextBlock.Text = string.Format(
                "Le téléchargement du fichier cascade a échoué.\n" +
                "Veuillez le télécharger manuellement à partir de l'URL suivante :\n" +
                "https://ftp.qoyri.fr/?dir=%2Fmnt%2Fcode&file=haarcascade_frontalface_default.xml\n\n" +
                "Ensuite, copiez-le dans le dossier :\n{0}", targetFolder);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}