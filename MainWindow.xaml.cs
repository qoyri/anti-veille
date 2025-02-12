using System.Windows;
using anti_veille.ViewModels;

namespace anti_veille
{
    public partial class MainWindow : Window
    {
        private MainViewModel viewModel;

        /// <summary>
        /// Indique si la fenêtre peut réellement se fermer.
        /// Par défaut, on la masque.
        /// </summary>
        public bool AllowClose { get; set; } = false;

        public MainWindow()
        {
            InitializeComponent();
            viewModel = new MainViewModel();
            DataContext = viewModel;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Si la fermeture n'est pas autorisée (fermeture par la croix),
            // on annule l'opération et on masque la fenêtre.
            if (!AllowClose)
            {
                e.Cancel = true;
                this.Hide();
            }
            else
            {
                // Pour une fermeture définitive, on peut disposer le ViewModel
                viewModel.Dispose();
                base.OnClosing(e);
            }
        }
    }
}