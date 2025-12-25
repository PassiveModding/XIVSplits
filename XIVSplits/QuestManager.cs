using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using XIVSplits.Config;
using XIVSplits.Timers;
using GameQuestManager = FFXIVClientStructs.FFXIV.Client.Game.QuestManager;

namespace XIVSplits
{
    public class QuestManager : IDisposable
    {
        public IPluginLog PluginLog { get; set; }
        public IDataManager DataManager { get; set; }
        public IFramework Framework { get; set; }
        public ConfigService ConfigService { get; }
        public InternalTimer InternalTimer { get; }
        public LiveSplit LiveSplit { get; }
        
        Lumina.Excel.ExcelSheet<Quest> questSheet { get; set; }

        HashSet<ushort> previousQuests = new();

        public QuestManager(IPluginLog pluginLog, IDataManager dataManager, IFramework framework, ConfigService configService, InternalTimer internalTimer, LiveSplit liveSplit)
        {
            PluginLog = pluginLog;
            DataManager = dataManager;
            Framework = framework;
            ConfigService = configService;
            InternalTimer = internalTimer;
            LiveSplit = liveSplit;
            questSheet = DataManager.GetExcelSheet<Quest>();

            // initial snapshot this is on plugin load which is certainly way too early
            // but since we are calling it on framework tick it should have been called properly before it actually matters
            previousQuests = SnapshotQuestState(); 

            this.Framework.Update += OnFrameworkTick;
        }

        private void OnFrameworkTick(IFramework framework)
        {
            HashSet<ushort> currentQuests = SnapshotQuestState();
            if (currentQuests == null || previousQuests == null)
                return;

            var config = ConfigService.Get();
            foreach (ushort quest in previousQuests)
            {
                //if our currentQuests still contains a quest from previousQuests ignore it it cant be complete yet
                if (currentQuests.Contains(quest))
                    continue;

                //if the quest isnt complete it must have been abandoned
                if (!GameQuestManager.IsQuestComplete(quest))
                    continue;

                //the 65536 offset between in-game quest IDs and Lumina rows
                uint luminaId = (uint)(quest + 65536);

                //null checking for the Quest sheet/row
                if (questSheet.GetRowOrDefault(luminaId) is not Quest questRow)
                    continue;

                //Check if this quest should trigger a split
                if (!config.EnableAllQuests && !config.SelectedQuestIds.Contains(luminaId))
                    continue;

                //get the questName and fire the split
                var name = questRow.Name.ToString();
                if (string.IsNullOrEmpty(name))
                    name = $"Unknown Quest [{luminaId}]";
                PluginLog.Information($"Quest completed: {name}");

                InternalTimer.ManualSplit(name);
                LiveSplit.Send("split");

            }

            previousQuests = currentQuests;
        }

        unsafe HashSet<ushort> SnapshotQuestState()
        {
            HashSet<ushort> currentQuests = new();

            var qm = GameQuestManager.Instance();
            if (qm == null)
                return currentQuests;

            foreach (var qw in qm->NormalQuests)
            {
                if (qw.QuestId != 0)
                    currentQuests.Add(qw.QuestId);
            }

            return currentQuests;
        }

        public void logCurrentlyTrackedQuestSequences()
        {
            if (previousQuests == null)
                return;

            foreach (ushort quest in previousQuests)
            {
                if (questSheet.GetRowOrDefault((uint)(quest + 65536)) is Quest questRow)
                {
                    PluginLog.Information($"name: {questRow.Name} internal-id: {quest}");
                }
            }
        }

        public void Dispose()
        {
            this.Framework.Update -= OnFrameworkTick;
        }
    }
}
