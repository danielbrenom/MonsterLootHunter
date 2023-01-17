using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using Dalamud.Plugin;
using MonsterLootHunter.Data;

namespace MonsterLootHunter.Logic;

public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 114;
    public bool ContextMenuIntegration { get; set; } = true;
    public Dictionary<string, IEnumerable<ShopListItem>> ShoppingList { get; set; }

    [NonSerialized] private DalamudPluginInterface _pluginInterface;

    public void Initialize(DalamudPluginInterface pluginInterface)
    {
        _pluginInterface = pluginInterface;
    }

    public void Save()
    {
        _pluginInterface!.SavePluginConfig(this);
    }
}