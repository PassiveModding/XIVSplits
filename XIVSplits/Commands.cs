using Dalamud.Game.Command;
using Dalamud.Logging;
using System;
using System.Collections.Generic;
using XIVSplits.Config;

namespace XIVSplits
{
    public class Commands : IDisposable
    {
        private Dictionary<string, CommandInfo> CommandCollection { get; init; }

        public CommandManager CommandManager { get; }

        public Commands(ConfigService configService, CommandManager commandManager)
        {
            CommandManager = commandManager;
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
            PluginLog.LogDebug("Disposing Commands");
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
                    PluginLog.LogDebug($"Removing command: {command.Key}");
                    CommandManager.RemoveHandler(command.Key);
                }
            }
        }
    }
}
