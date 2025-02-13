using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

public class PowerHelper
{
    // P/Invoke pour lire la valeur d'une option de puissance (non utilisée ici)
    [DllImport("powrprof.dll", SetLastError = true)]
    private static extern uint PowerGetActiveScheme(IntPtr UserRootPowerKey, out IntPtr ActivePolicyGuid);
    private static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);
    private static extern uint PowerReadACValue(
        IntPtr RootPowerKey,
        ref Guid SchemeGuid,
        ref Guid SubGroupOfPowerSettingsGuid,
        ref Guid PowerSettingGuid,
        out uint Type,
        out IntPtr Buffer,
        out uint BufferSize);

    // Pour libérer la mémoire allouée
    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr hMem);

    // GUID du sous-groupe "Sleep"
    private static Guid GUID_SLEEP_SUBGROUP = new Guid("94AC6D29-73CE-41A6-8098-4F07B43F3B3E");

    // GUID de la valeur "Sleep Timeout"
    private static Guid GUID_SLEEP_TIMEOUT = new Guid("29f6c1db-86da-48c5-9fdb-f2b67b1f44da");

    /// <summary>
    /// Surcharge permettant de récupérer le délai de mise en veille configuré en mode AC sans fournir le GUID.
    /// </summary>
    public static int GetSleepTimeoutAC()
    {
        string schemeGuid = GetActiveSchemeGuidFromPowercfg();
        if (string.IsNullOrEmpty(schemeGuid))
        {
            Console.WriteLine("Impossible de récupérer le GUID du schéma actif.");
            return -1;
        }

        return GetSleepTimeoutACSheme(schemeGuid);
    }

    /// <summary>
    /// Récupère le délai de mise en veille configuré en mode AC (en secondes) à partir du GUID du schéma d'alimentation.
    /// Retourne -1 en cas d'erreur.
    /// </summary>
    public static int GetSleepTimeoutACSheme(string schemeGuid)
    {
        // Préparation de la commande : on utilise /QUERY pour interroger le plan spécifié
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "powercfg",
            Arguments = $"/QUERY {schemeGuid}",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process process = Process.Start(psi);
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        // Recherche de la section concernant "Sleep after" (identifiée par son GUID)
        Regex regex = new Regex(
            @"Power Setting GUID:\s*29f6c1db-86da-48c5-9fdb-f2b67b1f44da\s*\([^)]*\).*?Current AC Power Setting Index:\s*0x([0-9a-fA-F]+)",
            RegexOptions.Singleline);
        Match match = regex.Match(output);

        if (match.Success)
        {
            string hexValue = match.Groups[1].Value;
            if (int.TryParse(hexValue, System.Globalization.NumberStyles.HexNumber, null, out int timeout))
            {
                return timeout;
            }
        }

        return -1;
    }

    /// <summary>
    /// Récupère automatiquement le GUID du schéma d'alimentation actif en utilisant la commande powercfg /GETACTIVESCHEME.
    /// </summary>
    private static string GetActiveSchemeGuidFromPowercfg()
    {
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "powercfg",
            Arguments = "/GETACTIVESCHEME",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process process = Process.Start(psi);
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        // Exemple de sortie : "Power Scheme GUID: 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c  (High performance)"
        Regex regex = new Regex(@"Power Scheme GUID:\s*([0-9a-fA-F\-]+)");
        Match match = regex.Match(output);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        return null;
    }

    public static string GetLastErrorMessage(uint errorCode)
    {
        return new System.ComponentModel.Win32Exception((int)errorCode).Message;
    }
    
    public static bool TriggerSleep(bool hibernate = false, bool forceCritical = true, bool disableWakeEvent = false)
    {
        bool result = SetSuspendState(hibernate, forceCritical, disableWakeEvent);
        if (!result)
        {
            int errorCode = Marshal.GetLastWin32Error();
            throw new Exception($"Échec de la mise en veille. Code d'erreur : {errorCode}");
        }
        return result;
    }
}