using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud;
using Dalamud.Logging;
using Lumina.Excel.GeneratedSheets;
using MonsterLootHunter.Utils;

namespace MonsterLootHunter.Logic;

public class ItemManager : IServiceType
{
    private readonly IEnumerable<Item> _items;
    private Dictionary<ItemSearchCategory, List<Item>> CachedList { get; set; }

    public ItemManager()
    {
        _items = Plugin.DataManager.GetExcelSheet<Item>();
        CachedList = SortCategoriesAndItems();
    }

    public Item RetrieveItem(uint itemId)
    {
        return _items.Single(i => i.RowId == itemId);
    }

    public (List<KeyValuePair<ItemSearchCategory, List<Item>>>, string) GetEnumerableItems(string nameSearched, bool shouldPerformSearch)
    {
        var result = string.IsNullOrEmpty(nameSearched) && !shouldPerformSearch
            ? CachedList.ToList()
            : CachedList
             .Select(kv => new KeyValuePair<ItemSearchCategory, List<Item>>(
                         kv.Key, kv.Value.Where(i => i.Name.ToString().ToUpperInvariant().Contains(nameSearched.ToUpperInvariant(), StringComparison.InvariantCulture)).ToList()))
             .Where(kv => kv.Value.Count > 0)
             .ToList();
        return (result, nameSearched);
    }

    private Dictionary<ItemSearchCategory, List<Item>> SortCategoriesAndItems()
    {
        try
        {
            var itemSearchCategories = Plugin.DataManager.GetExcelSheet<ItemSearchCategory>();
            if (itemSearchCategories is null) return default;
            var sortedCategories = itemSearchCategories.Where(c => c.Category > 0).OrderBy(c => c.Category).ThenBy(c => c.Order);
            var sortedCategoriesDict = new Dictionary<ItemSearchCategory, List<Item>>();

            foreach (var c in sortedCategories)
            {
                switch (c.Name)
                {
                    case LootIdentifierConstants.Leather:
                        sortedCategoriesDict.Add(c, _items.Where(i => i.ItemSearchCategory.Row == c.RowId).Where(i => LootIdentifierConstants.LeatherRegex.IsMatch(i.Name)).OrderBy(i => i.Name.ToString()).ToList());
                        break;
                    case LootIdentifierConstants.Reagents:
                    case LootIdentifierConstants.Bone:
                    case LootIdentifierConstants.Ingredients:
                        sortedCategoriesDict.Add(c, _items.Where(i => i.ItemSearchCategory.Row == c.RowId).OrderBy(i => i.Name.ToString()).ToList());
                        break;
                    default:
                        continue;
                }
            }

            return sortedCategoriesDict;
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Error loading category list.");
            return default;
        }
    }
}