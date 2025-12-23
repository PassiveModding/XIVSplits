using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Text;
using XIVSplits.Timers;
using static FFXIVClientStructs.FFXIV.Client.Game.UI.ContentFinderConditionInterface.Delegates;
using GameQuestManager = FFXIVClientStructs.FFXIV.Client.Game.QuestManager;

namespace XIVSplits
{
    public class QuestManager : IDisposable
    {
        public IPluginLog PluginLog { get; set; }
        public IDataManager DataManager { get; set; }
        public IFramework Framework { get; set; }
        public InternalTimer InternalTimer { get; }
        public LiveSplit LiveSplit { get; }

        HashSet<ushort> previousQuests = new();

        public QuestManager(IPluginLog pluginLog, IDataManager dataManager, IFramework framework, InternalTimer internalTimer, LiveSplit liveSplit)
        {
            PluginLog = pluginLog;
            DataManager = dataManager;
            Framework = framework;
            InternalTimer = internalTimer;
            LiveSplit = liveSplit;

            // initial snapshot this is on plugin load which is likely way too early
            // but since we are calling it on framework tick it will be called properly before it actually matters
            previousQuests = SnapshotQuestState(); 

            this.Framework.Update += OnFrameworkTick;
        }

        private void OnFrameworkTick(IFramework framework)
        {
            HashSet<ushort> currentQuests = SnapshotQuestState();
            if (currentQuests == null || previousQuests == null)
                return;

            foreach (ushort quest in previousQuests)
            {
                if (!currentQuests.Contains(quest))
                {
                    //TODO add config for this currently just splitting on all quest completions
                    var name = DataManager.GetExcelSheet<Quest>()?.GetRowOrDefault((uint)(quest + 65536))?.Name;
                    PluginLog.Information($"quest completed: {name}");
                    InternalTimer.ManualSplit(name.ToString());
                    LiveSplit.Send("split");
                }
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
                var name = DataManager.GetExcelSheet<Quest>()?.GetRowOrDefault((uint)(quest + 65536))?.Name;
                PluginLog.Information($"name: {name} internal-id: {quest}");
            }
        }

        public void Dispose()
        {
            this.Framework.Update -= OnFrameworkTick;
        }
    }
}
