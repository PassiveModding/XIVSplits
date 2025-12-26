using Dalamud.Bindings.ImGui;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using XIVSplits.Config;

namespace XIVSplits.UI
{
    public partial class QuestConfig : IPluginUIComponent
    {
        public IDataManager DataManager { get; }
        public ConfigService ConfigService { get; }

        private string search = string.Empty;
        private bool showOnlySelected = false;

        public QuestConfig(IDataManager dataManager, ConfigService configService)
        {
            DataManager = dataManager;
            ConfigService = configService;
        }

        public void Draw()
        {
            Config.Config config = ConfigService.Get();
            var questSheet = DataManager.Excel.GetSheet<Quest>();
            if (questSheet == null)
                return;

            if (ImGui.Checkbox("Enable all quests", ref config.EnableAllQuests))
                ConfigService.Save();

            if (config.EnableAllQuests)
            {
                ImGui.TextDisabled("All quests are enabled. Individual selection is ignored.");
                return;
            }

            ImGui.Separator();

            ImGui.InputText("Search", ref search, 100);

            ImGui.SameLine();

            ImGui.Checkbox("Show only selected", ref showOnlySelected);

            var filtered = questSheet
            .Select(q =>
            {
                var name = q.Name.ToString();
                return new
                {
                    q.RowId,
                    Name = name,
                    HasName = !string.IsNullOrWhiteSpace(name)
                };
            })
            .Where(q =>
            {
                bool isSelected = config.SelectedQuestIds.Contains(q.RowId);

                // Hide nameless quests unless already selected
                if (!q.HasName && !isSelected)
                    return false;

                string display = $"[{q.RowId}] {q.Name}";

                if (!string.IsNullOrEmpty(search) &&
                    !display.Contains(search, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (showOnlySelected && !isSelected)
                    return false;

                return true;
            })
            .ToList();

            ImGui.BeginChild("QuestScroll", new Vector2(0, 200), true);

            var clipper = new ImGuiListClipper();
            clipper.Begin(filtered.Count);

            while (clipper.Step())
            {
                for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                {
                    var quest = filtered[i];
                    bool isChecked = config.SelectedQuestIds.Contains(quest.RowId);

                    if (ImGui.Checkbox($"##quest_{quest.RowId}", ref isChecked))
                    {
                        if (isChecked)
                            config.SelectedQuestIds.Add(quest.RowId);
                        else
                            config.SelectedQuestIds.Remove(quest.RowId);

                        ConfigService.Save();
                    }

                    ImGui.SameLine();
                    ImGui.TextUnformatted($"[{quest.RowId}] {quest.Name}");
                }
            }

            clipper.End();
            ImGui.EndChild();

            ImGui.Spacing();

            if (ImGui.Button("Clear All Selected Quests"))
            {
                if (config.SelectedQuestIds.Count > 0)
                {
                    config.SelectedQuestIds.Clear();
                    ConfigService.Save();
                }
            }
        }
    }
}
