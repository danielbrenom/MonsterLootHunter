using System.Collections.Generic;
using MonsterLootHunter.Data;

namespace MonsterLootHunter.Services;

public class HuntListService
{
    private const string FileName = "hunt_lists.json";

    private SortedList<string, HuntList> HuntLists { get; set; } = new();


    public HuntListService()
    {
        HuntLists.Add("test", new HuntList("test")
        {
            HuntListItems = new[]
            {
                new HuntListItem { ItemId = 36217, Name = "Double-edged Herb", Quantity = 10, OwnedQuantity = 10, Completed = true },
                new HuntListItem { ItemId = 36217, Name = "Double-edged Herb", Quantity = 10, OwnedQuantity = 10, Completed = true },
                new HuntListItem { ItemId = 36217, Name = "Double-edged Herb", Quantity = 10, OwnedQuantity = 10, Completed = true },
                new HuntListItem { ItemId = 36217, Name = "Double-edged Herb", Quantity = 10, OwnedQuantity = 10, Completed = true },
            }
        });
    }

    public IEnumerable<string> GetHuntListNames() => HuntLists.Keys;

    public HuntList GetHuntList(string name) => HuntLists[name];
}