using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using XIVSplits.Config;
using XIVSplits.Models;

namespace XIVSplits.UI
{
    public class Splits : IPluginUIComponent
    {
        public Splits(ConfigService configService)
        {
            ConfigService = configService;
        }

        public ConfigService ConfigService { get; }

        public void Draw()
        {
            // create templates for runs, ie. specify segments and their names and the profile name

            // create a new run from a template
            var config = ConfigService.Get();
            // About section
            if (ImGui.CollapsingHeader("About"))
            {
                ImGui.TextWrapped("A template represents a set of splits that can be used to create a new run.");
                ImGui.TextWrapped("A run is a set of splits that are tracked and saved.");
                ImGui.TextWrapped("The template will also keep track of the best times for each split.");
                ImGui.TextColored(new Vector4(0.5f, 0.5f, 1f, 1f), "Game Segment");
                ImGui.TextWrapped("Game Segment is the time based on the 'completion time' text that appears in game.");
                ImGui.TextWrapped("This is only set using the 'Auto Completion Split' option and will otherwise be set to zero.");
                ImGui.TextColored(new Vector4(0.5f, 0.5f, 1f, 1f), "Segment");
                ImGui.TextWrapped("Segment is the time based on the time between the start of combat (or the split) and the end of combat (or the split).");
                ImGui.TextWrapped("Depending on the content being run, this may be equal to or shorter than the actual split time.");
                ImGui.TextColored(new Vector4(0.5f, 0.5f, 1f, 1f), "Split");
                ImGui.TextWrapped("Split is the time based on the time between the last split and the current split.");
            }

            // dropdown, select from existing templates
            if (ImGui.BeginCombo("Current Template", config.CurrentProfile))
            {
                foreach (string template in config.SplitCollection.Keys.OrderBy(x => x))
                {
                    bool isSelected = config.CurrentProfile == template;
                    if (ImGui.Selectable(template, isSelected))
                    {
                        config.CurrentProfile = template;
                        ConfigService.Save();
                    }

                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.SameLine();
            // add a new template
            if (ImGui.Button($"New"))
            {
                // check if name exists, add number if it does
                int i = 1;
                string newName = $"New Template";
                while (config.SplitCollection.ContainsKey(newName))
                {
                    newName = $"New Template ({i})";
                    i++;
                }

                config.SplitCollection[newName] = new SplitProfile();
                config.CurrentProfile = newName;
                ConfigService.Save();
            }

            ImGui.NewLine();
            // draw active template first
            var activeProfile = config.SplitCollection[config.CurrentProfile];
            DrawTemplate(config.CurrentProfile, activeProfile);
        }

        private void DrawTemplate(string profileName, SplitProfile profile)
        {
            var config = ConfigService.Get();
            var name = profileName;
            var splitTemplate = profile.Template;

            // if there are no splits, add one
            if (splitTemplate.Count == 0)
            {
                splitTemplate.Add(new Split());
                ConfigService.Save();
            }

            if (ImGui.InputText($"##{profile.GetHashCode()}_name", ref name, 256, ImGuiInputTextFlags.EnterReturnsTrue) && name != profileName)
            {
                // check if name exists, add number if it does
                int j = 1;
                string newName = name.Trim();
                while (config.SplitCollection.ContainsKey(newName))
                {
                    newName = $"{name} ({j})";
                    j++;
                }

                config.SplitCollection.Remove(profileName);
                config.SplitCollection[newName] = profile;
                config.CurrentProfile = newName;
                ConfigService.Save();
            }

            ImGui.SameLine();
            ImGui.Text("Name");

            // table for splits
            // fit content, do not expand Y
            if (ImGui.BeginTable("Split Template", 6, ImGuiTableFlags.Borders | ImGuiTableFlags.Hideable))
            {
                ImGui.TableSetupColumn("Reorder", ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableSetupColumn("Name");

                // columns for setting the time
                ImGui.TableSetupColumn("Best Game Segment", ImGuiTableColumnFlags.WidthFixed, 120);
                ImGui.TableSetupColumn("Best Segment", ImGuiTableColumnFlags.WidthFixed, 120);
                ImGui.TableSetupColumn("Best Split", ImGuiTableColumnFlags.WidthFixed, 120);

                ImGui.TableSetupColumn("Remove", ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableHeadersRow();

                for (int j = 0; j < splitTemplate.Count; j++)
                {
                    var split = splitTemplate[j];
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    // input index
                    var jText = j.ToString();
                    if (ImGui.InputText($"##{split.GetHashCode()}_index", ref jText, 256, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CharsDecimal) && int.Parse(jText) != j)
                    {
                        splitTemplate.Remove(split);
                        splitTemplate.Insert(int.Parse(jText), split);
                        ConfigService.Save();
                    }

                    ImGui.TableNextColumn();
                    ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
                    var splitName = split.Name;
                    if (ImGui.InputText($"##{split.GetHashCode()}_complete", ref splitName, 256) && splitTemplate[j].Name != splitName)
                    {
                        splitTemplate[j].Name = splitName;
                        ConfigService.Save();
                    }

                    ImGui.TableNextColumn();
                    ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
                    var bestParsed = split.BestSegmentParsed.FormatTimeHHMMSS(false);
                    if (ImGui.InputText($"##{split.GetHashCode()}_best_parsed", ref bestParsed, 256, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        if (TryParseTimeInput(bestParsed, out var time))
                        {
                            splitTemplate[j].BestSegmentParsed = time;
                            ConfigService.Save();
                        }
                    }

                    ImGui.TableNextColumn();
                    ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
                    var bestSegment = split.BestSegment.FormatTimeHHMMSS();
                    if (ImGui.InputText($"##{split.GetHashCode()}_best", ref bestSegment, 256, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        if (TryParseTimeInput(bestSegment, out var time))
                        {
                            splitTemplate[j].BestSegment = time;
                            ConfigService.Save();
                        }
                    }

                    ImGui.TableNextColumn();
                    ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
                    var bestSplit = split.BestSplit.FormatTimeHHMMSS();
                    if (ImGui.InputText($"##{split.GetHashCode()}_best_split", ref bestSplit, 256, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        if (TryParseTimeInput(bestSplit, out var time))
                        {
                            splitTemplate[j].BestSplit = time;
                            ConfigService.Save();
                        }
                    }

                    ImGui.TableNextColumn();
                    // remove function
                    if (splitTemplate.Count == 1)
                    {
                        // grey out button if there is only one split
                        ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 1));
                        ImGui.Button($"Remove##{split.GetHashCode()}");
                        ImGui.PopStyleColor();
                    }
                    else if (ImGui.Button($"Remove##{split.GetHashCode()}"))
                    {
                        // if there is only one split, do not remove it
                        if (splitTemplate.Count == 1)
                        {
                            break;
                        }
                        splitTemplate.RemoveAt(j);
                        ConfigService.Save();
                        break;
                    }
                }

                // row to add new split
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TableNextColumn();
                ImGui.TableNextColumn();
                ImGui.Text("00h00m00s");
                ImGui.TableNextColumn();
                ImGui.Text("00h00m00s.000");
                ImGui.TableNextColumn();
                ImGui.Text("00h00m00s.000");
                ImGui.TableNextColumn();
                if (ImGui.Button($"Add Split##{profile.GetHashCode()}"))
                {
                    splitTemplate.Add(new Split());
                    ConfigService.Save();
                }

                ImGui.EndTable();
            }

            if (ImGui.Button($"Duplicate##{profile.GetHashCode()}"))
            {
                // check if name exists, add number if it does
                int j = 1;
                string newName = $"{name} ({j})";
                while (config.SplitCollection.ContainsKey(newName))
                {
                    newName = $"{name} ({j})";
                    j++;
                }

                // shallow copy splits
                var newSplits = new List<Split>();
                foreach (var split in splitTemplate)
                {
                    newSplits.Add(split.CloneSplit());
                }

                config.SplitCollection[newName] = new SplitProfile()
                {
                    Template = newSplits,
                    History = new Dictionary<DateTime, List<Split>>()
                };
                ConfigService.Save();
            }


            // align right
            ImGui.SameLine();
            // grey out button if there is only one split
            if (config.SplitCollection.Count == 1)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 1));
                ImGui.Button($"Remove##{profile.GetHashCode()}");
                ImGui.PopStyleColor();
            }
            else if (ImGui.Button($"Remove##{profile.GetHashCode()}"))
            {
                config.SplitCollection.Remove(profileName);
                // update current profile if it was removed
                if (config.CurrentProfile == profileName)
                {
                    config.CurrentProfile = config.SplitCollection.Keys.First();
                }
                ConfigService.Save();
                return;
            }
        }

        // TODO: Make this not an ugly pos
        private bool TryParseTimeInput(string input, out TimeSpan time)
        {
            time = TimeSpan.Zero;

            if (string.IsNullOrWhiteSpace(input))
            {
                return true;
            }

            // ensure parsing as hh:mm:ss, mm:ss, or ss, can also include fractional seconds ie .f .ff .fff
            // allowed h, hh, m, mm, s, ss, f, ff, fff
            var hours = new string[] { @"h\:", @"hh\:", @"h\h", @"hh\h" };
            var minutes = new string[] { @"m\:", @"mm\:", @"m\m", @"mm\m" };
            var seconds = new string[] { @"s", @"ss", @"s\s", @"ss\s" };
            var fractions = new string[] { @"\.f", @"\.ff", @"\.fff", @"\.f\s", @"\.ff\s", @"\.fff\s" };

            // combine all possible formats
            foreach (string h in hours)
            foreach (string m in minutes)
            foreach (string s in seconds)
            foreach (string f in fractions)
            {
                var formats = new string[] 
                { 
                    $@"{h}{m}{s}{f}", 
                    $@"{h}{m}{s}", 
                    $@"{m}{s}{f}", 
                    $@"{m}{s}",
                    $@"{s}{f}",
                    $@"{s}"
                };
                foreach (var format in formats)
                {
                    if (TimeSpan.TryParseExact(input, format, null, out time))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
