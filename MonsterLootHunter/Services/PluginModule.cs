using MonsterLootHunter.Helpers;
using MonsterLootHunter.Logic;

namespace MonsterLootHunter.Services;

public class PluginModule : IModule
{
    public void Register(PluginServiceFactory container)
    {
        container.RegisterService<ItemManagerService>()
                 .RegisterService<MapManagerService>()
                 .RegisterService<WikiParser>()
                 .RegisterService<WikiClient>()
                 .RegisterService<ContextMenu>()
                 .RegisterService<GarlandClient>();
    }
}