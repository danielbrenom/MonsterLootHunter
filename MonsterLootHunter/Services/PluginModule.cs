using MonsterLootHunter.Helpers;
using MonsterLootHunter.Logic;

namespace MonsterLootHunter.Services;

public static class PluginModule
{
    public static void Register(PluginDependencyContainer container)
    {
        container.Register<ItemManagerService>()
                 .Register<MapManagerService>()
                 .Register<ImageService>()
                 .Register<MaterialTableRenderer>()
                 .Register<WikiClient>()
                 .Register<ContextMenu>()
                 .Register<GarlandClient>();
    }
}
