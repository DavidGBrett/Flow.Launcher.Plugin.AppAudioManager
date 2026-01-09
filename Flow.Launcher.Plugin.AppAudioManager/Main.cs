using System;
using System.Collections.Generic;
using Flow.Launcher.Plugin;
using NAudio.CoreAudioApi;
using System.Diagnostics;
using System.Windows.Forms.Design.Behavior;
using Microsoft.VisualBasic.Devices;
using NAudio.CoreAudioApi.Interfaces;
using System.Configuration;
using System.Linq;

namespace Flow.Launcher.Plugin.AppAudioManager
{
    public class AppAudioManager : IPlugin, IContextMenu
    {
        private PluginInitContext _context;

        private MMDeviceEnumerator deviceEnumerator;

        private AudioSessionGroup selectedSession;

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

                var availableOptions = getOptions(
                    queryString: query.Search,
                    session: session
                );

                results = getCurrentOptions(
                    availableOptions:availableOptions,
                    queryString:query.Search
                );

                return results;
            }

            selectedSession = null;

            MMDeviceCollection audioDeviceEndpoints = deviceEnumerator.EnumerateAudioEndPoints(
                DataFlow.Render, // Output devices
                DeviceState.Active
            );

            Dictionary<string, AudioSessionGroup> audioSessionGroups = new Dictionary<string, AudioSessionGroup>();

            foreach (var device in audioDeviceEndpoints)
            {

                var sessions = device.AudioSessionManager.Sessions;

                for (int i = 0; i < sessions.Count; i++)
                {
                    
                    var sessionInfo = new AudioSession(sessions[i]);

                    // skip sessions without a name matching the search query
                    if (!string.IsNullOrEmpty(query.Search) &&
                        !sessionInfo.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    AudioSessionGroup sessionGroup;

                    if (audioSessionGroups.ContainsKey(sessionInfo.Name))
                    {
                        audioSessionGroups[sessionInfo.Name].AudioSessions.Add(sessionInfo);
                        continue;
                    }
                    else
                    {
                        sessionGroup = new AudioSessionGroup(sessionInfo);
                        audioSessionGroups.Add(sessionInfo.Name, sessionGroup);
                    }

                    

                    string sessionState = sessionGroup.State switch
                    {
                        AudioSessionState.AudioSessionStateActive => "Active",
                        AudioSessionState.AudioSessionStateInactive => "Inactive",
                        AudioSessionState.AudioSessionStateExpired => "Expired",
                        _ => "Unknown"
                    };

                    // Prioritize audio sessions that are playing audio, ie in the Active state
                    var score = 0;
                    if (sessionGroup.State == AudioSessionState.AudioSessionStateActive)
                    {
                        score = 50;
                    }

                    results.Add(new Result{
                        Title = sessionGroup.Name,
                        SubTitle = $"{sessionState} | Volume: {sessionGroup.GetVolumeString()} | Muted: {sessionGroup.IsMuted}",
                        IcoPath = sessionGroup.IconPath,
                        ContextData = sessionGroup,
                        Score = score,
                        Action = _ =>
                        {
                            selectedSession = sessionGroup;
                            _context.API.ChangeQuery($"{_context.CurrentPluginMetadata.ActionKeyword} {sessionGroup.Name} >");
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

        public List<ActionOption> getOptions(string queryString, AudioSessionGroup session)
        {
            var results = new List<Result>();

            List<ActionOption> actionOptions = new List<ActionOption>();
            
            // Toggle Mute Action
            actionOptions.Add(new ActionOption(
                Names: new List<string> { "Toggle Mute", "Unmute" },
                CreateResult: () => new Result
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
                }
            ));

            // Increase Volume Action
            actionOptions.Add(new ActionOption(
                Names: new List<string> { "Increase Volume", "+" },
                CreateResult: () => new Result
                {
                    Title = "Increase Volume",
                    Glyph = new GlyphInfo("sans-serif", "+"),
                    SubTitle = $"Current volume: {session.GetVolumeString()}",
                    Action = _ =>
                    {
                        _context.API.ChangeQuery($"{_context.CurrentPluginMetadata.ActionKeyword} {session.Name} > vol+ ");
                        return false;
                    }
                },
                SubOptionKeyword: " vol+",
                GetSubOptions: ()=>new List<ActionOption>(){new ActionOption(
                    Names: new List<string> {},
                    CreateResult: () =>
                    {
                        float increaseAmount = ParseVolumeQuery(
                            queryString: queryString,
                            keyword: " vol+",
                            defaultVolume: 0.05f
                        );

                        return new Result
                        {
                            Title = $"Increase Volume by {Math.Round(increaseAmount * 100)}%",
                            Glyph = new GlyphInfo("sans-serif", "+"),
                            SubTitle = $"Current volume: {session.GetVolumeString()}",
                            Action = _ =>
                            {
                                session.addVolume(increaseAmount);

                                _context.API.ReQuery();
                                return true;
                            }
                        };
                    })
                }
            ));

            // Decrease Volume Action
            actionOptions.Add(new ActionOption(
                Names: new List<string> { "Decrease Volume", "-" },
                CreateResult: () => new Result
                {
                    Title = "Decrease Volume",
                    Glyph = new GlyphInfo("sans-serif", "-"),
                    SubTitle = $"Current volume: {session.GetVolumeString()}",
                    Action = _ =>
                    {
                        _context.API.ChangeQuery($"{_context.CurrentPluginMetadata.ActionKeyword} {session.Name} > vol- ");
                        return false;
                    }
                },
                SubOptionKeyword: " vol-",
                GetSubOptions: ()=>new List<ActionOption>(){new ActionOption(
                    Names: new List<string> {},
                    CreateResult: () =>
                    {
                        float decreaseAmount = ParseVolumeQuery(
                            queryString: queryString,
                            keyword: " vol-",
                            defaultVolume: 0.05f
                        );

                        return new Result
                        {
                            Title = $"Decrease Volume by {Math.Round(decreaseAmount * 100)}%",
                            Glyph = new GlyphInfo("sans-serif", "-"),
                            SubTitle = $"Current volume: {session.GetVolumeString()}",
                            Action = _ =>
                            {
                                session.addVolume(-decreaseAmount);

                                _context.API.ReQuery();
                                return true;
                            }
                        };
                    })
                }
            ));

            // Set Specific Volume Action
            actionOptions.Add(new ActionOption(
                Names: new List<string> { "Set Volume", "=" },
                CreateResult: () => new Result
                {
                    Title = "Set Volume",
                    Glyph = new GlyphInfo("sans-serif", "="),
                    SubTitle = $"Current volume: {session.GetVolumeString()}",
                    Action = _ =>
                    {
                        _context.API.ChangeQuery($"{_context.CurrentPluginMetadata.ActionKeyword} {session.Name} > vol= ");
                        return false;
                    }
                },
                SubOptionKeyword: " vol=",
                GetSubOptions: ()=>new List<ActionOption>(){new ActionOption(
                    Names: new List<string> {},
                    CreateResult: () =>
                    {
                        float targetVolume = ParseVolumeQuery(
                            queryString: queryString,
                            keyword: " vol=",
                            defaultVolume: 0.5f
                        );

                        return new Result
                        {
                            Title = $"Set Volume to {Math.Round(targetVolume * 100)}%",
                            Glyph = new GlyphInfo("sans-serif", "="),
                            SubTitle = $"Current volume: {session.GetVolumeString()}",
                            Action = _ =>
                            {
                                session.setVolume(targetVolume);

                                _context.API.ReQuery();
                                return true;
                            }
                        };
                    })
                }
            ));
            
            return actionOptions;
        }

        public List<Result> getCurrentOptions(List<ActionOption> availableOptions, string queryString)
        {
            var results = new List<Result>();

            string optionFilter = queryString.Split(">")[1].Trim().ToLower();

            foreach (var actionOption in availableOptions)
            {
                // check if we match the suboptionkeyword for this option
                if (
                    !string.IsNullOrEmpty(actionOption.SubOptionKeyword) 
                    && queryString.Contains(actionOption.SubOptionKeyword)
                ){
                    // go through each suboption and get their results
                    var subActionResults = new List<Result>();
                    foreach (var subAction in actionOption.GetSubOptions())
                    {
                        subActionResults.Add(subAction.CreateResult());
                    }
                    return subActionResults;
                }

                // otherwise check if one of the options names matches the current search filter
                else if (actionOption.Names.Any((name)=>name.ToLower().Contains(optionFilter)))
                {
                    results.Add(actionOption.CreateResult());
                }
            }

            return results;
        }

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            var results = new List<Result>();
            var session = (AudioSessionGroup)selectedResult.ContextData;

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

            List<int> processIDs = session.AudioSessions.ConvertAll((a)=>a.ProcessId);
            string processIDString = string.Join(",",processIDs);
            string title = processIDs.Count == 1 ?
                "Copy Process ID to Clipboard"
                : "Copy Process IDs to Clipboard";

            results.Add( new Result
            {
                Title = title,
                SubTitle = processIDString,
                Glyph = new GlyphInfo("sans-serif", "ID"),
                Action = _ =>
                {
                    _context.API.CopyToClipboard(processIDString);
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