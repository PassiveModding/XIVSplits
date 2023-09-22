using Dalamud.Logging;
using Dalamud.Plugin;
using ImGuiNET;
using System;
using System.Numerics;
using XIVSplits.Config;

namespace XIVSplits.UI
{
    public class PluginUI : IDisposable
    {
        public PluginUI(DalamudPluginInterface pluginInterface,
                        ConfigService configService,
                        ObjectivesConfig dutyObjectivesConfig,
                        LiveSplitConfig liveSplitConfig,
                        Splits splits,
                        SplitHistory splitHistory,
                        TimerWindow timerWindow)
        {
            PluginInterface = pluginInterface;
            ConfigService = configService;
            DutyObjectivesConfig = dutyObjectivesConfig;
            LiveSplitConfig = liveSplitConfig;
            Splits = splits;
            SplitHistory = splitHistory;
            TimerWindow = timerWindow;
            PluginLog.Log("Initializing PluginUI");

            PluginInterface.UiBuilder.Draw += Draw;
            PluginInterface.UiBuilder.OpenConfigUi += () =>
            {
                ConfigService.Get().ShowSettings = true;
            };
        }

        public DalamudPluginInterface PluginInterface { get; }
        public ConfigService ConfigService { get; }
        public ObjectivesConfig DutyObjectivesConfig { get; }
        public LiveSplitConfig LiveSplitConfig { get; }
        public Splits Splits { get; }
        public SplitHistory SplitHistory { get; }
        public TimerWindow TimerWindow { get; }

        public void Dispose()
        {
            PluginInterface.UiBuilder.Draw -= Draw;
        }

        public void Draw()
        {
            try
            {
                DrawConfig();
                TimerWindow.Draw();
            }
            catch (Exception e)
            {
                PluginLog.LogError(e, "Error in Draw");
            }
        }

        public void DrawConfig()
        {
            Config.Config config = ConfigService.Get();
            bool showSettings = config.ShowSettings;
            if (!showSettings) return;

            ImGui.SetNextWindowSize(new Vector2(700, 500), ImGuiCond.FirstUseEver);
            if (ImGui.Begin($"XIVSplits Settings###xivsplitsconfig", ref showSettings))
            {
                config.ShowSettings = showSettings;

                if (ImGui.BeginTabBar("ConfigMenuBar###xivsplitsconfigmenubar"))
                {
                    if (ImGui.BeginTabItem("LiveSplit###xivsplitsmainconfigtab"))
                    {
                        LiveSplitConfig.Draw();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Splits###xivsplitssetup"))
                    {
                        Splits.Draw();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Duty Objectives###xivdutyobjectives"))
                    {
                        DutyObjectivesConfig.Draw();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Split History###xivsplitshistory"))
                    {
                        SplitHistory.Draw();
                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }

                ImGui.End();
            }
        }
    }
}
