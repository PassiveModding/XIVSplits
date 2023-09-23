using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XIVSplits.Config;
using XIVSplits.Models;

namespace XIVSplits.UI
{
    public class SplitHistory : IPluginUIComponent
    {
        public SplitHistory(ConfigService configService)
        {
            ConfigService = configService;
        }

        public ConfigService ConfigService { get; }

        public void Draw()
        {
            var config = ConfigService.Get();            
            foreach (var profile in config.SplitCollection)
            {
                if (ImGui.CollapsingHeader(profile.Key))
                {
                    if (profile.Value.History.Count == 0)
                    {
                        ImGui.Text("No history yet.");
                        continue;
                    }
                    ImGui.BeginChild($"##{profile.GetHashCode()}_history", new System.Numerics.Vector2(0, 0), true, ImGuiWindowFlags.None);
                    DrawHistory(profile.Value);
                    ImGui.EndChild();
                }
            }
        }

        private void DrawHistory(SplitProfile profile)
        {
            var splitHistory = profile.History;
            // table for history
            for (int historyIndex = 0; historyIndex < splitHistory.Count; historyIndex++)
            {
                var history = splitHistory.ElementAt(historyIndex);
                var splits = history.Value;

                if (!ImGui.CollapsingHeader($"{history.Key} - {splits[^1].Total:mm\\:ss\\.ff}"))
                {
                    continue;
                }

                if (ImGui.BeginTable($"##{splits.GetHashCode()}_history", 6, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.Reorderable | ImGuiTableFlags.Hideable))
                {
                    ImGui.TableSetupColumn("Name");
                    ImGui.TableSetupColumn("Tracked Name");
                    ImGui.TableSetupColumn("Parsed Segment", ImGuiTableColumnFlags.WidthFixed, 100);
                    ImGui.TableSetupColumn("Actual Segment", ImGuiTableColumnFlags.WidthFixed, 100);
                    ImGui.TableSetupColumn("Split", ImGuiTableColumnFlags.WidthFixed, 80);
                    ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 80);
                    ImGui.TableHeadersRow();

                    for (int j = 0; j < splits.Count; j++)
                    {
                        var split = splits[j];

                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.Text(split.Name);
                        ImGui.TableNextColumn();
                        ImGui.Text(split.Objective);
                        ImGui.TableNextColumn();
                        ImGui.Text(split.SegmentParsed.ToString("mm\\:ss"));
                        ImGui.TableNextColumn();
                        ImGui.Text(split.Segment.ToString("mm\\:ss\\.ff"));
                        ImGui.TableNextColumn();
                        ImGui.Text(split.SplitTime.ToString("mm\\:ss\\.ff"));
                        ImGui.TableNextColumn();
                        ImGui.Text(split.Total.ToString("mm\\:ss\\.ff"));
                    }

                    ImGui.EndTable();
                }

                if (ImGui.Button($"Remove##{history.GetHashCode()}"))
                {
                    splitHistory.Remove(history.Key);
                    ConfigService.Save();
                    break;
                }
            }
        }
    }
}
