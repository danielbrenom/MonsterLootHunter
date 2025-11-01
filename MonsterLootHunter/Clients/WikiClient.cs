using Dalamud.Plugin.Services;
using HtmlAgilityPack;
using MonsterLootHunter.Data;
using MonsterLootHunter.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MonsterLootHunter.Clients;

public class WikiClient(IPluginLog pluginLog, HttpClient httpClient)
{
    public async Task<LootData> GetLootData(LootData data, CancellationToken cancellationToken)
    {
        if (_itemNameFix.TryGetValue(data.LootName, out var fixedName))
            data.LootName = fixedName;

        var uri = new UriBuilder(string.Format(PluginConstants.WikiBaseUrl, data.LootName.Replace(" ", "_"))).ToString();
        var document = new HtmlDocument();
        var response = await httpClient.GetAsync(uri, cancellationToken);
        var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
        var jsonResponse = JsonConvert.DeserializeObject<JObject>(responseString);
        var htmlText = jsonResponse?["parse"]?["text"]?["*"];
        if (htmlText is null)
            return data;

        document.LoadHtml(htmlText.ToString());

        try
        {
            return await WikiParser.ParseResponse(document, data).WaitAsync(cancellationToken);
        }
        catch (Exception e)
        {
            pluginLog.Error("{0}\n{1}", e.Message, e.StackTrace ?? string.Empty);
            return data;
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
