using Lumina.Excel.Sheets;
using MonsterLootHunter.Clients;
using MonsterLootHunter.Data;
using MonsterLootHunter.Logic;
using MonsterLootHunter.Utils;

namespace MonsterLootHunter.Services;

public class ItemFetchService(Configuration configuration, GarlandClient garlandClient, WikiClient wikiClient, GatheringNodesService gatheringNodesService, FileUtils fileUtils)
{
    private const string StoredFileName = "stored_loot_data.cache";
    private StoredLootData _storedLootData = new();

    public async Task<LootData> FetchLootData(Item item, CancellationToken token)
    {
        var itemName = await CheckItemName(item, token);
        var lootData = new LootData(itemName);

        if (_storedLootData.HasAnyData &&
            _storedLootData.StoredData.TryGetValue(itemName, out var cachedLootData) &&
            cachedLootData.HasLocations &&
            cachedLootData.ContainsWikiData)
            return cachedLootData;

        if (configuration.PreferWikiData)
        {
            lootData = await wikiClient.GetLootData(lootData, token).ConfigureAwait(false);
            if (configuration.AppendInternalData || lootData.LootLocations.Count == 0 || lootData.LootLocations.Any(l => string.IsNullOrEmpty(l.MobLocation)))
                await gatheringNodesService.CheckGatherable(item.RowId, lootData);

            lootData.ContainsWikiData = true;
            _storedLootData.StoredData.TryAdd(itemName, lootData);
            return lootData;
        }

        await gatheringNodesService.CheckGatherable(item.RowId, lootData);

        if (!lootData.HasLocations)
        {
            lootData.ContainsWikiData = true;
            lootData = await wikiClient.GetLootData(lootData, token).ConfigureAwait(false);
        }

        _storedLootData.StoredData.TryAdd(itemName, lootData);

        return lootData;
    }

    public void LoadStoredLootData()
    {
        var persistedData = fileUtils.LoadPersistentDataFromFile<StoredLootData>(StoredFileName);
        _storedLootData = persistedData;
    }

    public void SaveStoredLootData() => fileUtils.PersistDataOnFile(StoredFileName, _storedLootData);

    public void ClearStoredLootData() => _storedLootData = new StoredLootData()
    {
        StoredData = new Dictionary<string, LootData>(),
        NormalizedNames = new Dictionary<uint, string>()
    };

    private async Task<string> CheckItemName(Item item, CancellationToken token)
    {
        if (!configuration.UsingAnotherLanguage)
            return item.Name.ToString();

        if (_storedLootData.NormalizedNames.TryGetValue(item.RowId, out var cachedName))
            return cachedName;

        var normalizedItemName = await garlandClient.GetItemName(item.RowId, token);
        _storedLootData.NormalizedNames.TryAdd(item.RowId, normalizedItemName);
        return normalizedItemName;
    }
}
