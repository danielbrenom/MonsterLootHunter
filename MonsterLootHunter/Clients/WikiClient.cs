using Dalamud.Plugin.Services;
using HtmlAgilityPack;
using MonsterLootHunter.Data;
using MonsterLootHunter.Utils;

namespace MonsterLootHunter.Clients;

public class WikiClient(IPluginLog pluginLog)
{
    private readonly HtmlWeb _webClient = new();

    public async Task GetLootData(LootData data, CancellationToken cancellationToken)
    {
        if (_itemNameFix.TryGetValue(data.LootName, out var fixedName))
            data.LootName = fixedName;

        var uri = new UriBuilder(string.Format(PluginConstants.WikiBaseUrl, data.LootName.Replace(" ", "_"))).ToString();
        var pageResponse = await _webClient.LoadFromWebAsync(uri, cancellationToken);

        try
        {
            await WikiParser.ParseResponse(pageResponse, data).WaitAsync(cancellationToken);
        }
        catch (Exception e)
        {
            pluginLog.Error("{0}\n{1}", e.Message, e.StackTrace ?? string.Empty);
        }
    }

    private readonly Dictionary<string, string> _itemNameFix = new()
    {
        { "Blue Cheese", "Blue Cheese (Item)" },
        { "Gelatin", "Gelatin (Item)" },
        { "Leather", "Leather (Item)" },
        { "Morel", "Morel (Item)" }
    };
}
