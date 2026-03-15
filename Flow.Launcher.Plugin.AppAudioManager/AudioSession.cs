using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Xml.Linq;
using Microsoft.VisualBasic.Devices;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using Windows.Management.Deployment;

namespace Flow.Launcher.Plugin.AppAudioManager
{
    public class AudioSession
    {
        private AudioSessionControl _session;
        public string Name { get; }
        public int ProcessId { get;}
        public string? ProcessFilePath { get; }
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

        public AudioSession(AudioSessionControl session, string name, int processId, string? processFilePath, string iconPath)
        {
            _session = session;
            Name = name;
            ProcessId = processId;
            ProcessFilePath = processFilePath;
            IconPath = iconPath;
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