using System;
using System.Diagnostics;
using Microsoft.VisualBasic.Devices;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace Flow.Launcher.Plugin.AppAudioManager
{
    public class AudioSessionWrapper
    {
        private AudioSessionControl _session;
        public string Name { get; }
        public int ProcessId { get;}
        public string IconPath { get; }
        public AudioSessionState State
        {
            get { return _session.State; }
        }
        public float Volume
        {
            get { return _session.SimpleAudioVolume.Volume; }
            set {
                // clamp value between 0.0 and 1.0
                if (value < 0.0f) value = 0.0f;
                if (value > 1.0f) value = 1.0f;
                
                _session.SimpleAudioVolume.Volume = value; 
            }
        }

        public bool IsMuted
        {
            get { return _session.SimpleAudioVolume.Mute; }
            set { _session.SimpleAudioVolume.Mute = value; }
        }

        public AudioSessionWrapper(AudioSessionControl session)
        {
            _session = session;

            ProcessId = (int)_session.GetProcessID;

            // First check if this is the system sounds
            if (
                !string.IsNullOrEmpty(session.DisplayName) 
                && session.DisplayName.StartsWith("@%SystemRoot%\\System32\\AudioSrv.Dll")
            ){
                Name = "System Sounds";
                IconPath = "";
                return;
            }

            // Get process associated with the audio session
            Process sessionProcess = null;
            try {
                sessionProcess = Process.GetProcessById(ProcessId);
            }   catch (Exception ex) when (
                ex is ArgumentException or InvalidOperationException) { /* Ignore */ };

            // If the process is a WebView2 process, go up the parent chain to find the actual host process
            var parentProcess = sessionProcess;
            while (parentProcess is not null && parentProcess.ProcessName == "msedgewebview2")
            {
                var parentProcessId = ProcessHelper.GetParentProcessId(parentProcess.Id);
                if (parentProcessId == -1)
                {
                    break;
                }

                if (parentProcess != sessionProcess) parentProcess.Dispose();

                try {    
                    parentProcess = Process.GetProcessById(parentProcessId);
                }   catch (Exception ex) when (
                    ex is ArgumentException or InvalidOperationException)
                {
                    parentProcess = null;
                }
            }

            // For Name and Icon, Use the parent process if found, otherwise use the session process
            var referenceProcess = sessionProcess;
            if (parentProcess is not null)
            {
                referenceProcess = parentProcess;
            }
            Name = GetBestName(_session, referenceProcess);
            IconPath = GetIconPath(_session, referenceProcess);

            if (sessionProcess is not null) sessionProcess.Dispose();
            if (parentProcess is not null) parentProcess.Dispose();
        }

        private static string GetBestName(AudioSessionControl session, Process process)
        {
            if (!string.IsNullOrEmpty(session.DisplayName))
            {
                return session.DisplayName;
            }

            if (process != null)
            {
                try{if (!string.IsNullOrEmpty(process.MainWindowTitle))
                {
                    return process.MainWindowTitle;
                }}  catch (Exception ex) when (
                    ex is InvalidOperationException or NotSupportedException) { /* Ignore */ }

                try{if (!string.IsNullOrEmpty(process.ProcessName))
                {
                    return process.ProcessName;
                }}  catch (Exception ex) when (
                    ex is InvalidOperationException or NotSupportedException) {  /* Ignore */ }
            }

            return "Unknown";
        }
    
        private static string GetIconPath(AudioSessionControl session, Process process)
        {
            if (!string.IsNullOrEmpty(session.IconPath))
            {
                return session.IconPath;
            }

            if (process != null)
            {
                try{
                    return process.MainModule.FileName;
                } catch (Exception ex) when (
                    ex is InvalidOperationException or NotSupportedException or System.ComponentModel.Win32Exception) 
                    {  /* Ignore */ }
            }

            return string.Empty;
        }

        public void ToggleMute()
        {
            _session.SimpleAudioVolume.Mute = !_session.SimpleAudioVolume.Mute;
        }

        public void Dispose()
        {
            _session.Dispose();
        }
    }
}