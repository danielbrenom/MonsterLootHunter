namespace MonsterLootHunter.Data;

public class ShopListItem
{
    public uint ItemId { get; set; }
    public int Quantity { get; set; }
    public int OwnedQuantity { get; set; }
    public bool Completed { get; set; }
}