using System;
using System.Collections.Generic;

namespace Flow.Launcher.Plugin.AppAudioManager
{
    public record ActionOption(
        List<string> Names,
        Func<Result> CreateResult,
        string SubOptionKeyword = null,
        Func<List<ActionOption>> GetSubOptions = null
    );
}