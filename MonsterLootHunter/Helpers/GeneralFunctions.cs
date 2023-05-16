using System;
using System.IO;
using Dalamud.Logging;
using Dalamud.Plugin;
using MonsterLootHunter.Logic;

namespace MonsterLootHunter.Helpers;

public class GeneralFunctions
{
    private readonly DalamudPluginInterface _pluginInterface;
    public GeneralFunctions(DalamudPluginInterface pluginInterface)
    {
        _pluginInterface = pluginInterface;
    }
    
    public FileInfo ObtainSaveFile(string fileName)
    {
        var dir = new DirectoryInfo(_pluginInterface.GetPluginConfigDirectory());
        if (dir.Exists)
            return new FileInfo(Path.Combine(dir.FullName, fileName));

        try
        {
            dir.Create();
        }
        catch (Exception e)
        {
            PluginLog.Error($"Could not create save directory at {dir.FullName}:\n{e}");
            return null;
        }

        return new FileInfo(Path.Combine(dir.FullName, fileName));
    }
}