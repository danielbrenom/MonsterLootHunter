using System.Collections.Generic;
using System.Linq;

namespace MonsterLootHunter.Data;

public class HuntList
{
    public string Name { get; init; }
    public bool Enabled { get; set; }
    public IEnumerable<HuntListItem> HuntListItems { get; init; }
    public HuntList(string name)
    {
        Name = name;
        Enabled = false;
        HuntListItems = Enumerable.Empty<HuntListItem>();
    }
}