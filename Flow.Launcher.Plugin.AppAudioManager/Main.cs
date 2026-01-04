using System;
using System.Collections.Generic;
using Flow.Launcher.Plugin;
using NAudio.CoreAudioApi;
using System.Diagnostics;
using System.Windows.Forms.Design.Behavior;
using Microsoft.VisualBasic.Devices;
using NAudio.CoreAudioApi.Interfaces;

namespace Flow.Launcher.Plugin.AppAudioManager
{
    public class AppAudioManager : IPlugin, IContextMenu
    {
        private PluginInitContext _context;

        private MMDeviceEnumerator deviceEnumerator;

        private AudioSessionWrapper selectedSession;

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
                    SubTitle = $"Current mute status: {session.IsMuted}",
                    Action = _ =>
                    {
                        session.ToggleMute();

                        _context.API.ReQuery();
                        return true;
                    }
                });

                results.Add(new Result
                {
                    Title = "Increase Volume by 5%",
                    Glyph = new GlyphInfo("sans-serif", "+"),
                    SubTitle = $"Current volume: {Math.Round(session.Volume * 100)}%",
                    Action = _ =>
                    {
                        session.Volume += 0.05f;

                        _context.API.ReQuery();
                        return true;
                    }
                });

                results.Add(new Result
                {
                    Title = "Decrease Volume by 5%",
                    Glyph = new GlyphInfo("sans-serif", "-"),
                    SubTitle = $"Current volume: {Math.Round(session.Volume * 100)}%",
                    Action = _ =>
                    {
                        session.Volume -= 0.05f;

                        _context.API.ReQuery();
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
                    
                    var sessionInfo = new AudioSessionWrapper(sessions[i]);

                    // skip sessions without a name matching the search query
                    if (!string.IsNullOrEmpty(query.Search) &&
                        !sessionInfo.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string sessionState = sessionInfo.State switch
                    {
                        AudioSessionState.AudioSessionStateActive => "Active",
                        AudioSessionState.AudioSessionStateInactive => "Inactive",
                        AudioSessionState.AudioSessionStateExpired => "Expired",
                        _ => "Unknown"
                    };

                    // Prioritize audio sessions that are playing audio, ie in the Active state
                    var score = 0;
                    if (sessionInfo.State == AudioSessionState.AudioSessionStateActive)
                    {
                        score = 50;
                    }

                    results.Add(new Result{
                        Title = sessionInfo.Name,
                        SubTitle = $"{sessionState} | Volume: {Math.Round(sessionInfo.Volume * 100)}% | Muted: {sessionInfo.IsMuted}",
                        IcoPath = sessionInfo.IconPath,
                        ContextData = sessionInfo,
                        Score = score,
                        Action = _ =>
                        {
                            selectedSession = sessionInfo;
                            _context.API.ChangeQuery($"{_context.CurrentPluginMetadata.ActionKeyword} {sessionInfo.Name} >");
                            return false;
                        }
                    });
                }
            }

            return results;
        }

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            var results = new List<Result>();
            var session = (AudioSessionWrapper)selectedResult.ContextData;

            results.Add( new Result
            {
                Title = "Copy Name to Clipboard",
                SubTitle = session.Name,
                Glyph = new GlyphInfo("sans-serif", "N"),
                Action = _ =>
                {
                    _context.API.CopyToClipboard(session.Name);
                    return true;
                }
            });

            results.Add( new Result
            {
                Title = "Copy Process ID to Clipboard",
                SubTitle = session.ProcessId.ToString(),
                Glyph = new GlyphInfo("sans-serif", "ID"),
                Action = _ =>
                {
                    _context.API.CopyToClipboard(session.ProcessId.ToString());
                    return true;
                }
            });

            results.Add( new Result
            {
                Title = "Copy Icon File Path to Clipboard",
                SubTitle = session.IconPath,
                IcoPath = session.IconPath,
                Action = _ =>
                {
                    _context.API.CopyToClipboard(session.IconPath);
                    return true;
                }
            });

            return results;
        }
    }
}