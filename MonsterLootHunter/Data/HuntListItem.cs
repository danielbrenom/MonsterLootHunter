namespace MonsterLootHunter.Data;

public class HuntListItem
{
    public uint ItemId { get; set; }
    public string Name { get; set; }
    public int Quantity { get; set; }
    public int OwnedQuantity { get; set; }
    public bool Completed { get; set; }
    public bool Enabled { get; set; }
    public bool Editing { get; set; }
    public string[] Locations { get; set; }
}