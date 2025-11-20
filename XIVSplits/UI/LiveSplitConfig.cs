using Dalamud.Bindings.ImGui;
using System.Threading.Tasks;
using XIVSplits.Config;
using XIVSplits.Timers;

namespace XIVSplits.UI
{
    public class LiveSplitConfig : IPluginUIComponent
    {
        public LiveSplitConfig(ConfigService configService, LiveSplit liveSplit)
        {
            LiveSplit = liveSplit;
            ConfigService = configService;
        }

        public LiveSplit LiveSplit { get; }
        public ConfigService ConfigService { get; }

        public void Draw()
        {
            Config.Config config = ConfigService.Get();
            // explain that you need to enable the server in LiveSplit
            ImGui.Text("You need to enable the server in LiveSplit for this to work.\n" +
                       "You can do this by going to \"Edit Layout\" and adding the \"Server\" component.\n" +
                       "Then, right click on the component and click \"Start Server\".\n" +
                       "You can also set the port if you want to, but the default is 16834.\n\n" +
                       "You must manually click connect in this plugin to connect to LiveSplit.");

            // only allow editing if not connected
            ImGuiInputTextFlags editflags = ImGuiInputTextFlags.ReadOnly;
            if (!LiveSplit.Connected)
            {
                editflags = ImGuiInputTextFlags.None;
            }

            string liveSplitServer = config.LiveSplitServer;
            if (ImGui.InputText("LiveSplit Server", ref liveSplitServer, 256, editflags) && liveSplitServer != config.LiveSplitServer)
            {
                config.LiveSplitServer = liveSplitServer;
                ConfigService.Save();
            }

            int liveSplitPort = config.LiveSplitPort;
            if (ImGui.InputInt("LiveSplit Port", ref liveSplitPort, 1, 100, flags: editflags) && liveSplitPort != config.LiveSplitPort)
            {
                config.LiveSplitPort = liveSplitPort;
                ConfigService.Save();
            }

            if (LiveSplit.Connected)
            {
                if (ImGui.Button($"Disconnect"))
                {
                    LiveSplit.Disconnect();
                }
            }
            else
            {
                if (ImGui.Button($"Connect"))
                {
                    Task _ = Task.Run(LiveSplit.ConnectAsync);
                }

                if (LiveSplit.Connecting)
                {
                    ImGui.SameLine();
                    ImGui.Text("Connecting...");
                }
            }

            ImGui.SameLine();
            ImGui.Text(LiveSplit.Connected ? $"Connected to {config.LiveSplitServer}:{config.LiveSplitPort}" : "Not connected");
        }
    }
}
