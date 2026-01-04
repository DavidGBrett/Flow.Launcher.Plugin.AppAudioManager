using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using NAudio.CoreAudioApi;

namespace Flow.Launcher.Plugin.AppAudioManager
{
    public class ProcessHelper
    {
        // Windows API declarations
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(
            uint processAccess, bool bInheritHandle, uint processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(
            IntPtr processHandle, int processInformationClass,
            ref PROCESS_BASIC_INFORMATION processInformation,
            int processInformationLength, out int returnLength);

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_BASIC_INFORMATION
        {
            public IntPtr Reserved1;
            public IntPtr PebBaseAddress;
            public IntPtr Reserved2_0;
            public IntPtr Reserved2_1;
            public IntPtr UniqueProcessId;
            public IntPtr InheritedFromUniqueProcessId; // Parent PID
        }

        // Process access rights
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const uint PROCESS_VM_READ = 0x0010;

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