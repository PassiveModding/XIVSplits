using Dalamud.Configuration;
using System;
using System.Collections.Generic;
using XIVSplits.Models;

namespace XIVSplits.Config
{
    public partial class Config : IPluginConfiguration
    {
        public const int CurrentVersion = 1;

        public int Version { get; set; } = CurrentVersion;
        public bool ShowTimer { get; set; } = true;
        public bool ShowSettings { get; set; }
        public string LiveSplitServer { get; set; } = "localhost";
        public int LiveSplitPort { get; set; } = 16834;

        public bool AutoStartTimer { get; set; } = true;
        public bool AutoCompletionTimeSplit { get; set; } = true;
        public bool SingleDutyMode { get; set; } = false;

        public Dictionary<string, List<Objective>> DutyObjectives { get; set; } = new();
        public List<Objective> GenericObjectives { get; set; } = new();
        public string CurrentProfile { get; set; } = "Default";
        public Dictionary<string, SplitProfile> SplitCollection { get; set; } = new()
        {
            { 
                "Default", 
                new SplitProfile()
                {
                    Template = new List<Split>()
                    {
                        new Split() { Name = "Duty Complete" }
                    },
                    History = new Dictionary<DateTime, List<Split>>()
                }
            }
        };

        public SplitProfile GetCurrentProfile()
        {
            if (SplitCollection.TryGetValue(CurrentProfile, out SplitProfile? profile))
            {
                return profile;
            }

            SplitCollection.Add(CurrentProfile, new SplitProfile
            {
                Template = new List<Split>()
                {
                    new Split() { Name = "Duty Complete" }
                },
            });
            return SplitCollection[CurrentProfile];
        }
    }
}
