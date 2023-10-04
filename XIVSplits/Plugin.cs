using Dalamud.Data;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
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
            ICommandManager commands,
            IChatGui chat,
            IGameGui gameGui,
            IDataManager dataManager,
            IPluginLog log)
        {
            ServiceCollection serviceCollection = new();
            serviceCollection.AddSingleton(pluginInterface);
            serviceCollection.AddSingleton(commands);
            serviceCollection.AddSingleton(chat);
            serviceCollection.AddSingleton(gameGui);
            serviceCollection.AddSingleton(dataManager);
            serviceCollection.AddSingleton(log);

            ConfigService configService = new(pluginInterface, log);
            configService.Load();
            serviceCollection.AddSingleton(configService);
            ConfigureServices(serviceCollection, log);
            serviceProvider = serviceCollection.BuildServiceProvider();
            pluginInterface.UiBuilder.DisableAutomaticUiHide = true;

            // need to initialize these after the service provider is built
            // livesplit, internaltimer, objectivemanager, commands, pluginui
            serviceProvider.GetRequiredService<LiveSplit>();
            serviceProvider.GetRequiredService<InternalTimer>();
            serviceProvider.GetRequiredService<ObjectiveManager>();
            serviceProvider.GetRequiredService<Commands>();
            serviceProvider.GetRequiredService<PluginUI>();
        }

        private static void ConfigureServices(IServiceCollection services, IPluginLog log)
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
                    log.Verbose($"Adding UI Component: {type.Name}");
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