using Dalamud.Plugin;
using MonsterLootHunter.Logic;

namespace MonsterLootHunter;

public static class PluginServices
{
    public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
    public static ItemManager ItemManager { get; private set; } = null!;
    public static Configuration Configuration { get; private set; } = null!;
    public static MapManager MapManager { get; private set; } = null!;

    public static void Initialize(DalamudPluginInterface pluginInterface)
    {
        PluginInterface = pluginInterface;
        ItemManager = new ItemManager();
        Configuration = (Configuration)PluginInterface.GetPluginConfig() ?? new Configuration();
        Configuration.Initialize(pluginInterface);
        MapManager = new MapManager();
    }

    public static void Dispose()
    {
        ItemManager = null;
        Configuration = null;
        MapManager = null;
    }
}