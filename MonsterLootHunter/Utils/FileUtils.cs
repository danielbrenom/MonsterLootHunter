using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Newtonsoft.Json;

namespace MonsterLootHunter.Utils;

public class FileUtils(IDalamudPluginInterface pluginInterface, IPluginLog log)
{
    private FileInfo? ObtainSaveFile(string fileName)
    {
        var dir = new DirectoryInfo(pluginInterface.GetPluginConfigDirectory());

        if (dir.Exists)
            return new FileInfo(Path.Combine(dir.FullName, fileName));

        try
        {
            dir.Create();
        }
        catch (Exception e)
        {
            log.Error($"Could not create save directory at {dir.FullName}:\n{e}");
            return null;
        }

        return new FileInfo(Path.Combine(dir.FullName, fileName));
    }

    public void PersistDataOnFile<T>(string fileName, T data)
    {
        var file = ObtainSaveFile(fileName);

        if (file is null)
            return;

        try
        {
            var text = JsonConvert.SerializeObject(data, Formatting.None);
            File.WriteAllText(file.FullName, text);
        }
        catch (Exception e)
        {
            log.Error($"Could not write cache file {file.FullName}:\n{e}");
        }
    }

    public T LoadPersistentDataFromFile<T>(string fileName) where T : class, new()
    {
        var file = ObtainSaveFile(fileName);

        if (file is not { Exists: true })
        {
            log.Warning($"No persist file found for {fileName}");
            return new T();
        }

        try
        {
            var dataText = File.ReadAllText(file.FullName);
            var persistedData = JsonConvert.DeserializeObject<T>(dataText);

            if (persistedData is not null)
                return persistedData;

            log.Error($"Could not load persisted data from file {file.FullName}");
            return new T();
        }
        catch (Exception e)
        {
            log.Error($"Could not load persisted data from file {file.FullName}:\n{e}");
            return new T();
        }
    }
}
