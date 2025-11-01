using MonsterLootHunter.Data.Gatherable;
using Newtonsoft.Json;

namespace MonsterLootHunter.Data;

public class LootData(string lootName)
{
    public string LootName { get; set; } = lootName;
    public bool ContainsWikiData { get; set; }
    public List<LootDrops> LootLocations { get; set; } = [];
    public List<LootPurchase> LootPurchaseLocations { get; set; } = [];

    [JsonIgnore]
    public bool HasLocations => LootLocations.Count != 0 || LootPurchaseLocations.Count != 0;
}

public class LootDrops
{
    public string MobName { get; set; } = string.Empty;
    public string MobLocation { get; set; } = string.Empty;
    public string MobLevel { get; set; } = string.Empty;
    public string MobFlag { get; set; } = string.Empty;
    public LootNode? Node { get; set; }
}

public class LootNode
{
    public uint TerritoryId { get; set; }
    public uint MapId { get; set; }
    public int[] LootPosition { get; set; } = new int[2];
    public GatheringKind Kind { get; set; }
    public int PositionRadius { get; set; }
}

public class LootPurchase
{
    public string Vendor { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string FlagPosition { get; set; } = string.Empty;
    public string Cost { get; set; } = string.Empty;
    public string CostType { get; set; } = string.Empty;
}

public enum LootSortId
{
    Name = 1,
    Location = 2,
    Level = 3,
    Flag = 4,
    Action = 5
}
