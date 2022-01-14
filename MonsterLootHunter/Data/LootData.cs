using System.Collections.Generic;

namespace MonsterLootHunter.Data
{
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
}