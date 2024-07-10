using Lumina.Excel.GeneratedSheets;
using MonsterLootHunter.Clients;
using MonsterLootHunter.Data;
using MonsterLootHunter.Logic;

namespace MonsterLootHunter.Services;

public class ItemFetchService(Configuration configuration, GarlandClient garlandClient, WikiClient wikiClient, GatheringNodesService gatheringNodesService)
{
    public async Task<LootData> FetchLootData(Item item, CancellationToken token)
    {
        var itemName = await CheckItemName(item, token);
        var lootData = new LootData(itemName);

        if (configuration.PreferWikiData)
        {
            await wikiClient.GetLootData(lootData, token).ConfigureAwait(false);
            if (configuration.AppendInternalData || lootData.LootLocations.Count == 0 || lootData.LootLocations.Any(l => string.IsNullOrEmpty(l.MobLocation)))
                await gatheringNodesService.CheckGatherable(item.RowId, lootData);

            return lootData;
        }

        await gatheringNodesService.CheckGatherable(item.RowId, lootData);

        if (lootData.LootLocations.Count == 0)
        {
            await wikiClient.GetLootData(lootData, token).ConfigureAwait(false);
        }

        return lootData;
    }

    private async Task<string> CheckItemName(Item item, CancellationToken token)
    {
        if (configuration.UsingAnotherLanguage)
            return await garlandClient.GetItemName(item.RowId, token);

        return item.Name.ToString();
    }
}
