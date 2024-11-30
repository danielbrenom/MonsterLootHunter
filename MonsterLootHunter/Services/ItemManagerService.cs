using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using MonsterLootHunter.Utils;

namespace MonsterLootHunter.Services;

public class ItemManagerService
{
    private readonly IDataManager _dataManager;
    private readonly IPluginLog _pluginLog;

    private readonly IEnumerable<Item>? _items;
    private Dictionary<ItemSearchCategory, List<Item>> CachedList { get; }

    public ItemManagerService(IDataManager dataManager, IPluginLog pluginLog)
    {
        _dataManager = dataManager;
        _pluginLog = pluginLog;
        _items = dataManager.GetExcelSheet<Item>();
        CachedList = SortCategoriesAndItems();
    }

    public bool CheckSelectedItem(ulong itemId)
    {
        var foundItem = RetrieveItem(itemId);
        return foundItem is not null && CachedList.Any(pair => pair.Value.Any(item => item.RowId == foundItem.Value.RowId));
    }

    public Item? RetrieveItem(ulong? itemId) => _items?.SingleOrDefault(i => i.RowId == itemId);

    public (List<KeyValuePair<ItemSearchCategory, List<Item>>>, string) GetEnumerableItems(string nameSearched, bool shouldPerformSearch)
    {
        var result = string.IsNullOrEmpty(nameSearched) || !shouldPerformSearch
            ? CachedList.ToList()
            : CachedList.ToDictionary(items => items.Key,
                                      items => items.Value.Where(i => i.Name.ToString().Contains(nameSearched, StringComparison.InvariantCultureIgnoreCase)).ToList())
                        .Where(kv => kv.Value.Count > 0)
                        .ToList();
        return (result, nameSearched);
    }

    private Dictionary<ItemSearchCategory, List<Item>> SortCategoriesAndItems()
    {
        try
        {
            if (_items is null)
                throw new ArgumentNullException(nameof(_items));

            var itemSearchCategories = _dataManager.GetExcelSheet<ItemSearchCategory>();
            var sortedCategories = itemSearchCategories.Where(c => c.Category > 0 && LootIdentifierConstants.CategoryIds.Contains(c.RowId))
                                                       .OrderBy(c => c.Category).ThenBy(c => c.Order);

            if (sortedCategories is null)
                throw new ArgumentNullException(nameof(sortedCategories));

            return sortedCategories.ToDictionary(searchCategory => searchCategory,
                                                 c => _items.Where(i => i.ItemSearchCategory.RowId == c.RowId)
                                                            .Where(i => !LootIdentifierConstants.ExclusionRegex.IsMatch(i.Name.ToString()))
                                                            .OrderBy(i => i.Name.ToString())
                                                            .ToList());
        }
        catch (Exception ex)
        {
            _pluginLog.Error(ex, "Error loading loot category list.");
            return new Dictionary<ItemSearchCategory, List<Item>>(0);
        }
    }
}
