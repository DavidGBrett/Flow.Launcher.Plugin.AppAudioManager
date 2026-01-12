using System;
using System.Runtime.InteropServices;
using static Flow.Launcher.Plugin.AppAudioManager.ProcessInterop;

namespace Flow.Launcher.Plugin.AppAudioManager
{
    public class ProcessHelper
    {
        public static int GetParentProcessId(int processId)
        {
            IntPtr processHandle = IntPtr.Zero;
            try
            {
                // Open the process with query permissions
                processHandle = OpenProcess(
                    PROCESS_QUERY_INFORMATION | PROCESS_VM_READ,
                    false, (uint)processId);

                if (processHandle == IntPtr.Zero)
                {
                    // Access denied or process doesn't exist
                    return -1;
                }

                // Query for parent process ID
                var pbi = new PROCESS_BASIC_INFORMATION();
                int status = NtQueryInformationProcess(
                    processHandle, 0, // ProcessBasicInformation
                    ref pbi, Marshal.SizeOf(pbi), out _);

                if (status != 0) return -1; // Failed
                
                return pbi.InheritedFromUniqueProcessId.ToInt32();
            }
            finally
            {
                if (processHandle != IntPtr.Zero)
                    CloseHandle(processHandle);
            }
        }
    }
}