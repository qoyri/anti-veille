using System;
using System.Runtime.InteropServices;

namespace anti_veille.Utilities
{
    public static class UserActivityMonitor
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        /// <summary>
        /// Renvoie l’heure du dernier événement d’entrée utilisateur.
        /// </summary>
        public static DateTime GetLastInputTime()
        {
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            if (!GetLastInputInfo(ref lastInputInfo))
            {
                // En cas d'erreur, on retourne l'heure actuelle.
                return DateTime.Now;
            }
            // Calcul du temps inactif en millisecondes.
            uint idleMilliseconds = (uint)Environment.TickCount - lastInputInfo.dwTime;
            return DateTime.Now.AddMilliseconds(-idleMilliseconds);
        }
    }
}