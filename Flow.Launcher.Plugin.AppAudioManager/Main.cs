using System;
using System.Collections.Generic;
using Flow.Launcher.Plugin;
using NAudio.CoreAudioApi;
using System.Diagnostics;

namespace Flow.Launcher.Plugin.AppAudioManager
{
    public class AppAudioManager : IPlugin
    {
        private PluginInitContext _context;

        private MMDeviceEnumerator deviceEnumerator;

        public void Init(PluginInitContext context)
        {
            _context = context;

            deviceEnumerator = new MMDeviceEnumerator();

        }

        public List<Result> Query(Query query)
        {
            var results = new List<Result>();

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

                    results.Add(new Result{
                        Title = session.DisplayName,
                        SubTitle = $"{processId} - {state} - {processName} - {mainWindowTitle}",
                        IcoPath = session.IconPath,
                        Action = _ =>
                        {
                            // Toggle mute
                            session.SimpleAudioVolume.Mute = !session.SimpleAudioVolume.Mute;
                            return true;
                        }
                    });
                }
            }

            return results;
        }
    }
}