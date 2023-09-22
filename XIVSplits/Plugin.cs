using Dalamud.Data;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Logging;
using Dalamud.Plugin;
using Microsoft.Extensions.DependencyInjection;
using XIVSplits.Config;
using XIVSplits.Timers;
using XIVSplits.UI;

namespace XIVSplits
{
    public class Plugin : IDalamudPlugin
    {
        public string Name => "XIVSplits";

        private ServiceProvider serviceProvider { get; }

        public Plugin(DalamudPluginInterface pluginInterface,
            CommandManager commands,
            ChatGui chat,
            GameGui gameGui,
            DataManager dataManager)
        {
            ServiceCollection serviceCollection = new();
            serviceCollection.AddSingleton(pluginInterface);
            serviceCollection.AddSingleton(commands);
            serviceCollection.AddSingleton(chat);
            serviceCollection.AddSingleton(gameGui);
            serviceCollection.AddSingleton(dataManager);
            ConfigService configService = new(pluginInterface);
            configService.Load();
            serviceCollection.AddSingleton(configService);
            ConfigureServices(serviceCollection);
            serviceProvider = serviceCollection.BuildServiceProvider();
            pluginInterface.UiBuilder.DisableAutomaticUiHide = true;

            // This is temporary I swear
            LiveSplit liveSplit = serviceProvider.GetRequiredService<LiveSplit>();
            InternalTimer internalTimer = serviceProvider.GetRequiredService<InternalTimer>();
            ObjectiveManager objectiveManager = serviceProvider.GetRequiredService<ObjectiveManager>();
            Commands commandsHandler = serviceProvider.GetRequiredService<Commands>();
            PluginUI pluginUI = serviceProvider.GetRequiredService<PluginUI>();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<LiveSplit>();
            services.AddSingleton<InternalTimer>();
            services.AddSingleton<ObjectiveManager>();
            services.AddSingleton<Commands>();

            // UI Setup
            // get all implementations of IPluginUIComponent
            foreach (System.Type type in typeof(PluginUI).Assembly.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface) continue;
                if (typeof(IPluginUIComponent).IsAssignableFrom(type))
                {
                    PluginLog.LogVerbose($"Adding UI Component: {type.Name}");
                    services.AddSingleton(type);
                }
            }

            services.AddSingleton<PluginUI>();
        }

        public void Dispose()
        {
            serviceProvider.Dispose();
        }
    }
}