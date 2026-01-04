using System;
using System.Collections.Generic;
using Flow.Launcher.Plugin;
using NAudio.CoreAudioApi;
using System.Diagnostics;
using System.Windows.Forms.Design.Behavior;

namespace Flow.Launcher.Plugin.AppAudioManager
{
    public class AppAudioManager : IPlugin
    {
        private PluginInitContext _context;

        private MMDeviceEnumerator deviceEnumerator;

        private AudioSessionControl selectedSession;

        public void Init(PluginInitContext context)
        {
            _context = context;

            deviceEnumerator = new MMDeviceEnumerator();

            selectedSession = null;

        }

        public List<Result> Query(Query query)
        {
            var results = new List<Result>();

            if (query.Search.Contains(">"))
            {
                var session = selectedSession;

                results.Add(new Result
                {
                    Title = "Toggle Mute",
                    Glyph = new GlyphInfo("sans-serif", "🔇"),
                    SubTitle = $"Current mute status: {session.SimpleAudioVolume.Mute}",
                    Action = _ =>
                    {
                        // Toggle mute
                        session.SimpleAudioVolume.Mute = !session.SimpleAudioVolume.Mute;
                        return true;
                    }
                });

                return results;
            }

            selectedSession = null;

            MMDeviceCollection audioDeviceEndpoints = deviceEnumerator.EnumerateAudioEndPoints(
                DataFlow.Render, // Output devices
                DeviceState.Active
            );

            foreach (var device in audioDeviceEndpoints)
            {
                

                var sessions = device.AudioSessionManager.Sessions;

                for (int i = 0; i < sessions.Count; i++)
                {
                    AudioSessionControl session = sessions[i];


                    // Get session state: Inactive, Active, or Expired
                    var state = session.State;
                    
                    // Try to get process info
                    uint processId = session.GetProcessID;
                    string processName = "Unknown";
                    string mainWindowTitle = "";
                    try
                    {
                        var process = Process.GetProcessById((int)processId);
                        processName = process.ProcessName;
                        mainWindowTitle = process.MainWindowTitle;
                        
                    }
                    catch (ArgumentException)
                    {
                        // Process may have exited or PID is invalid
                    }

                    var bestName = !string.IsNullOrEmpty(session.DisplayName) ? session.DisplayName : processName;

                    results.Add(new Result{
                        Title = session.DisplayName,
                        SubTitle = $"{processId} - {processName} - {mainWindowTitle}",
                        IcoPath = session.IconPath,
                        Action = _ =>
                        {
                            // // Toggle mute
                            // session.SimpleAudioVolume.Mute = !session.SimpleAudioVolume.Mute;
                            // return true;

                            selectedSession = session;
                            _context.API.ChangeQuery($"{_context.CurrentPluginMetadata.ActionKeyword} {bestName} >");
                            return false;
                        }
                    });
                }
            }

            return results;
        }
    }
}