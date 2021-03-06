using System;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace MonsterLootHunter
{
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        public bool ContextMenuIntegration { get; set; } = false;

        // the below exist just to make saving less cumbersome

        [NonSerialized]
        private DalamudPluginInterface _pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            _pluginInterface = pluginInterface;
        }

        public void Save()
        {
            _pluginInterface!.SavePluginConfig(this);
        }
    }
}