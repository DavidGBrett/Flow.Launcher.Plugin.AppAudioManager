using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NAudio.CoreAudioApi.Interfaces;

namespace Flow.Launcher.Plugin.AppAudioManager
{
    public class AudioSessionGroup
    {
        public List<AudioSession> AudioSessions;

        public string Name;

        public string IconPath;

        public AudioSessionState State
        {
        get {
            bool allExpired = true;

            foreach (var session in AudioSessions)
            {
                // if any session is active then the group is
                if (session.State == AudioSessionState.AudioSessionStateActive)
                    return AudioSessionState.AudioSessionStateActive;

                // if a single session ins't expired then the group isnt
                if (session.State != AudioSessionState.AudioSessionStateExpired)
                    allExpired = false;
            }

            if (allExpired) return AudioSessionState.AudioSessionStateExpired;
            
            // in all other cases we treat the group as inactive
            return AudioSessionState.AudioSessionStateInactive;
            }
        }

        public bool IsMuted
        {
            get { 
                // group is muted only if all audio sessions are muted
                return AudioSessions.All(
                    (a)=>a.IsMuted
                ); 
            }
            set { 
                //set mute state of all audio sessions
                AudioSessions.ForEach(
                    (a)=>a.IsMuted=value
                );
            }
        }

        public void ToggleMute()
        {
            this.IsMuted = !this.IsMuted;
        }

        public void setVolume(float value)
        {
            AudioSessions.ForEach(
                    (a)=>a.Volume=value
                );
        }
        public void addVolume(float value)
        {
            AudioSessions.ForEach(
                    (a)=>a.Volume+=value
                );
        }

        public string GetVolumeString()
        {
            float minVolume = AudioSessions.Min(
                (a)=>a.Volume
            );
            double minVolumePercent = Math.Round(minVolume * 100);
            
            // if theres only one session than we already have the volume
            if (AudioSessions.Count == 1)
            {
                return $"{minVolumePercent}%";
            }

            // otherwise find the max and give a range
            float maxVolume = AudioSessions.Max(
                (a)=>a.Volume
            );
            double maxVolumePercent = Math.Round(maxVolume * 100);

            return $"{minVolumePercent} - {maxVolumePercent}%";

        }

        public AudioSessionGroup(AudioSession audioSession)
        : this(new List<AudioSession> { audioSession })
        {}

        public AudioSessionGroup(List<AudioSession> audioSessions)
        {
            if (audioSessions is null)
            {
                throw new ArgumentNullException(nameof(audioSessions));
            }
            if (audioSessions.Count == 0)
            {
                throw new ArgumentException($"{nameof(audioSessions)} cannot be an empty list");
            }

            this.AudioSessions = audioSessions;

            this.Name = audioSessions[0].Name;
            this.IconPath = audioSessions[0].IconPath;
        }
    }
}