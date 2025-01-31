using Dalamud.Interface;
using ImGuiNET;
using System;
using System.Numerics;
using System.Threading.Tasks;
using XIVSplits.Config;
using XIVSplits.Models;
using XIVSplits.Timers;

namespace XIVSplits.UI
{
    public class TimerWindow : IPluginUIComponent
    {
        public TimerWindow(ConfigService configService, InternalTimer internalTimer, LiveSplit liveSplit)
        {
            ConfigService = configService;
            InternalTimer = internalTimer;
            LiveSplit = liveSplit;
        }

        public ConfigService ConfigService { get; }
        public InternalTimer InternalTimer { get; }
        public LiveSplit LiveSplit { get; }

        public void Draw()
        {
            Config.Config config = ConfigService.Get();
            bool showSettings = config.ShowTimer;
            if (!showSettings) return;

            ImGui.SetNextWindowSize(new Vector2(700, 500), ImGuiCond.FirstUseEver);
            if (ImGui.Begin($"{config.CurrentProfile}###xivsplitstimer", ref showSettings))
            {
                if (showSettings != config.ShowTimer)
                {
                    config.ShowTimer = showSettings;
                    ConfigService.Save();
                }

                // start/split button
                if (ImGui.Button("Start"))
                {
                    InternalTimer.Start();
                }

                // split button
                ImGui.SameLine();
                if (ImGui.Button("Split"))
                {
                    InternalTimer.ManualSplit();
                }

                // reset button
                ImGui.SameLine();
                if (ImGui.Button("Reset"))
                {
                    InternalTimer.Stop();
                }

                ImGui.SameLine();
                if (ImGui.Button("Reset Splits"))
                {
                    InternalTimer.ResetSplits();
                }

                // icon to connect livesplit
                ImGui.SameLine();
                if (LiveSplit.Connected)
                {
                    // disconnect icon button
                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button(FontAwesomeIcon.Link.ToIconString()))
                    {
                        LiveSplit.Disconnect();
                    }
                    ImGui.PopFont();

                    // hover text to identify the button
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Disconnect from LiveSplit");
                    }
                }
                else if (!LiveSplit.Connecting)
                {
                    // connect icon button
                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button(FontAwesomeIcon.Unlink.ToIconString()))
                    {
                        Task _ = Task.Run(LiveSplit.ConnectAsync);
                    }
                    ImGui.PopFont();

                    // hover text to identify the button
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Connect to LiveSplit");
                    }
                }
                else
                {
                    // connecting icon
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.Button(FontAwesomeIcon.Sync.ToIconString());
                    ImGui.PopFont();

                    // hover text to identify the button
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Connecting to LiveSplit...");
                    }
                }

                // icon button to open settings
                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button(FontAwesomeIcon.Cog.ToIconString()))
                {
                    config.ShowSettings = true;
                }
                ImGui.PopFont();


                // table for splits
                // fit content, do not expand Y
                var currentProfile = config.GetCurrentProfile();
                TimeSpan sumOfSplits = TimeSpan.Zero;

                if (ImGui.BeginTable("Splits", 11, ImGuiTableFlags.Borders | ImGuiTableFlags.Hideable | ImGuiTableFlags.Reorderable | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
                {


                    // word for current but shorter "cur"
                    ImGui.TableSetupColumn("Cur");
                    ImGui.TableSetupColumn("Name");
                    ImGui.TableSetupColumn("Tracked");
                    ImGui.TableSetupColumn("Segment");
                    ImGui.TableSetupColumn("Game");
                    ImGui.TableSetupColumn("Actual");
                    ImGui.TableSetupColumn("Split");
                    ImGui.TableSetupColumn("Time");
                    ImGui.TableSetupColumn("Best Game"); // use best parsed segment
                    ImGui.TableSetupColumn("Best Segment");
                    ImGui.TableSetupColumn("Best Split");

                    ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
                    SetColumnHover(0, "Cur", "Current split");
                    SetColumnHover(1, "Name", "Name of the split");
                    SetColumnHover(2, "Tracked", "Objective tracked");
                    SetColumnHover(3, "Segment", "Game time or actual time if game time is not tracked");
                    SetColumnHover(4, "Game", "Duty completion time from chat");
                    SetColumnHover(5, "Actual", "From start of combat to kill or otherwise split time");
                    SetColumnHover(6, "Split", "Time from last split to this split");
                    SetColumnHover(7, "Time", "Total time since the start of the run");
                    SetColumnHover(8, "Best Game", "Best segment time from game");
                    SetColumnHover(9, "Best Segment", "Best segment time");
                    SetColumnHover(10, "Best Split", "Best split time");

                    for (int i = 0; i < currentProfile.Template.Count; i++)
                    {
                        Split? split = currentProfile.Template[i];
                        if (split == null)
                        {
                            split = new Split();
                            currentProfile.Template[i] = split;
                        }
                        ImGui.TableNextRow();

                        ImGui.TableNextColumn();

                        // Show an icon next to the current split
                        if (i == InternalTimer.CurrentSplitIndex)
                        {
                            ImGui.PushFont(UiBuilder.IconFont);
                            ImGui.Text(FontAwesomeIcon.Play.ToIconString());
                            ImGui.PopFont();
                        }

                        ImGui.TableNextColumn();
                        // fit width
                        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                        ImGui.Text(split.Name);
                        ImGui.TableNextColumn();
                        ImGui.Text(split.Objective);
                        ImGui.TableNextColumn();


                        // combine parsed and actual based on whether parsed is set
                        if (split.SegmentParsed == TimeSpan.Zero)
                        {
                            DrawStyledText(split.Segment, split.BestSegment);
                            // hover text to identify the split
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.SetTooltip("Showing actual time since the segment could not be parsed from chat messages.");
                            }
                        }
                        else
                        {
                            DrawStyledText(split.SegmentParsed, split.BestSegmentParsed);
                        }

                        ImGui.TableNextColumn();
                        DrawStyledText(split.SegmentParsed, split.BestSegmentParsed);
                        ImGui.TableNextColumn();
                        DrawStyledText(split.Segment, split.BestSegment);
                        ImGui.TableNextColumn();
                        DrawStyledText(split.SplitTime, split.BestSplit);
                        ImGui.TableNextColumn();
                        // calculated from splits instead of using split.Total so we can account for the user reordering splits
                        if (split.SplitTime != TimeSpan.Zero)
                        {
                            sumOfSplits += split.SplitTime;
                            ImGui.Text(sumOfSplits.FormatTime());
                        }
                        else
                        {
                            // forecast potential time save
                            sumOfSplits += split.BestSplit;
                            ImGui.Text(sumOfSplits.FormatTime());
                        }
                        ImGui.TableNextColumn();
                        ImGui.Text(split.BestSegmentParsed.FormatTime());
                        ImGui.TableNextColumn();
                        ImGui.Text(split.BestSegment.FormatTime());
                        ImGui.TableNextColumn();
                        ImGui.Text(split.BestSplit.FormatTime());
                    }

                    ImGui.EndTable();
                }

                TimeSpan sumParsedSegment = TimeSpan.Zero;
                TimeSpan sumActualSegment = TimeSpan.Zero;

                foreach (Split split in currentProfile.Template)
                {
                    sumParsedSegment += split.SegmentParsed;
                    sumActualSegment += split.Segment;
                }

                ImGui.Text($"Real Time: {InternalTimer.RealTime.Elapsed.FormatTime()}");
                ImGui.Text($"Segment Time: {InternalTimer.SegmentTime.Elapsed.FormatTime()}");
                ImGui.Text($"Actual Segments (Total): {sumActualSegment.FormatTime()}");
                ImGui.Text($"Game Segments (Total): {sumParsedSegment.FormatTime(false)}");
                ImGui.Text($"Forecast: {sumOfSplits.FormatTime()}");

                ImGui.End();
            }
        }

        private void SetColumnHover(int index, string columnName, string description)
        {
            // on hover of each column, explain and show full name
            ImGui.TableSetColumnIndex(index);
            ImGui.PushID(columnName);
            ImGui.TableHeader(columnName);
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(description);
            }
            ImGui.PopID();
        }

        // helper function to colour text based on whether the current split is better than the best split
        private void DrawStyledText(TimeSpan segment, TimeSpan compare)
        {
            if (compare == TimeSpan.Zero || segment == TimeSpan.Zero)
            {
                // white
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 1, 1, 1));
                ImGui.Text(segment.FormatTime());
                ImGui.PopStyleColor();
            }
            else if (segment <= compare)
            {
                // green
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 1, 0, 1));
                ImGui.Text(segment.FormatTime());
                ImGui.PopStyleColor();
            }
            else if (segment > compare)
            {
                // red
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0, 0, 1));
                ImGui.Text(segment.FormatTime());
                ImGui.PopStyleColor();
            }
            else
            {
                // white
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 1, 1, 1));
                ImGui.Text(segment.FormatTime());
                ImGui.PopStyleColor();
            }
        }

    }
}
