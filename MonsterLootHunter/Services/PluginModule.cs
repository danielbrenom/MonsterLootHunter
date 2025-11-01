using MonsterLootHunter.Clients;
using MonsterLootHunter.Logic;

namespace MonsterLootHunter.Services;

public static class PluginModule
{
    public static PluginDependencyContainer RegisterModules(this PluginDependencyContainer container)
    {
        container.Register<ItemManagerService>()
                 .Register<MapManagerService>()
                 .Register<ImageService>()
                 .Register<MaterialTableRenderer>()
                 .Register<WikiClient>()
                 .Register<ContextMenu>()
                 .Register<GarlandClient>()
                 .Register<ItemFetchService>()
                 .Register<GatheringNodesService>();
        return container;
    }
}
