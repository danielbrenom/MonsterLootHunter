using System;
using Dalamud;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace MonsterLootHunter.Logic;

public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 114;
    public bool ContextMenuIntegration { get; set; } = true;
    public bool UseLegacyViewer { get; set; }
    private bool ClientUsingAnotherLanguage { get; set; }

    [NonSerialized] private DalamudPluginInterface _pluginInterface;

    public void Initialize(DalamudPluginInterface pluginInterface, ClientLanguage language)
    {
        _pluginInterface = pluginInterface;
        ClientUsingAnotherLanguage = language != ClientLanguage.English;
    }

    public bool UsingAnotherLanguage() => ClientUsingAnotherLanguage;

    public void Save()
    {
        _pluginInterface!.SavePluginConfig(this);
    }
}