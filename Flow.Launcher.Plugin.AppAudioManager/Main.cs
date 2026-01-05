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

                results = getActions(
                    queryString: query.Search,
                    session: session
                );

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

        public float ParseVolumeQuery(string queryString, string keyword, float defaultVolume=0.05f)
        {
            // split the query at the keyword to isolate the volume part
            string[] parts = queryString.Split(new[] { keyword }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length <= 1)  return defaultVolume;

            // Try to parse the volume part after the keyword
            bool sucessfulParse = float.TryParse(
                parts[1].Trim().TrimEnd('%'), out float parsedVolume);

            if (! sucessfulParse) return defaultVolume;

            return parsedVolume / 100f;
        }

        public List<Result> getActions(string queryString, AudioSessionWrapper session)
        {
            var results = new List<Result>();

            var actions = new List<(
                List<string> Names,
                Func<Result> GetResult,
                string SubActionKeyword,
                Func<List<Result>> GetSubActions
            )>(){
                (
                    Names: new List<string>{ "Increase Volume", "+"},
                    GetResult: ()=>new Result
                    {
                        Title = "Increase Volume",
                        Glyph = new GlyphInfo("sans-serif", "+"),
                        SubTitle = $"Current volume: {Math.Round(session.Volume * 100)}%",
                        Action = _ =>
                        {
                            _context.API.ChangeQuery(
                                $"{_context.CurrentPluginMetadata.ActionKeyword} {session.Name} > vol+ ");
                            return false;
                        }
                    },
                    SubActionKeyword: " vol+ ",
                    GetSubActions: () => {
                        float increaseAmount = ParseVolumeQuery(
                            queryString: queryString,
                            keyword: "vol+",
                            defaultVolume: 0.05f
                        );

                        results.Add(new Result
                        {
                            Title = $"Increase Volume by {Math.Round(increaseAmount * 100)}%",
                            Glyph = new GlyphInfo("sans-serif", "+"),
                            SubTitle = $"Current volume: {Math.Round(session.Volume * 100)}%",
                            Action = _ =>
                            {
                                session.Volume += increaseAmount;

                                _context.API.ReQuery();
                                return true;
                            }
                        });

                        return results;
                    }
                )
            };

            if (queryString.Contains("vol+"))
            {
                // Extract the desired volume increase from the query
                string[] parts = queryString.Split(new[] { "vol+" }, StringSplitOptions.RemoveEmptyEntries);
                
                float increaseAmount = ParseVolumeQuery(
                    queryString: queryString,
                    keyword: "vol+",
                    defaultVolume: 0.05f
                );

                results.Add(new Result
                {
                    Title = $"Increase Volume by {Math.Round(increaseAmount * 100)}%",
                    Glyph = new GlyphInfo("sans-serif", "+"),
                    SubTitle = $"Current volume: {Math.Round(session.Volume * 100)}%",
                    Action = _ =>
                    {
                        session.Volume += increaseAmount;

                        _context.API.ReQuery();
                        return true;
                    }
                });

                return results;
            } else if (queryString.Contains("vol-"))
            {
                // Extract the desired volume decrease from the query
                float decreaseAmount = ParseVolumeQuery(
                    queryString: queryString,
                    keyword: "vol-",
                    defaultVolume: 0.05f
                );

                results.Add(new Result
                {
                    Title = $"Decrease Volume by {Math.Round(decreaseAmount * 100)}%",
                    Glyph = new GlyphInfo("sans-serif", "-"),
                    SubTitle = $"Current volume: {Math.Round(session.Volume * 100)}%",
                    Action = _ =>
                    {
                        session.Volume -= decreaseAmount;

                        _context.API.ReQuery();
                        return true;
                    }
                });

                return results;
            } else if (queryString.Contains("vol="))
            {
                // Extract the desired volume level from the query
                float setVolume = ParseVolumeQuery(
                    queryString: queryString,
                    keyword: "vol=",
                    defaultVolume: 0.5f
                );

                results.Add(new Result
                {
                    Title = $"Set Volume to {Math.Round(setVolume * 100)}%",
                    Glyph = new GlyphInfo("sans-serif", "="),
                    SubTitle = $"Current volume: {Math.Round(session.Volume * 100)}%",
                    Action = _ =>
                    {
                        session.Volume = setVolume;

                        _context.API.ReQuery();
                        return true;
                    }
                });

                return results;
            }

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
                Title = "Increase Volume",
                Glyph = new GlyphInfo("sans-serif", "+"),
                SubTitle = $"Current volume: {Math.Round(session.Volume * 100)}%",
                Action = _ =>
                {
                    _context.API.ChangeQuery(
                        $"{_context.CurrentPluginMetadata.ActionKeyword} {session.Name} > vol+ ");
                    return false;
                }
            });

            results.Add(new Result
            {
                Title = "Decrease Volume",
                Glyph = new GlyphInfo("sans-serif", "-"),
                SubTitle = $"Current volume: {Math.Round(session.Volume * 100)}%",
                Action = _ =>
                {
                    _context.API.ChangeQuery(
                        $"{_context.CurrentPluginMetadata.ActionKeyword} {session.Name} > vol- ");
                    return false;
                }
            });

            results.Add(new Result
            {
                Title = "Set Volume",
                Glyph = new GlyphInfo("sans-serif", "="),
                SubTitle = $"Current volume: {Math.Round(session.Volume * 100)}%",
                Action = _ =>
                {
                    _context.API.ChangeQuery(
                        $"{_context.CurrentPluginMetadata.ActionKeyword} {session.Name} > vol= ");
                    return false;
                }
            });
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