﻿using Dalamud.Logging;
using Dalamud.Plugin;
using System;

namespace XIVSplits.Config
{
    public class ConfigService : IDisposable
    {
        public ConfigService(DalamudPluginInterface dalamudPluginInterface)
        {
            DalamudPluginInterface = dalamudPluginInterface;
        }

        private DalamudPluginInterface DalamudPluginInterface { get; }
        private Config _config = null!;

        public Config Load()
        {
            Dalamud.Configuration.IPluginConfiguration? config = DalamudPluginInterface.GetPluginConfig();
            if (config?.Version == Config.CurrentVersion)
            {
                _config = (Config)config;
                return _config;
            }

            _config = new Config();
            Save();
            return _config;
        }

        public Config Get()
        {
            if (_config == null || IsDisposed)
            {
                throw new ObjectDisposedException(nameof(ConfigService));
            }

            return _config;
        }

        public void Save()
        {
            try
            {
                Config config = Get();
                DalamudPluginInterface.SavePluginConfig(config);
            }
            catch (Exception e)
            {
                PluginLog.LogError(e, "Failed to save config");
            }
        }

        private bool IsDisposed = false;
        public void Dispose()
        {
            if (IsDisposed) return;
            Save();
            _config = null!;
            IsDisposed = true;
        }
    }
}
