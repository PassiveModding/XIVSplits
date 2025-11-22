using Dalamud.Bindings.ImGui;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System;
using System.Numerics;
using XIVSplits.Config;

namespace XIVSplits.UI
{
    public class PluginUI : IDisposable
    {
        public PluginUI(IDalamudPluginInterface pluginInterface,
                        ConfigService configService,
                        ObjectivesConfig dutyObjectivesConfig,
                        LiveSplitConfig liveSplitConfig,
                        Splits splits,
                        SplitHistory splitHistory,
                        TimerWindow timerWindow,
                        IPluginLog pluginLog)
        {
            PluginInterface = pluginInterface;
            ConfigService = configService;
            DutyObjectivesConfig = dutyObjectivesConfig;
            LiveSplitConfig = liveSplitConfig;
            Splits = splits;
            SplitHistory = splitHistory;
            TimerWindow = timerWindow;
            PluginLog = pluginLog;
            PluginLog.Info("Initializing PluginUI");

            PluginInterface.UiBuilder.Draw += Draw;
            PluginInterface.UiBuilder.OpenConfigUi += () =>
            {
                ConfigService.Get().ShowSettings = true;
            };
        }

        public IDalamudPluginInterface PluginInterface { get; }
        public ConfigService ConfigService { get; }
        public ObjectivesConfig DutyObjectivesConfig { get; }
        public LiveSplitConfig LiveSplitConfig { get; }
        public Splits Splits { get; }
        public SplitHistory SplitHistory { get; }
        public TimerWindow TimerWindow { get; }
        public IPluginLog PluginLog { get; }

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
                PluginLog.Error(e, "Error in Draw");
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
                if (showSettings != config.ShowSettings)
                {
                    config.ShowSettings = showSettings;
                    ConfigService.Save();
                }

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
