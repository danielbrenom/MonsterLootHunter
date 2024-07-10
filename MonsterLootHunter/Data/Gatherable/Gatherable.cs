using Lumina.Excel.GeneratedSheets;

namespace MonsterLootHunter.Data.Gatherable;

public class Gatherable
{
    public Item ItemData { get; }
    public GatheringItem GatheringData { get; }

    public string Name { get; } = string.Empty;

    public IList<GatheringNode> NodeList { get; } = new List<GatheringNode>();
    public int InternalLocationId { get; internal set; } = 0;

    public IEnumerable<GatheringNode> Locations
        => NodeList;

    public uint ItemId => ItemData.RowId;
    public uint GatheringId => GatheringData.RowId;

    public Gatherable(Item itemData, GatheringItem gatheringData)
    {
        GatheringData = gatheringData;
        ItemData = itemData;
        if (ItemData.RowId == 0)
            return;

        var levelData = gatheringData.GatheringItemLevel?.Value;
        _levelStars = levelData == null ? 0 : (levelData.GatheringItemLevel << 3) + levelData.Stars;
        Name = ItemData.Name.ToString();
    }

    public int Level
        => _levelStars >> 3;

    public int Stars
        => _levelStars & 0b111;

    public string StarsString()
        => StarsArray[Stars];

    public string LevelString()
        => $"{Level}{StarsString()}";

    public override string ToString()
        => $"{Name} ({Level}{StarsString()})";

    public int CompareTo(Gatherable? rhs)
        => ItemId.CompareTo(rhs?.ItemId ?? 0);

    private readonly int _levelStars;

    private static readonly string[] StarsArray =
    {
        "",
        "*",
        "**",
        "***",
        "****",
    };
}
