using Dalamud.Game.Command;
using Dalamud.Logging;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using XIVSplits.Config;

namespace XIVSplits
{
    public class Commands : IDisposable
    {
        private Dictionary<string, CommandInfo> CommandCollection { get; init; }

        public ICommandManager CommandManager { get; }
        public IPluginLog PluginLog { get; }

        public Commands(ConfigService configService, ICommandManager commandManager, IPluginLog pluginLog)
        {
            CommandManager = commandManager;
            PluginLog = pluginLog;
            Config.Config config = configService.Get();
            CommandCollection = new Dictionary<string, CommandInfo>()
            {
                {
                    "/splitconfig", new CommandInfo((a,b) => config.ShowSettings = true)
                    {
                        HelpMessage = "Show split configuration UI"
                    }
                },
                {
                    "/splittimer", new CommandInfo((a,b) => config.ShowTimer = !config.ShowTimer)
                    {
                        HelpMessage = "Show split timers UI"
                    }
                }
            };

            foreach (KeyValuePair<string, CommandInfo> command in CommandCollection)
            {
                CommandManager.AddHandler(command.Key, command.Value);
            }
        }

        public void Dispose()
        {
            PluginLog.Debug("Disposing Commands");
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (CommandCollection == null) return;
                foreach (KeyValuePair<string, CommandInfo> command in CommandCollection)
                {
                    PluginLog.Debug($"Removing command: {command.Key}");
                    CommandManager.RemoveHandler(command.Key);
                }
            }
        }
    }
}
