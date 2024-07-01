using Dalamud.Configuration;
using Dalamud.Game;
using Dalamud.Plugin;

namespace MonsterLootHunter.Logic;

public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 194;
    public bool ContextMenuIntegration { get; set; } = true;
    public bool UseLegacyViewer { get; set; }
    private bool ClientUsingAnotherLanguage { get; set; }
    public string? PluginLanguage { get; set; }
    public float MinimumWindowScale { get; set; } = 1f;
    public float MaximumWindowScale { get; set; } = 1.5f;

    [NonSerialized]
    private IDalamudPluginInterface? _pluginInterface;

    public void Initialize(IDalamudPluginInterface pluginInterface, ClientLanguage language)
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
