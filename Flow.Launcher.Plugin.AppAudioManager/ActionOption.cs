using System;
using System.Collections.Generic;

namespace Flow.Launcher.Plugin.AppAudioManager
{
    public class ActionOption
    {
        private readonly Func<ActionOption, Result> _getResult;
        private Result _cachedResult;
        public List<string> Names;
        public string SubActionKeyword;
        public List<ActionOption> SubActions;

        public ActionOption ParentAction;

        public ActionOption(
            List<string> names,
            Func<ActionOption, Result> getResult,
            string subActionKeyword = null,
            List<ActionOption> subActions = null,
            ActionOption parentAction = null
        ){
            Names = names;
            _getResult = getResult;
            _cachedResult = null;

            SubActionKeyword = subActionKeyword;
            SubActions = subActions;
            
            if (SubActions != null){foreach (var subAction in SubActions)
            {
                subAction.ParentAction = this;
            }}

            ParentAction = parentAction;
        }

        public Result getResult()
        {
            if (_cachedResult == null)
            {
                _cachedResult = _getResult(this);
            }
            return _cachedResult;
        }

        public bool GoToSubActions(PluginInitContext context, string currentQuery)
        {
            context.API.ChangeQuery(
                $"{context.CurrentPluginMetadata.ActionKeyword} {currentQuery}{SubActionKeyword} "
            );
            return false;
        }
    }
}