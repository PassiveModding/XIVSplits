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
            if (ImGui.Begin($"XIVSplits Timer", ref showSettings))
            {
                config.ShowTimer = showSettings;

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


                // table for splits
                // fit content, do not expand Y
                var currentProfile = config.GetCurrentProfile();
                if (ImGui.BeginTable("Splits", 10, ImGuiTableFlags.Borders | ImGuiTableFlags.Hideable))
                {
                    // word for current but shorter "cur"
                    ImGui.TableSetupColumn("Cur", ImGuiTableColumnFlags.WidthFixed, 20);
                    ImGui.TableSetupColumn("Name");
                    ImGui.TableSetupColumn("Tracked");
                    ImGui.TableSetupColumn("Segment", ImGuiTableColumnFlags.WidthFixed, 60);
                    ImGui.TableSetupColumn("Game", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, 60);
                    ImGui.TableSetupColumn("Actual", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, 60);
                    ImGui.TableSetupColumn("Split", ImGuiTableColumnFlags.WidthFixed, 60);
                    ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 60);
                    ImGui.TableSetupColumn("Best", ImGuiTableColumnFlags.WidthFixed, 60); // use best parsed segment
                    ImGui.TableSetupColumn("Best Split", ImGuiTableColumnFlags.WidthFixed, 60);
                    ImGui.TableHeadersRow();


                    // sum of splits to keep track of total
                    TimeSpan sum = TimeSpan.Zero;
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
                            ImGui.Text(split.Segment.ToString("mm\\:ss\\.ff"));
                            // hover text to identify the split
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.SetTooltip("Showing actual time since the segment could not be parsed from chat messages.");
                            }
                        }
                        else
                        {
                            ImGui.Text(split.SegmentParsed.ToString("mm\\:ss"));
                        }

                        ImGui.TableNextColumn();
                        DrawStyledText(split.SegmentParsed, split.BestSegmentParsed, "mm\\:ss\\.ff");
                        ImGui.TableNextColumn();
                        DrawStyledText(split.Segment, split.BestSegment, "mm\\:ss\\.ff");
                        ImGui.TableNextColumn();
                        ImGui.Text(split.SplitTime.ToString("mm\\:ss\\.ff"));
                        ImGui.TableNextColumn();
                        sum += split.Total;
                        ImGui.Text(sum.ToString("mm\\:ss\\.ff"));
                        ImGui.TableNextColumn();
                        ImGui.Text(split.BestSegmentParsed.ToString("mm\\:ss\\.ff"));
                        ImGui.TableNextColumn();
                        ImGui.Text(split.BestSegment.ToString("mm\\:ss\\.ff"));
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

                ImGui.Text($"Real Time: {InternalTimer.RealTime.Elapsed:mm\\:ss\\.ff}");
                ImGui.Text($"Segment Time: {InternalTimer.SegmentTime.Elapsed:mm\\:ss\\.ff}");
                ImGui.Text($"Actual Segments (Total): {sumActualSegment:mm\\:ss\\.ff}");
                ImGui.Text($"Game Segments (Total): {sumParsedSegment:mm\\:ss}");

                ImGui.End();
            }
        }

        // helper function to colour text based on whether the current split is better than the best split
        private void DrawStyledText(TimeSpan segment, TimeSpan compare, string format)
        {
            if (compare == TimeSpan.Zero)
            {
                // white
                ImGui.Text(segment.ToString(format));
            }
            else if (segment < compare)
            {
                // green
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 1, 0, 1));
                ImGui.Text(segment.ToString(format));
                ImGui.PopStyleColor();
            }
            else if (segment > compare)
            {
                // red
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0, 0, 1));
                ImGui.Text(segment.ToString(format));
                ImGui.PopStyleColor();
            }
            else
            {
                // white
                ImGui.Text(segment.ToString(format));
            }
        }

    }
}
