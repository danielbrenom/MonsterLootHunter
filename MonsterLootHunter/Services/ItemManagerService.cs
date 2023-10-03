using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud;
using Dalamud.Plugin.Services;
using Lumina.Excel.GeneratedSheets;
using MonsterLootHunter.Utils;

namespace MonsterLootHunter.Services;

public class ItemManagerService : IServiceType
{
    private readonly IDataManager _dataManager;
    private IPluginLog _pluginLog;
    private readonly IEnumerable<Item> _items;
    private Dictionary<ItemSearchCategory, List<Item>> CachedList { get; }

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
        return foundItem is not null && CachedList.Any(pair => pair.Value.Any(item => item.RowId == foundItem.RowId));
    }

    public Item RetrieveItem(uint itemId)
    {
        return _items.SingleOrDefault(i => i.RowId == itemId);
    }

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
            var itemSearchCategories = _dataManager.GetExcelSheet<ItemSearchCategory>();
            var sortedCategories = itemSearchCategories?.Where(c => c.Category > 0 && LootIdentifierConstants.CategoryIds.Contains(c.RowId))
                                                        .OrderBy(c => c.Category).ThenBy(c => c.Order);

            return sortedCategories?.ToDictionary(searchCategory => searchCategory,
                                                  c => _items.Where(i => i.ItemSearchCategory.Row == c.RowId)
                                                             .Where(i => !LootIdentifierConstants.ExclusionRegex.IsMatch(i.Name))
                                                             .OrderBy(i => i.Name.ToString())
                                                             .ToList());
        }
        catch (Exception ex)
        {
            _pluginLog.Error(ex, "Error loading category list.");
            return default;
        }
    }
}