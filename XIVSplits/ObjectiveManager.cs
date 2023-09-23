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
using XIVSplits.Models;
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

            var config = ConfigService.Get();
            if (CurrentDuty != dutyName)
            {
                // reset objectives
                AcknowledgedObjectives.Clear();
                CurrentDuty = dutyName;
                PluginLog.LogInformation($"New duty detected: {dutyName}");
            }


            var currentObjectives = GetDutyObjectives();
            if (currentObjectives == null) return;


            var dutyObjectiveConfig = config.DutyObjectives.FirstOrDefault(x => x.Key == dutyName);
            var genericObjectives = config.GenericObjectives.Where(x => x.GoalType == GoalType.DutyObjective).ToArray();

            for (int i = 0; i < currentObjectives.Count; i++)
            {
                (string objective, float progress) = currentObjectives[i];

                if (dutyObjectiveConfig.Value != null)
                {
                    for (int j = 0; j < dutyObjectiveConfig.Value.Count; j++)
                    {
                        Objective configObjective = dutyObjectiveConfig.Value[j];

                        Match match = Regex.Match(objective, configObjective.CompleteObjective, RegexOptions.IgnoreCase);
                        if (match.Success && configObjective.TriggerSplit)
                        {
                            if (AcknowledgedObjectives.Contains(objective)) continue;
                            if (progress < 1) continue;

                            AcknowledgedObjectives.Add(objective);
                            // trigger split
                            PluginLog.LogInformation($"Splitting on duty objective: {objective}");
                            InternalTimer.ManualSplit(objective);
                        }
                    }
                }

                // TODO: Since it's a generic objective, should we check if it's already been acknowledged?
                // this would prevent the same objective from triggering multiple splits but if it follows the same pattern of
                // being reset per duty what if someone wants to split on the same objective multiple times in a duty?
                foreach (Objective genericObjective in genericObjectives)
                {
                    Match match = Regex.Match(objective, genericObjective.CompleteObjective, RegexOptions.IgnoreCase);
                    if (match.Success && genericObjective.TriggerSplit)
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

                try
                {
                    foreach (Objective? command in config.GenericObjectives.Where(x => x.GoalType == GoalType.Chat))
                    {
                        if (HandleChatObjective(command, message.TextValue))
                        {
                            return;
                        }
                    }

                    var duty = config.DutyObjectives.FirstOrDefault(x => x.Key == CurrentDuty);
                    if (duty.Value == null) return;
                    foreach (Objective? command in duty.Value.Where(x => x.GoalType == GoalType.Chat))
                    {
                        if (HandleChatObjective(command, message.TextValue))
                        {
                            return;
                        }
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

        private bool HandleChatObjective(Objective command, string message)
        {
            // skip triggers without text
            if (string.IsNullOrEmpty(command.CompleteObjective)) return false;
            if (!command.TriggerSplit) return false;

            // regex match command trigger to message text
            Regex triggerRegex = new(command.CompleteObjective);
            Match match = triggerRegex.Match(message);
            if (!match.Success) return false;
            PluginLog.LogInformation($"Matched {command.CompleteObjective} to {message}");

            LiveSplit.Send("split");
            InternalTimer.ManualSplit(command.CompleteObjective);
            AcknowledgedObjectives.Add(message);
            return true;
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
    }
}
