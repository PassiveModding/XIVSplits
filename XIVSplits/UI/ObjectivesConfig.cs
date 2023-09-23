using Dalamud.Data;
using Dalamud.Logging;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using XIVSplits.Config;
using XIVSplits.Models;

namespace XIVSplits.UI
{
    public partial class ObjectivesConfig : IPluginUIComponent
    {
        public ObjectivesConfig(DataManager dataManager, ConfigService configService)
        {
            DataManager = dataManager;
            ConfigService = configService;
        }

        private string dutySearch = "";

        public DataManager DataManager { get; }
        public ConfigService ConfigService { get; }

        private readonly Objective NewGenericObjective = new()
        {
            CompleteObjective = "^.+ has ended\\.$",
            ParseFromDutyInfo = true,
            TriggerSplit = true
        };

        private readonly Objective NewDutyObjective = new()
        {
            CompleteObjective = "^Activate the coral trigger.+$",
            ParseFromDutyInfo = true,
            TriggerSplit = true
        };

        public void Draw()
        {
            // dropdown for about info
            if (ImGui.CollapsingHeader("About"))
            {
                ImGui.TextColored(new Vector4(0.5f, 0.5f, 1f, 1f), "Generic Objectives");
                ImGui.TextWrapped("You can also add generic objectives that will trigger a split when completed. These aren't restricted to a specific duty");

                ImGui.TextColored(new Vector4(0.5f, 0.5f, 1f, 1f), "Duty Objectives");
                ImGui.TextWrapped("These work the same as generic objectives but will only trigger within a specific duty.\n" +
                    "You can Add Duty Objectives by entering the duty name in the \"Add Duty\" box, it will pre-fill with potential duty objectives.\n" +
                    "Note: Some of these may not be valid as it may include some boss chat messages.");

                ImGui.TextColored(new Vector4(0.5f, 0.5f, 1f, 1f), "Duty Goal");
                ImGui.TextWrapped("Parsed from the list of objectives under Duty Information\n" +
                    "Example: The objective \"^Arrive at the God Who Whispers.+$\" will split once the progress bar is complete\n" +
                    "Note for dungeons this will typically mean the splits are called as you arrive at a mini-boss not as you kill them");

                ImGui.TextColored(new Vector4(0.5f, 0.5f, 1f, 1f), "Chat Goal");
                ImGui.TextWrapped("Parsed from the chat log\n" +
                    "Example: The objective \"^Sohm Al has ended\\.$\" will trigger a split once the text is sent to chat.");
            }

            ImGui.BeginChild("Objectives Config", new Vector2(0, 0), true);
            DrawBasicTriggers();
            DrawGenericObjectives();
            ImGui.NewLine();
            DrawDutyObjectives();
            ImGui.EndChild();
        }

        private void DrawGenericObjectives()
        {
            ImGui.Text("Generic Objectives");
            Config.Config config = ConfigService.Get();

            if (ImGui.BeginTable("Generic Objectives", 6, ImGuiTableFlags.Borders))
            {
                ImGui.TableSetupColumn("Reorder", ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableSetupColumn("Objective");
                ImGui.TableSetupColumn("Duty Goal", ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableSetupColumn("Chat Goal", ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableSetupColumn("Split", ImGuiTableColumnFlags.WidthFixed, 40);
                ImGui.TableSetupColumn("Remove", ImGuiTableColumnFlags.WidthFixed, 60);

                ImGui.TableHeadersRow();
                for (int i = 0; i < config.GenericObjectives.Count; i++)
                {
                    Objective objective = config.GenericObjectives[i];
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    if (ImGui.ArrowButton($"##{objective.GetHashCode()}_up", ImGuiDir.Up))
                    {
                        // move up
                        if (i == 0) continue;
                        Objective temp = config.GenericObjectives[i - 1];
                        config.GenericObjectives[i - 1] = objective;
                        config.GenericObjectives[i] = temp;
                    }

                    ImGui.SameLine();

                    if (ImGui.ArrowButton($"##{objective.GetHashCode()}_down", ImGuiDir.Down))
                    {
                        // move down
                        if (i == config.GenericObjectives.Count - 1) continue;
                        Objective temp = config.GenericObjectives[i + 1];
                        config.GenericObjectives[i + 1] = objective;
                        config.GenericObjectives[i] = temp;
                    }

                    ImGui.TableNextColumn();
                    ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
                    string completeObjective = objective.CompleteObjective;
                    if (ImGui.InputText($"##{objective.GetHashCode()}_complete", ref completeObjective, 256) && completeObjective != objective.CompleteObjective)
                    {
                        objective.CompleteObjective = completeObjective;
                        ConfigService.Save();
                    }

                    ImGui.TableNextColumn();
                    bool parseFromDutyInfo = objective.ParseFromDutyInfo;
                    if (ImGui.Checkbox($"##{objective.GetHashCode()}_dutyinfo", ref parseFromDutyInfo) && parseFromDutyInfo != objective.ParseFromDutyInfo)
                    {
                        objective.ParseFromDutyInfo = parseFromDutyInfo;
                        ConfigService.Save();
                    }

                    ImGui.TableNextColumn();
                    bool parseFromChat = objective.ParseFromChat;
                    if (ImGui.Checkbox($"##{objective.GetHashCode()}_chat", ref parseFromChat) && parseFromChat != objective.ParseFromChat)
                    {
                        objective.ParseFromChat = parseFromChat;
                        ConfigService.Save();
                    }

                    ImGui.TableNextColumn();
                    bool triggerSplit = objective.TriggerSplit;
                    if (ImGui.Checkbox($"##{objective.GetHashCode()}_split", ref triggerSplit) && triggerSplit != objective.TriggerSplit)
                    {
                        objective.TriggerSplit = triggerSplit;
                        ConfigService.Save();
                    }

                    ImGui.TableNextColumn();
                    if (ImGui.Button($"Remove##{objective.GetHashCode()}_remove"))
                    {
                        config.GenericObjectives.Remove(objective);
                        ConfigService.Save();
                        break;
                    }
                }

                // additional row for adding new objectives
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TableNextColumn();
                ImGui.TableNextColumn();
                ImGui.TableNextColumn();
                ImGui.TableNextColumn();
                ImGui.TableNextColumn();
                if (ImGui.Button($"Add##add_new_objective_generic"))
                {
                    Objective newObjective = new()
                    {
                        CompleteObjective = "^(.+) has been defeated.$",
                        ParseFromDutyInfo = false,
                        ParseFromChat = false,
                        TriggerSplit = false
                    };
                    config.GenericObjectives.Add(newObjective);
                    ConfigService.Save();
                }

                ImGui.EndTable();
            }
        }

        private void DrawBasicTriggers()
        {
            Config.Config config = ConfigService.Get();
            // autosplit and autostart checkboxes
            bool autoStart = config.AutoStartTimer;
            if (ImGui.Checkbox("Auto Start", ref autoStart) && autoStart != config.AutoStartTimer)
            {
                config.AutoStartTimer = autoStart;
                ConfigService.Save();
            }
            ImGui.SameLine();
            // hover text for regex help
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip($"Will start the timer when \"{ObjectiveManager.HasBegunRegex()}\" is sent to chat.");
            }

            ImGui.SameLine();
            bool autoSplit = config.AutoCompletionTimeSplit;
            if (ImGui.Checkbox("Auto Completion Split", ref autoSplit) && autoSplit != config.AutoCompletionTimeSplit)
            {
                config.AutoCompletionTimeSplit = autoSplit;
                ConfigService.Save();
            }
            ImGui.SameLine();
            // hover text for regex help
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip($"Will split the timer when \"{ObjectiveManager.CompletionTimeRegex()}\" is sent to chat.");
            }

            /*
            ImGui.SameLine();
            bool autoSealedOff = config.AutoSealedOffSplit;
            if (ImGui.Checkbox("Auto Sealed Off Split", ref autoSealedOff) && autoSealedOff != config.AutoSealedOffSplit)
            {
                config.AutoSealedOffSplit = autoSealedOff;
                ConfigService.Save();
            }
            ImGui.SameLine();
            // hover text for regex help
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip($"Will split the timer when \"{ObjectiveManager.SealedOffRegex()}\" is sent to chat.\nNote if you kill a boss without aggroing it, \n" +
                    $"this will not trigger because there is no message sent.");
            }

            
            ImGui.SameLine();
            var autoNoLongerSealedOff = config.AutoNoLongerSealedSplit;
            if (ImGui.Checkbox("Auto No Longer Sealed Split", ref autoNoLongerSealedOff) && autoNoLongerSealedOff != config.AutoNoLongerSealedSplit)
            {
                config.AutoNoLongerSealedSplit = autoNoLongerSealedOff;
                ConfigService.Save();
            }
            ImGui.SameLine();
            // hover text for regex help
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip($"Will split the timer when \"{ObjectiveManager.NoLongerSealedRegex}\" is sent to chat.\n" +
                    "Note this will not trigger if a boss is killed before a duty is sealed off");
            }*/
        }


        private void DrawDutyObjectives()
        {
            ImGui.Text("Duty Objectives");
            DrawDutySearchInput();
            Config.Config config = ConfigService.Get();
            for (int i = 0; i < config.DutyObjectives.Count; i++)
            {
                KeyValuePair<string, List<Objective>> duty = config.DutyObjectives.ElementAt(i);
                // dropdown 
                if (ImGui.CollapsingHeader(duty.Key))
                {
                    string name = duty.Key;
                    if (ImGui.InputText($"##{duty.Key}_name", ref name, 256, ImGuiInputTextFlags.EnterReturnsTrue) && name != duty.Key)
                    {
                        // if new name found in list already, abort
                        if (config.DutyObjectives.ContainsKey(name))
                        {
                            PluginLog.LogError($"Duty name already exists: {name}");
                            continue;
                        }

                        config.DutyObjectives.Remove(duty.Key);
                        config.DutyObjectives.Add(name, duty.Value);
                        ConfigService.Save();
                    }

                    if (ImGui.BeginTable(name, 6, ImGuiTableFlags.Borders))
                    {
                        ImGui.TableSetupColumn("Reorder", ImGuiTableColumnFlags.WidthFixed, 60);
                        ImGui.TableSetupColumn("Objective");
                        ImGui.TableSetupColumn("Duty Goal", ImGuiTableColumnFlags.WidthFixed, 60);
                        ImGui.TableSetupColumn("Chat Goal", ImGuiTableColumnFlags.WidthFixed, 60);
                        ImGui.TableSetupColumn("Split", ImGuiTableColumnFlags.WidthFixed, 40);
                        ImGui.TableSetupColumn("Remove", ImGuiTableColumnFlags.WidthFixed, 60);

                        ImGui.TableHeadersRow();

                        // should be able to re-order objectives

                        for (int j = 0; j < duty.Value.Count; j++)
                        {
                            Objective objective = duty.Value[j];
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();

                            if (ImGui.ArrowButton($"##{objective.GetHashCode()}_up", ImGuiDir.Up))
                            {
                                // move up
                                if (j == 0) continue;
                                Objective temp = duty.Value[j - 1];
                                duty.Value[j - 1] = objective;
                                duty.Value[j] = temp;
                            }

                            ImGui.SameLine();

                            if (ImGui.ArrowButton($"##{objective.GetHashCode()}_down", ImGuiDir.Down))
                            {
                                // move down
                                if (j == duty.Value.Count - 1) continue;
                                Objective temp = duty.Value[j + 1];
                                duty.Value[j + 1] = objective;
                                duty.Value[j] = temp;
                            }

                            ImGui.TableNextColumn();
                            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
                            string completeObjective = objective.CompleteObjective;
                            if (ImGui.InputText($"##{objective.GetHashCode()}_complete", ref completeObjective, 256) && completeObjective != objective.CompleteObjective)
                            {
                                objective.CompleteObjective = completeObjective;
                                ConfigService.Save();
                            }

                            ImGui.TableNextColumn();
                            bool parseFromDutyInfo = objective.ParseFromDutyInfo;
                            if (ImGui.Checkbox($"##{objective.GetHashCode()}_dutyinfo", ref parseFromDutyInfo) && parseFromDutyInfo != objective.ParseFromDutyInfo)
                            {
                                objective.ParseFromDutyInfo = parseFromDutyInfo;
                                ConfigService.Save();
                            }

                            ImGui.TableNextColumn();
                            bool parseFromChat = objective.ParseFromChat;
                            if (ImGui.Checkbox($"##{objective.GetHashCode()}_chat", ref parseFromChat) && parseFromChat != objective.ParseFromChat)
                            {
                                objective.ParseFromChat = parseFromChat;
                                ConfigService.Save();
                            }

                            ImGui.TableNextColumn();
                            bool triggerSplit = objective.TriggerSplit;
                            if (ImGui.Checkbox($"##{objective.GetHashCode()}_split", ref triggerSplit) && triggerSplit != objective.TriggerSplit)
                            {
                                objective.TriggerSplit = triggerSplit;
                                ConfigService.Save();
                            }

                            ImGui.TableNextColumn();
                            if (ImGui.Button($"Remove##{objective.GetHashCode()}_remove"))
                            {
                                duty.Value.Remove(objective);
                                ConfigService.Save();
                                break;
                            }
                        }

                        // additional row for adding new objectives
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.TableNextColumn();
                        ImGui.TableNextColumn();
                        ImGui.TableNextColumn();
                        ImGui.TableNextColumn();
                        ImGui.TableNextColumn();
                        if (ImGui.Button($"Add##add_new_objective_{duty.Key.GetHashCode()}"))
                        {
                            Objective newObjective = new()
                            {
                                CompleteObjective = "^Activate the button.+$",
                                ParseFromDutyInfo = false,
                                ParseFromChat = false,
                                TriggerSplit = false
                            };
                            duty.Value.Add(newObjective);
                            ConfigService.Save();
                        }

                        ImGui.EndTable();
                    }

                    if (ImGui.Button($"Remove Duty##{duty.Key.GetHashCode()}"))
                    {
                        config.DutyObjectives.Remove(duty.Key);
                        ConfigService.Save();
                        break;
                    }
                }
            }
        }

        private List<ContentFinderCondition> contentFinderConditionCache = new();

        private void DrawDutySearchInput()
        {
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 100);
            bool changed = ImGui.InputText("##duty_search", ref dutySearch, 256);

            ImGui.SameLine();
            ImGui.Text("Add Duty");
            ImGui.SameLine();
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("This will search for the duty name in the duty finder and add all objectives as splits, \n" +
                                "note there may be some junk entries since not all objective texts are displayed\n" +
                                "I recommend taking note of the objectives at the end of a duty for this purpose.");
            }

            if (dutySearch == "")
            {
                contentFinderConditionCache.Clear();
                return;
            }

            // show results
            if (changed)
            {
                PluginLog.Log($"Searching for {dutySearch}");
                Lumina.Excel.ExcelSheet<ContentFinderCondition>? contentFinderConditions = DataManager.GetExcelSheet<ContentFinderCondition>();
                IEnumerable<ContentFinderCondition> matches = contentFinderConditions.Where(x => x.Name.RawString.Contains(dutySearch, StringComparison.InvariantCultureIgnoreCase));
                contentFinderConditionCache = matches.ToList();
            }

            if (contentFinderConditionCache.Count > 0)
            {
                ImGui.Text("Matches");
                foreach (ContentFinderCondition condition in contentFinderConditionCache)
                {
                    if (ImGui.Button(condition.Name.RawString))
                    {
                        AddDuty(condition.Name.RawString);
                        dutySearch = "";
                    }
                }
            }
        }

        private void AddDuty(string searchTerm)
        {
            // add new duty objective
            Lumina.Excel.ExcelSheet<ContentFinderCondition>? contentFinderConditions = DataManager.GetExcelSheet<ContentFinderCondition>();
            ContentFinderCondition? condition = contentFinderConditions.FirstOrDefault(x =>
            {
                // trim non a-z characters from both, then compare
                Regex regex = DutyNameRegex();
                string xName = regex.Replace(x.Name.RawString, "");
                string searchTermName = regex.Replace(searchTerm, "");

                return xName.Equals(searchTermName, StringComparison.InvariantCultureIgnoreCase);
            });
            if (condition == null)
            {
                PluginLog.LogError($"Could not find duty: {dutySearch}");
                return;
            }

            Lumina.Excel.ExcelSheet<InstanceContent>? instanceContents = DataManager.GetExcelSheet<InstanceContent>();

            InstanceContent? match = instanceContents.FirstOrDefault(x => x.RowId == condition.Content);
            if (match == null)
            {
                return;
            }

            int startIndex = (int)match.InstanceContentTextDataObjectiveStart.Row;
            int endIndex = (int)match.InstanceContentTextDataObjectiveEnd.Row;

            Lumina.Excel.ExcelSheet<InstanceContentTextData>? instanceContentsTextData = DataManager.GetExcelSheet<InstanceContentTextData>();

            IEnumerable<InstanceContentTextData> matched = instanceContentsTextData.Where(x => x.RowId >= startIndex && x.RowId <= endIndex);

            List<Objective> objectives = new();
            foreach (InstanceContentTextData? item in matched)
            {
                objectives.Add(new Objective
                {
                    CompleteObjective = $"^{Regex.Escape(item.Text)}.*$",
                    ParseFromDutyInfo = true,
                    TriggerSplit = true,
                });
            }

            Config.Config config = ConfigService.Get();
            config.DutyObjectives.Add(condition.Name.RawString, objectives);
            ConfigService.Save();
        }

        [GeneratedRegex("[^a-zA-Z]")]
        private static partial Regex DutyNameRegex();
    }
}
