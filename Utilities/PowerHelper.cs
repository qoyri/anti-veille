using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class PowerHelperAPI
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool LockWorkStation();
    
    // P/Invoke pour appeler la fonction native qui met en veille
    [DllImport("powrprof.dll", SetLastError = true)]
    private static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);
    
    // Import de PowerGetActiveScheme pour obtenir le schéma de puissance actif
    [DllImport("powrprof.dll", SetLastError = true)]
    private static extern uint PowerGetActiveScheme(IntPtr UserRootPowerKey, out IntPtr ActivePolicyGuid);

    // Import de PowerReadACValue pour lire la valeur AC
    [DllImport("powrprof.dll", SetLastError = true)]
    private static extern uint PowerReadACValue(
        IntPtr RootPowerKey,
        ref Guid SchemeGuid,
        ref Guid SubGroupOfPowerSettingsGuid,
        ref Guid PowerSettingGuid,
        out uint Type,
        IntPtr Buffer,
        ref uint BufferSize);

    // Import de PowerReadDCValue pour lire la valeur DC
    [DllImport("powrprof.dll", SetLastError = true)]
    private static extern uint PowerReadDCValue(
        IntPtr RootPowerKey,
        ref Guid SchemeGuid,
        ref Guid SubGroupOfPowerSettingsGuid,
        ref Guid PowerSettingGuid,
        out uint Type,
        IntPtr Buffer,
        ref uint BufferSize);

    // Import de LocalFree pour libérer la mémoire allouée
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LocalFree(IntPtr hMem);

    // GUID du sous-groupe "Sleep" (veille)
    private static Guid GUID_SLEEP_SUBGROUP = new Guid("238c9fa8-0aad-41ed-83f4-97be242c8f20");
    // GUID du paramètre "Sleep after" (veille après)
    private static Guid GUID_SLEEP_TIMEOUT = new Guid("29f6c1db-86da-48c5-9fdb-f2b67b1f44da");

    /// <summary>
    /// Obtient le GUID du schéma de puissance actif.
    /// </summary>
    public static Guid GetActiveSchemeGuid()
    {
        IntPtr pActiveScheme;
        uint res = PowerGetActiveScheme(IntPtr.Zero, out pActiveScheme);
        if (res != 0)
        {
            throw new Win32Exception((int)res, "Erreur lors de la récupération du schéma actif.");
        }
        Guid activeScheme = (Guid)Marshal.PtrToStructure(pActiveScheme, typeof(Guid));
        LocalFree(pActiveScheme);
        return activeScheme;
    }

    /// <summary>
    /// Lit la valeur du paramètre "Sleep after" en mode AC.
    /// </summary>
    public static int GetSleepTimeoutAC()
    {
        Guid schemeGuid = GetActiveSchemeGuid();
        return ReadValue(schemeGuid, useAC: true);
    }

    /// <summary>
    /// Lit la valeur du paramètre "Sleep after" en mode DC.
    /// </summary>
    public static int GetSleepTimeoutDC()
    {
        Guid schemeGuid = GetActiveSchemeGuid();
        return ReadValue(schemeGuid, useAC: false);
    }

    private static int ReadValue(Guid schemeGuid, bool useAC)
    {
        uint type;
        // Allouer directement un buffer de 4 octets, puisque la valeur est un DWORD.
        uint bufferSize = (uint)Marshal.SizeOf(typeof(uint)); // 4 octets
        IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);
        uint res;

        if (useAC)
        {
            res = PowerReadACValue(IntPtr.Zero, ref schemeGuid, ref GUID_SLEEP_SUBGROUP, ref GUID_SLEEP_TIMEOUT,
                                   out type, buffer, ref bufferSize);
        }
        else
        {
            res = PowerReadDCValue(IntPtr.Zero, ref schemeGuid, ref GUID_SLEEP_SUBGROUP, ref GUID_SLEEP_TIMEOUT,
                                   out type, buffer, ref bufferSize);
        }
        if (res != 0)
        {
            Marshal.FreeHGlobal(buffer);
            throw new Win32Exception((int)res, "Erreur lors de la lecture de la valeur de puissance.");
        }
        int value = Marshal.ReadInt32(buffer);
        Marshal.FreeHGlobal(buffer);
        return value;
    }
    
    /// <summary>
    /// Verrouille la session Windows.
    /// </summary>
    public static bool TriggerLock()
    {
        bool result = LockWorkStation();
        if (!result)
        {
            int errorCode = Marshal.GetLastWin32Error();
            Debug.WriteLine("LockWorkStation failed with error: " + errorCode);
            throw new Win32Exception(errorCode, "Échec du verrouillage de la session.");
        }
        return result;
    }
    
    /// <summary>
    /// Déclenche la mise en veille ou l'hibernation.
    /// Attention : cela mettra l'ordinateur en veille/hibernation !
    /// </summary>
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