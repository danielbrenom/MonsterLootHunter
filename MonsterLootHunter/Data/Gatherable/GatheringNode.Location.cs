namespace MonsterLootHunter.Data.Gatherable;

public partial class GatheringNode
{
    public int IntegralXCoord { get; set; }
    public int IntegralYCoord { get; set; }
    public int DefaultXCoord { get; internal set; }
    public int DefaultYCoord { get; internal set; }

    public ushort Radius { get; set; }
    public ushort DefaultRadius { get; internal set; }
}
