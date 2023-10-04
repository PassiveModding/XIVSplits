using Dalamud.Data;
using Dalamud.Logging;
using Dalamud.Plugin.Services;
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
        public ObjectivesConfig(IDataManager dataManager, ConfigService configService, IPluginLog pluginLog)
        {
            DataManager = dataManager;
            ConfigService = configService;
            PluginLog = pluginLog;
        }

        private string dutySearch = "";

        public IDataManager DataManager { get; }
        public ConfigService ConfigService { get; }
        public IPluginLog PluginLog { get; }

        private readonly Objective NewGenericObjective = new()
        {
            CompleteObjective = "^.+ has ended\\.$",
            GoalType = GoalType.Chat,
            TriggerSplit = true
        };

        private readonly Objective NewDutyObjective = new()
        {
            CompleteObjective = "^Activate the coral trigger.+$",
            GoalType = GoalType.DutyObjective,
            TriggerSplit = true
        };

        public void Draw()
        {
            // dropdown for about info
            if (ImGui.CollapsingHeader("About"))
            {
                ImGui.TextWrapped("You can also add generic objectives that will trigger a split when completed. These aren't restricted to a specific duty");
                ImGui.TextWrapped("You can also add a duty instead, objectives under a duty will only trigger within that duty.\n" +
                    "Enter a duty name in the \"Add Duty\" box, it will pre-fill with potential duty objectives.\n" +
                    "Note: Some of these may not be valid as it may include some boss chat messages.");

                // Goals info
                ImGui.TextColored(new Vector4(0.5f, 0.5f, 1f, 1f), "Chat Goal");
                ImGui.TextWrapped("Parsed from the chat log\n" +
                    "Example: The objective \"^Sohm Al has ended\\.$\" will trigger a split once the text is sent to chat.");

                ImGui.TextColored(new Vector4(0.5f, 0.5f, 1f, 1f), "Duty Objective Goal");
                ImGui.TextWrapped("Parsed from the list of objectives under Duty Information\n" +
                    "Example: The objective \"^Arrive at the God Who Whispers.+$\" will split once the progress bar is complete\n" +
                    "Note for dungeons this will typically mean the splits are called as you arrive at a mini-boss not as you kill them");
            }

            DrawBasicTriggers();
            DrawGenericObjectives();
            ImGui.NewLine();
            DrawDutyObjectives();
        }

        private void DrawGenericObjectives()
        {
            ImGui.Text("Objectives");
            Config.Config config = ConfigService.Get();

            if (ImGui.BeginTable("Generic Objectives", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                DrawObjectiveColumnHeaders();

                for (int i = 0; i < config.GenericObjectives.Count; i++)
                {
                    Objective objective = config.GenericObjectives[i];
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    // input index
                    var jText = i.ToString();
                    if (ImGui.InputText($"##{objective.GetHashCode()}_index", ref jText, 256, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CharsDecimal) && int.Parse(jText) != i)
                    {
                        config.GenericObjectives.Remove(objective);
                        config.GenericObjectives.Insert(int.Parse(jText), objective);
                        ConfigService.Save();
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
                    ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
                    DrawGoalTypeDropDown(objective);

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
                if (ImGui.Button($"Add##add_new_objective_generic"))
                {
                    Objective newObjective = new()
                    {
                        CompleteObjective = "^(.+) has been defeated.$",
                        GoalType = GoalType.Chat,
                        TriggerSplit = false
                    };
                    config.GenericObjectives.Add(newObjective);
                    ConfigService.Save();
                }

                ImGui.EndTable();
            }
        }

        private void DrawGoalTypeDropDown(Objective objective)
        {
            GoalType goalType = objective.GoalType;
            if (ImGui.BeginCombo($"##{objective.GetHashCode()}_goaltype", goalType.ToString()))
            {
                foreach (GoalType type in Enum.GetValues(typeof(GoalType)))
                {
                    if (ImGui.Selectable(type.ToString(), type == goalType))
                    {
                        objective.GoalType = type;
                        ConfigService.Save();
                    }
                }

                ImGui.EndCombo();
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
        }


        private void DrawDutyObjectives()
        {
            ImGui.Text("Duties");
            DrawDutySearchInput();
            Config.Config config = ConfigService.Get();
            for (int i = 0; i < config.DutyObjectives.Count; i++)
            {
                KeyValuePair<string, List<Objective>> duty = config.DutyObjectives.ElementAt(i);
                // dropdown 
                if (!ImGui.CollapsingHeader(duty.Key))
                {
                    continue;
                }

                string name = duty.Key;
                ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
                if (ImGui.InputText($"##{duty.Key}_name", ref name, 256, ImGuiInputTextFlags.EnterReturnsTrue) && name != duty.Key)
                {
                    // if new name found in list already, abort
                    if (config.DutyObjectives.ContainsKey(name))
                    {
                        PluginLog.Error($"Duty name already exists: {name}");
                        continue;
                    }

                    config.DutyObjectives.Remove(duty.Key);
                    config.DutyObjectives.Add(name, duty.Value);
                    ConfigService.Save();
                }

                if (ImGui.BeginTable(name, 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
                {
                    DrawObjectiveColumnHeaders();

                    for (int j = 0; j < duty.Value.Count; j++)
                    {
                        Objective objective = duty.Value[j];
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        // input index
                        var jText = j.ToString();
                        if (ImGui.InputText($"##{objective.GetHashCode()}_index", ref jText, 256, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CharsDecimal) && int.Parse(jText) != j)
                        {
                            duty.Value.Remove(objective);
                            duty.Value.Insert(int.Parse(jText), objective);
                            ConfigService.Save();
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
                        ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
                        DrawGoalTypeDropDown(objective);

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
                    if (ImGui.Button($"Add##add_new_objective_{duty.Key.GetHashCode()}"))
                    {
                        Objective newObjective = new()
                        {
                            CompleteObjective = "^Activate the button.+$",
                            GoalType = GoalType.DutyObjective,
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

        private void DrawObjectiveColumnHeaders()
        {
            ImGui.TableSetupColumn("Reorder", ImGuiTableColumnFlags.WidthFixed, 60);
            ImGui.TableSetupColumn("Objective");
            ImGui.TableSetupColumn("Goal Type", ImGuiTableColumnFlags.WidthFixed, 120);
            ImGui.TableSetupColumn("Split", ImGuiTableColumnFlags.WidthFixed, 30);
            ImGui.TableSetupColumn("Remove", ImGuiTableColumnFlags.WidthFixed, 70);
            ImGui.TableHeadersRow();
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
                PluginLog.Debug($"Searching for {dutySearch}");
                Lumina.Excel.ExcelSheet<ContentFinderCondition>? contentFinderConditions = DataManager.GetExcelSheet<ContentFinderCondition>();
                IEnumerable<ContentFinderCondition> matches = contentFinderConditions!.Where(x => x.Name.RawString.Contains(dutySearch, StringComparison.InvariantCultureIgnoreCase));
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
            ContentFinderCondition? condition = contentFinderConditions!.FirstOrDefault(x =>
            {
                // trim non a-z characters from both, then compare
                Regex regex = DutyNameRegex();
                string xName = regex.Replace(x.Name.RawString, "");
                string searchTermName = regex.Replace(searchTerm, "");

                return xName.Equals(searchTermName, StringComparison.InvariantCultureIgnoreCase);
            });
            if (condition == null)
            {
                PluginLog.Error($"Could not find duty: {dutySearch}");
                return;
            }

            Lumina.Excel.ExcelSheet<InstanceContent>? instanceContents = DataManager.GetExcelSheet<InstanceContent>();

            InstanceContent? match = instanceContents!.FirstOrDefault(x => x.RowId == condition.Content);
            if (match == null)
            {
                return;
            }

            int startIndex = (int)match.InstanceContentTextDataObjectiveStart.Row;
            int endIndex = (int)match.InstanceContentTextDataObjectiveEnd.Row;

            Lumina.Excel.ExcelSheet<InstanceContentTextData>? instanceContentsTextData = DataManager.GetExcelSheet<InstanceContentTextData>();

            IEnumerable<InstanceContentTextData> matched = instanceContentsTextData!.Where(x => x.RowId >= startIndex && x.RowId <= endIndex);

            List<Objective> objectives = new();
            foreach (InstanceContentTextData? item in matched)
            {
                objectives.Add(new Objective
                {
                    CompleteObjective = $"^{Regex.Escape(item.Text)}.*$",
                    GoalType = GoalType.DutyObjective,
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
