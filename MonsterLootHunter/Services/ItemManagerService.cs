using Dalamud;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using MonsterLootHunter.Data;
using MonsterLootHunter.Utils;

namespace MonsterLootHunter.Services;

public class ItemManagerService : IServiceType
{
    private readonly IDataManager _dataManager;
    private readonly ExcelSheet<Item> _items;
    private readonly IPluginLog _pluginLog;
    private Dictionary<SearchCategories, List<Item>> CachedList { get; }

    public ItemManagerService(IDataManager dataManager, IPluginLog pluginLog)
    {
        _dataManager = dataManager;
        _pluginLog = pluginLog;
        _items = dataManager.GetExcelSheet<Item>();
        CachedList = SortCategoriesAndItems();
    }

    public bool CheckSelectedItem(uint itemId)
    {
        var foundItem = RetrieveItem(itemId);
        return foundItem is not null && CachedList.Any(pair => pair.Value.Any(item => item.RowId == foundItem.Value.RowId));
    }

    public Item? RetrieveItem(uint itemId) => _items.GetRowOrDefault(itemId);

    public (List<KeyValuePair<SearchCategories, List<Item>>>, string) GetEnumerableItems(string nameSearched, bool shouldPerformSearch)
    {
        var result = string.IsNullOrEmpty(nameSearched) || !shouldPerformSearch
            ? CachedList.ToList()
            : CachedList.ToDictionary(items => items.Key,
                                      items => items.Value.Where(i => i.Name.ExtractText().Contains(nameSearched, StringComparison.InvariantCultureIgnoreCase)).ToList())
                        .Where(kv => kv.Value.Count > 0)
                        .ToList();
        return (result, nameSearched);
    }

    private Dictionary<SearchCategories, List<Item>> SortCategoriesAndItems()
    {
        try
        {
            var itemSearchCategories = _dataManager.GetExcelSheet<ItemSearchCategory>()
                                                   .Where(c => c.Category > 0 && LootIdentifierConstants.CategoryIds.Contains(c.RowId))
                                                   .Select(c => new SearchCategories(c.Category, c.RowId, c.Name.ExtractText(), c.Order))
                                                   .OrderBy(c => c.Category)
                                                   .ThenBy(c => c.Order);

            return itemSearchCategories.ToDictionary(searchCategory => searchCategory,
                                                     c => _items.Where(i => i.ItemSearchCategory.RowId == c.RowId && !LootIdentifierConstants.ExclusionRegex.IsMatch(i.Name.ExtractText()))
                                                                .OrderBy(i => i.Name.ToString())
                                                                .ToList());
        }
        catch (Exception ex)
        {
            _pluginLog.Error(ex, "Error loading category list.");
            return [];
        }
    }
}
