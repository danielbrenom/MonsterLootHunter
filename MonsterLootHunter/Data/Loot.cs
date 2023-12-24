﻿namespace MonsterLootHunter.Data;

public class LootData
{
    public string LootName { get; set; }
    public List<LootDrops> LootLocations { get; set; }
    public List<LootPurchase> LootPurchaseLocations { get; set; }

    public LootData(string lootName)
    {
        LootName = lootName;
        LootLocations = new List<LootDrops>();
        LootPurchaseLocations = new List<LootPurchase>();
    }
}

public class LootDrops
{
    public string MobName { get; set; } = string.Empty;
    public string MobLocation { get; set; } = string.Empty;
    public string MobLevel { get; set; } = string.Empty;
    public string MobFlag { get; set; } = string.Empty;
    public MaterialType Type { get; set; }
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