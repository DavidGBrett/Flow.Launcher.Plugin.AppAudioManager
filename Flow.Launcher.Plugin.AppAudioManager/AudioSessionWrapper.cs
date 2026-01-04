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
            set { _session.SimpleAudioVolume.Volume = value; }
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

            Process process = null;
            try {
                process = Process.GetProcessById(ProcessId);
            }   catch (Exception ex) when (
                ex is ArgumentException or InvalidOperationException) { /* Ignore */ };

            Name = GetBestName(_session, process);
            IconPath = GetIconPath(_session, process);

            if (process is not null) process.Dispose();
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