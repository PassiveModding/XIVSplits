using Dalamud.Logging;
using Dalamud.Plugin.Services;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using XIVSplits.Config;

namespace XIVSplits.Timers
{
    public class LiveSplit : IDisposable
    {
        public LiveSplit(ConfigService configService, IPluginLog pluginLog)
        {
            ConfigService = configService;
            PluginLog = pluginLog;
        }

        private TcpClient? _client = null;
        public bool Connected => _client?.Connected ?? false;
        public bool Connecting = false;

        public ConfigService ConfigService { get; }
        public IPluginLog PluginLog { get; }

        public async Task ConnectAsync()
        {
            try
            {
                PluginLog.Information("Connecting to LiveSplit");
                if (Connected)
                {
                    _client?.Close();
                    _client = null;
                    PluginLog.Debug("Closed existing connection to LiveSplit");
                }

                if (_client == null)
                {
                    Connecting = true;
                    _client = new TcpClient();
                    Config.Config config = ConfigService.Get();
                    await _client.ConnectAsync(config.LiveSplitServer, config.LiveSplitPort);
                    PluginLog.Information("Connected to LiveSplit");
                }

                Connecting = false;
            }
            catch (Exception e)
            {
                Connecting = false;
                _client?.Close();
                _client = null;
                PluginLog.Error(e, "Failed to connect to LiveSplit");
            }
        }

        public void Disconnect()
        {
            if (!Connected)
            {
                return;
            }
            _client?.Close();
            _client = null;

            PluginLog.Information("Disconnected from LiveSplit");
        }

        public void Send(string message)
        {
            try
            {
                if (_client == null)
                {
                    PluginLog.Debug("Skipping sending message to LiveSplit because we're not connected");
                    return;
                }

                PluginLog.Debug($"Sending message to LiveSplit: {message}");
                byte[] data = Encoding.UTF8.GetBytes($"{message}\r\n");
                NetworkStream stream = _client.GetStream();
                stream.Write(data);
            }
            catch (Exception e)
            {
                PluginLog.Error(e, "Failed to send message to LiveSplit");
            }
        }

        public void Dispose()
        {
            PluginLog.Debug("Disposing LiveSplit");
            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }
        }
    }
}