using System;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MonsterLootHunter.Data;

namespace MonsterLootHunter.Helpers
{
    public static class ScrapperClient
    {
        public static async Task<LootData> GetLootData(string lootName, CancellationToken cancellationToken)
        {
            var client = new HtmlWeb();
            var uri = new UriBuilder($"https://ffxiv.consolegameswiki.com/wiki/{lootName.Replace(" ", "_")}").ToString();
            var pageResponse = await client.LoadFromWebAsync(uri, cancellationToken);
            return ScrapperSanitizer.ParseResponse(pageResponse, lootName);
        }
    }
}