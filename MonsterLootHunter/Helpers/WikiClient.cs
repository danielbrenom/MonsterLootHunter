using Dalamud;
using HtmlAgilityPack;
using MonsterLootHunter.Data;
using MonsterLootHunter.Utils;

namespace MonsterLootHunter.Helpers;

public class WikiClient : IServiceType
{
    private readonly HtmlWeb _webClient = new();

    public async Task<LootData> GetLootData(string lootName, CancellationToken cancellationToken)
    {
        if (_itemNameFix.TryGetValue(lootName, out var fixedName))
            lootName = fixedName;
        var uri = new UriBuilder(string.Format(PluginConstants.WikiBaseUrl, lootName.Replace(" ", "_"))).ToString();
        var pageResponse = await _webClient.LoadFromWebAsync(uri, cancellationToken);

        try
        {
            return await WikiParser.ParseResponse(pageResponse, lootName).WaitAsync(cancellationToken);
        }
        catch (Exception)
        {
            return new LootData(lootName);
        }
    }

    private readonly Dictionary<string, string> _itemNameFix = new()
    {
        { "Blue Cheese", "Blue Cheese (Item)" }
    };
}
