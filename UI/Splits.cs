using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
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


            // dropdown, select from existing templates
            if (ImGui.BeginCombo("Template", config.CurrentProfile))
            {
                foreach (string template in config.SplitCollection.Keys)
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

            // add a new template
            if (ImGui.Button($"Add Template"))
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
                ConfigService.Save();
            }

            for (int collectionIndex = 0; collectionIndex < config.SplitCollection.Count; collectionIndex++)
            {
                var profile = config.SplitCollection.ElementAt(collectionIndex);
                var name = profile.Key;
                var splitTemplate = profile.Value.Template;

                if (!ImGui.CollapsingHeader(name))
                {
                    continue;
                }

                if (ImGui.InputText($"##{profile.GetHashCode()}_name", ref name, 256, ImGuiInputTextFlags.EnterReturnsTrue) && name != profile.Key)
                {
                    // check if name exists, add number if it does
                    int j = 1;
                    string newName = name.Trim();
                    while (config.SplitCollection.ContainsKey(newName))
                    {
                        newName = $"{name} ({j})";
                        j++;
                    }

                    config.SplitCollection.Remove(profile.Key);
                    config.SplitCollection[newName] = profile.Value;
                    ConfigService.Save();
                }

                ImGui.SameLine();
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
                if (ImGui.Button($"Remove##{profile.GetHashCode()}"))
                {
                    config.SplitCollection.Remove(profile.Key);
                    ConfigService.Save();
                    break;
                }

                // table for splits
                // fit content, do not expand Y
                if (ImGui.BeginTable("Split Template", 3, ImGuiTableFlags.Borders))
                {
                    ImGui.TableSetupColumn("Reorder", ImGuiTableColumnFlags.WidthFixed, 60);
                    ImGui.TableSetupColumn("Name");
                    ImGui.TableSetupColumn("Remove", ImGuiTableColumnFlags.WidthFixed, 60);
                    ImGui.TableHeadersRow();

                    for (int j = 0; j < splitTemplate.Count; j++)
                    {
                        var split = splitTemplate[j];
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();

                        // input index
                        var jText = j.ToString();
                        if (ImGui.InputText($"##{split.GetHashCode()}_index", ref jText, 256, ImGuiInputTextFlags.EnterReturnsTrue| ImGuiInputTextFlags.CharsDecimal) && int.Parse(jText) != j)
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
                    if (ImGui.Button($"Add Split##{profile.GetHashCode()}"))
                    {
                        splitTemplate.Add(new Split());
                        ConfigService.Save();
                    }

                    ImGui.EndTable();
                }
            }
        }
    }
}
