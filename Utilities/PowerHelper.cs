using System;
using System.Runtime.InteropServices;

namespace anti_veille.Utilities
{
    public class PowerHelper
    {
        [DllImport("powrprof.dll", SetLastError = true)]
        private static extern uint PowerGetActiveScheme(IntPtr UserRootPowerKey, out IntPtr ActivePolicyGuid);

        [DllImport("powrprof.dll", SetLastError = true)]
        private static extern uint PowerReadACValue(
            IntPtr RootPowerKey,
            ref Guid SchemeGuid,
            ref Guid SubGroupOfPowerSettingsGuid,
            ref Guid PowerSettingGuid,
            out uint Type,
            out IntPtr Buffer,
            out uint BufferSize);

        [DllImport("powrprof.dll", SetLastError = true)]
        private static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LocalFree(IntPtr hMem);

        private static Guid GUID_SLEEP_SUBGROUP = new Guid("94AC6D29-73CE-41A6-8098-4F07B43F3B3E");
        private static Guid GUID_SLEEP_TIMEOUT = new Guid("29f6c1db-86da-48c5-9fdb-f2b67b1f44da");

        public static int GetSleepTimeoutAC()
        {
            IntPtr pActiveScheme;
            uint res = PowerGetActiveScheme(IntPtr.Zero, out pActiveScheme);
            if (res != 0)
                return -1;

            Guid activeScheme = (Guid)Marshal.PtrToStructure(pActiveScheme, typeof(Guid));

            uint type;
            IntPtr buffer;
            uint bufferSize;
            res = PowerReadACValue(IntPtr.Zero, ref activeScheme, ref GUID_SLEEP_SUBGROUP, ref GUID_SLEEP_TIMEOUT,
                out type, out buffer, out bufferSize);
            if (res != 0)
                return -1;

            int timeout = Marshal.ReadInt32(buffer);
            LocalFree(buffer);
            return timeout;
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
}
