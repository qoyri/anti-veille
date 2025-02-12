using Microsoft.Win32;

namespace anti_veille.Utilities
{
    public static class AppSettingsHelper
    {
        // Chemin dans le registre (sous HKCU)
        private const string RegistryPath = @"Software\anti_veille";
        private const string SensitivityKey = "DetectionSensitivity";

        /// <summary>
        /// Récupère la valeur de la sensibilité depuis le registre.
        /// Si elle n'existe pas, renvoie 2 par défaut.
        /// </summary>
        public static int GetDetectionSensitivity()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryPath))
                {
                    if (key != null)
                    {
                        object value = key.GetValue(SensitivityKey);
                        if (value != null && int.TryParse(value.ToString(), out int result))
                        {
                            return result;
                        }
                    }
                }
            }
            catch
            {
                // En cas d'erreur, on retourne la valeur par défaut
            }
            return 2; // valeur par défaut
        }

        /// <summary>
        /// Sauvegarde la valeur de la sensibilité dans le registre.
        /// </summary>
        public static void SaveDetectionSensitivity(int sensitivity)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryPath))
                {
                    key.SetValue(SensitivityKey, sensitivity, RegistryValueKind.DWord);
                }
            }
            catch
            {
                // Vous pouvez gérer ou logger l'exception ici si besoin.
            }
        }
    }
}