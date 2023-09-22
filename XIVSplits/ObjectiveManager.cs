using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Logging;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using XIVSplits.Config;
using XIVSplits.Timers;

namespace XIVSplits
{
    public partial class ObjectiveManager : IDisposable
    {
        public ObjectiveManager(ChatGui chatGui, DalamudPluginInterface pluginInterface, GameGui gameGui, ConfigService configService, InternalTimer internalTimer, LiveSplit liveSplit)
        {
            ChatGui = chatGui;
            PluginInterface = pluginInterface;
            GameGui = gameGui;
            ConfigService = configService;
            InternalTimer = internalTimer;
            LiveSplit = liveSplit;

            ChatGui.ChatMessage += ChatOnChatMessage;
            PluginInterface.UiBuilder.Draw += HandleDutyObjectives;
        }

        private DateTime lastCachedTime = DateTime.MinValue;
        private List<(string objective, float progress)>? cachedObjectives = null;
        private string CurrentDuty = "";
        private readonly List<string> AcknowledgedObjectives = new();

        public ChatGui ChatGui { get; }
        public DalamudPluginInterface PluginInterface { get; }
        public GameGui GameGui { get; }
        public ConfigService ConfigService { get; }
        public InternalTimer InternalTimer { get; }
        public LiveSplit LiveSplit { get; }

        public unsafe List<(string objective, float progress)>? GetDutyObjectives()
        {
            // Check if the cached result is still valid
            if (cachedObjectives != null && DateTime.Now - lastCachedTime <= TimeSpan.FromMilliseconds(100))
            {
                return cachedObjectives;
            }

            EventFramework* framework = EventFramework.Instance();
            if (framework == null || framework->DirectorModule.ActiveContentDirector == null)
            {
                return null;
            }

            Director currentDirector = framework->DirectorModule.ActiveContentDirector->Director;
            string duty = currentDirector.String0.ToString();
            if (string.IsNullOrEmpty(duty))
            {
                return null;
            }

            AtkUnitBase* addon = (AtkUnitBase*)GameGui.GetAddonByName("_ToDoList", 1);
            if (addon == null)
            {
                return null;
            }

            List<(string objective, float progress)> objectives = new();

            AtkUldManager manager = addon->UldManager;
            for (int i = 0; i < manager.NodeListCount; i++)
            {
                AtkResNode* node = manager.NodeList[i];
                // Sastasha ids 21001-21005, assuming other duties have more, not sure where the limit is
                if (node->NodeID < 21001 || node->NodeID > 21015)
                {
                    continue;
                }

                AtkComponentNode* componentNode = (AtkComponentNode*)node;
                if (componentNode == null || componentNode->Component == null)
                {
                    continue;
                }

                string? objective = null;
                float? progress = null;
                AtkUldManager nodeManager = componentNode->Component->UldManager;
                for (int j = 0; j < nodeManager.NodeListCount; j++)
                {
                    AtkResNode* lineNode = nodeManager.NodeList[j];
                    if (lineNode->NodeID == 6)
                    {
                        // Objective text
                        AtkTextNode* lineTextNode = (AtkTextNode*)lineNode;
                        objective = lineTextNode->NodeText.ToString();
                    }

                    if (lineNode->NodeID == 2)
                    {
                        // Objective progress bar
                        AtkNineGridNode* lineProgressNode = (AtkNineGridNode*)lineNode;
                        if (lineProgressNode->AtkResNode.IsVisible)
                        {
                            progress = lineProgressNode->AtkResNode.ScaleX;
                        }
                    }
                }

                if (objective != null && progress != null)
                {
                    objectives.Add((objective, progress.Value));
                }
            }

            // Cache the result and timestamp
            cachedObjectives = objectives;
            lastCachedTime = DateTime.Now;

            return objectives;
        }


        private unsafe string? GetDutyName()
        {
            EventFramework* framework = EventFramework.Instance();
            if (framework == null || framework->DirectorModule.ActiveContentDirector == null)
            {
                return null;
            }

            Director currentDirector = framework->DirectorModule.ActiveContentDirector->Director;
            string duty = currentDirector.String0.ToString();
            if (string.IsNullOrEmpty(duty))
            {
                return null;
            }

            return duty;
        }

        private void HandleDutyObjectives()
        {
            string? dutyName = GetDutyName();
            if (dutyName == null || string.IsNullOrWhiteSpace(dutyName))
            {
                if (CurrentDuty != "")
                {
                    // reset objectives
                    PluginLog.LogInformation($"Duty ended: {CurrentDuty}");
                    AcknowledgedObjectives.Clear();
                    CurrentDuty = "";
                }

                return;
            }

            Config.Config config = ConfigService.Get();
            if (CurrentDuty != dutyName)
            {
                // reset objectives
                AcknowledgedObjectives.Clear();
                CurrentDuty = dutyName;
                PluginLog.LogInformation($"New duty detected: {dutyName}");
            }

            KeyValuePair<string, List<Models.Objective>> dutyObjectiveConfig = config.DutyObjectives.FirstOrDefault(x => x.Key == dutyName);
            if (dutyObjectiveConfig.Value == null) return;

            List<(string objective, float progress)>? currentObjectives = GetDutyObjectives();
            if (currentObjectives == null) return;

            for (int i = 0; i < currentObjectives.Count; i++)
            {
                (string objective, float progress) = currentObjectives[i];
                for (int j = 0; j < dutyObjectiveConfig.Value.Count; j++)
                {
                    Models.Objective configObjective = dutyObjectiveConfig.Value[j];

                    Match match = Regex.Match(objective, configObjective.CompleteObjective, RegexOptions.IgnoreCase);
                    if (match.Success && configObjective.TriggerSplit)
                    {
                        if (AcknowledgedObjectives.Contains(objective)) continue;
                        if (progress < 1) continue;

                        AcknowledgedObjectives.Add(objective);
                        // trigger split
                        PluginLog.LogInformation($"Splitting on objective: {objective}");
                        InternalTimer.ManualSplit(objective);
                    }
                }
            }
        }

        private SemaphoreSlim SemaphoreSlim = new(1, 1);

        private void ChatOnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            SemaphoreSlim.Wait();

            try
            {

                if (AcknowledgedObjectives.Contains(message.TextValue))
                {
                    PluginLog.LogInformation($"Skipping objective: {message.TextValue} already acknowledged in the current duty");
                    return;
                }

                Config.Config config = ConfigService.Get();
                if (config.AutoStartTimer)
                {
                    Regex dutyRegex = HasBegunRegex();
                    Match match = dutyRegex.Match(message.TextValue);
                    if (match.Success)
                    {
                        string name = match.Groups[1].Value;
                        PluginLog.LogInformation($"Matched {message.TextValue} AutoStartTimer");
                        AcknowledgedObjectives.Add(message.TextValue);

                        if (!InternalTimer.IsRunning)
                        {
                            InternalTimer.Start(name);
                            LiveSplit.Send("startorsplit");
                        }
                        else
                        {
                            InternalTimer.Start(name);
                        }

                        return;
                    }
                }

                if (config.AutoCompletionTimeSplit)
                {
                    Regex splitRegex = CompletionTimeRegex();
                    Match splitMatch = splitRegex.Match(message.TextValue);
                    if (splitMatch.Success)
                    {
                        try
                        {
                            string name = splitMatch.Groups[1].Value;
                            string time = splitMatch.Groups[2].Value;

                            // format = mm:ss
                            int minutes = int.Parse(time.Split(":")[0]);
                            int seconds = int.Parse(time.Split(":")[1]);
                            TimeSpan parsedTime = new(0, 0, minutes, seconds);
                            PluginLog.LogInformation($"Matched {message.TextValue} AutoCompletionTimeSplit");
                            AcknowledgedObjectives.Add(message.TextValue);
                            InternalTimer.Split(parsedTime, name);
                            LiveSplit.Send("split");
                            return;
                        }
                        catch (Exception e)
                        {
                            PluginLog.LogError(e, "Failed to parse split");
                        }
                    }
                }

                /*
                if (config.AutoNoLongerSealedSplit)
                {
                    var sealedRegex = NoLongerSealedRegex();
                    var sealedMatch = sealedRegex.Match(message.TextValue);
                    if (sealedMatch.Success)
                    {
                        var name = sealedMatch.Groups[1].Value;
                        PluginLog.LogInformation($"Matched {message.TextValue} AutoNoLongerSealedOffSplit");
                        InternalTimer.ManualSplit($"{name} no longer sealed");
                        AcknowledgedObjectives.Add(message.TextValue);
                        return;
                    }
                }*/

                try
                {
                    foreach (Models.Objective? command in config.GenericObjectives.Where(x => x.ParseFromChat))
                    {
                        // skip triggers without text
                        if (string.IsNullOrEmpty(command.CompleteObjective)) continue;
                        if (!command.TriggerSplit) continue;

                        // regex match command trigger to message text
                        Regex triggerRegex = new(command.CompleteObjective);
                        Match match = triggerRegex.Match(message.TextValue);
                        if (!match.Success) continue;
                        PluginLog.LogInformation($"Matched {command.CompleteObjective} to {message.TextValue}");

                        LiveSplit.Send("split");
                        InternalTimer.ManualSplit(command.CompleteObjective);
                        AcknowledgedObjectives.Add(message.TextValue);
                        return;
                    }
                }
                catch (Exception e)
                {
                    PluginLog.LogError(e, "Error in ChatOnChatMessage");
                }
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }

        public void Dispose()
        {
            PluginInterface.UiBuilder.Draw -= HandleDutyObjectives;
            ChatGui.ChatMessage -= ChatOnChatMessage;
        }

        [GeneratedRegex("^(.+) completion time: (\\d+:\\d+)\\.$")]
        public static partial Regex CompletionTimeRegex();

        [GeneratedRegex("^(.+) has begun\\.$")]
        public static partial Regex HasBegunRegex();

        [GeneratedRegex("^(.+) will be sealed off in.+$")]
        public static partial Regex SealedOffRegex();

        [GeneratedRegex("^(.+) is no longer sealed!$")]
        public static partial Regex NoLongerSealedRegex();
    }
}
