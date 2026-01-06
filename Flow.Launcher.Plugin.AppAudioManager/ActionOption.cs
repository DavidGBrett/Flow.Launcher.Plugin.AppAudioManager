using System;
using System.Collections.Generic;

namespace Flow.Launcher.Plugin.AppAudioManager
{
    public record ActionOption(
        Func<Result> CreateResult,
        List<string> Names = null,
        string SubOptionKeyword = null,
        Func<List<ActionOption>> GetSubOptions = null
    ){
        public class Builder
        {
            private Func<Result> createResult;
            private List<string> names;
            private string subOptionKeyword;
            private Func<List<ActionOption>> getSubOptions;

            public Builder(Func<Result> CreateResult)
            {
                createResult = CreateResult;
            }

            public Builder WithNames(List<string> names)
            {
                this.names = names;
                return this;
            }
            public Builder WithSubOptions(string subOptionKeyword, Func<List<ActionOption>> getSubOptions)
            {
                this.subOptionKeyword = subOptionKeyword;
                this.getSubOptions = getSubOptions;
                return this;
            }

            public ActionOption Build()
            {
                return new ActionOption(

                    CreateResult: createResult,
                    Names: names,
                    SubOptionKeyword: subOptionKeyword,
                    GetSubOptions: getSubOptions
                );
            }
        }
    }
}