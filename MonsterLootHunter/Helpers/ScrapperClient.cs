using System;
using System.Threading;
using System.Threading.Tasks;
using Dalamud;
using HtmlAgilityPack;
using MonsterLootHunter.Data;
using MonsterLootHunter.Utils;

namespace MonsterLootHunter.Helpers;

public class ScrapperClient : IServiceType
{
    private readonly HtmlWeb _webClient;
    private readonly ScrapperSanitizer _scrapperSanitizer;

    public ScrapperClient(ScrapperSanitizer scrapperSanitizer)
    {
        _scrapperSanitizer = scrapperSanitizer;
        _webClient = new HtmlWeb();
    }

    public async Task<LootData> GetLootData(string lootName, CancellationToken cancellationToken)
    {
        var uri = new UriBuilder(string.Format(PluginConstants.WikiBaseUrl, lootName.Replace(" ", "_"))).ToString();
        var pageResponse = await _webClient.LoadFromWebAsync(uri, cancellationToken);
        try
        {
            return await _scrapperSanitizer.ParseResponse(pageResponse, lootName).WaitAsync(cancellationToken);
        }
        catch (Exception)
        {
            return new LootData(lootName);
        }
    }
}