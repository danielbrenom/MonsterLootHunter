namespace MonsterLootHunter.Data;

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
    public string MobName { get; set; }
    public string MobLocation { get; set; }
    public string MobLevel { get; set; }
    public string MobFlag { get; set; }
}

public class LootPurchase
{
    public string Vendor { get; set; }
    public string Location { get; set; }
    public string FlagPosition { get; set; }
    public string Cost { get; set; }
    public string CostType { get; set; }
}

public enum LootSortId
{
    Name = 1,
    Location = 2,
    Level = 3,
    Flag = 4,
    Action = 5
}