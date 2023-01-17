using MonsterLootHunter.Services;

namespace MonsterLootHunter;

public interface IModule
{
    void Register(PluginServiceFactory container);
}