using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace anti_veille.Utilities
{
    public static class StartupShortcutHelper
    {
        // Déclaration de l'interface COM IShellLinkW
        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        private interface IShellLinkW
        {
            void GetPath([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out WIN32_FIND_DATAW pfd, uint fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
            void Resolve(IntPtr hwnd, uint fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        // Structure WIN32_FIND_DATAW
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WIN32_FIND_DATAW
        {
            public uint dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        // Déclaration de l'interface COM IPersistFile
        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("0000010B-0000-0000-C000-000000000046")]
        private interface IPersistFile
        {
            void GetClassID(out Guid pClassID);
            void IsDirty();
            void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
            void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, bool fRemember);
            void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
            void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
        }

        // Classe ShellLink
        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        private class ShellLink { }

        /// <summary>
        /// Crée un raccourci dans le dossier Startup de l'utilisateur.
        /// </summary>
        public static void CreateStartupShortcut()
        {
            try
            {
                string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                string shortcutPath = Path.Combine(startupFolder, "AntiVeille.lnk");
                // Utilise Process.GetCurrentProcess().MainModule.FileName pour obtenir le chemin complet de l'exécutable
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                string workingDirectory = Path.GetDirectoryName(exePath);

                // Crée une instance de ShellLink
                IShellLinkW shellLink = (IShellLinkW)new ShellLink();
                shellLink.SetPath(exePath);
                shellLink.SetWorkingDirectory(workingDirectory);
                shellLink.SetDescription("Lance AntiVeille au démarrage de Windows");
                shellLink.SetShowCmd(1); // SW_SHOWNORMAL

                // Sauvegarde le raccourci via IPersistFile
                IPersistFile persistFile = (IPersistFile)shellLink;
                persistFile.Save(shortcutPath, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de la création du raccourci de démarrage : " + ex.Message,
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void RemoveStartupShortcut()
        {
            try
            {
                string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                string shortcutPath = Path.Combine(startupFolder, "AntiVeille.lnk");
                if (File.Exists(shortcutPath))
                    File.Delete(shortcutPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de la suppression du raccourci de démarrage : " + ex.Message,
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
