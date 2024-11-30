///ref: https://github.com/Ottermandias/GatherBuddy/blob/main/GatherBuddy.GameData/Classes/Node.Base.cs

using System.Numerics;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using MonsterLootHunter.Utils;

namespace MonsterLootHunter.Data.Gatherable;

public partial class GatheringNode : IComparable<GatheringNode>
{
    public GatheringPointBase BaseNodeData { get; init; }
    public string Name { get; init; }
    public Vector3[] Markers { get; set; } = [];
    public TerritoryType? Territory { get; private set; }

    public uint Id => BaseNodeData.RowId;

    public IEnumerable<Gatherable> Gatherables => Items;

    public int Level => BaseNodeData.GatheringLevel;

    public GatheringKind GatheringKind => (GatheringKind)BaseNodeData.GatheringType.Value.RowId;

    public GatheringNode(IDataManager data, Dictionary<uint, Gatherable> gatherablesByGatherId, IReadOnlyDictionary<uint, List<uint>> gatheringPoint,
                         IReadOnlyDictionary<uint, List<uint>> gatheringItemPoint, GatheringPointBase node)
    {
        BaseNodeData = node;

        // Obtain the territory from the first node that has this as a base.
        var nodes = data.GetExcelSheet<GatheringPoint>()!;
        var nodeList = gatheringPoint.TryGetValue(node.RowId, out var nl) ? (IReadOnlyList<uint>)nl : Array.Empty<uint>();
        var nodeRow = nodeList.Count > 0 ? nodes.GetRowOrDefault(nodeList[0]) : null;
        Name = nodeRow?.PlaceName.ValueNullable?.Name.ToString() ?? string.Empty;
        // Obtain the center of the coordinates. We do not care for the radius.
        var coords = data.GetExcelSheet<ExportedGatheringPoint>();
        var coordRow = coords.GetRowOrDefault(node.RowId);
        Territory = nodeRow?.TerritoryType.Value;
        IntegralXCoord = coordRow != null ? MapUtils.NodeToMap(coordRow.Value.X, nodeRow?.TerritoryType.ValueNullable?.Map.ValueNullable?.SizeFactor ?? 100f) : 100;
        IntegralYCoord = coordRow != null ? MapUtils.NodeToMap(coordRow.Value.Y, nodeRow?.TerritoryType.ValueNullable?.Map.ValueNullable?.SizeFactor ?? 100f) : 100;

        Radius = coordRow?.Radius ?? 10;

        DefaultXCoord = IntegralXCoord;
        DefaultYCoord = IntegralYCoord;
        DefaultRadius = Radius;

        // Obtain the items and add the node to their individual lists.
        Items = node.Item
                    .Select(i => gatherablesByGatherId.GetValueOrDefault(i.RowId))
                    .OfType<Gatherable>()
                    .ToList();

        foreach (var n in nodeList)
        {
            if (!gatheringItemPoint.TryGetValue(n, out var gatherableList))
                break;

            foreach (var g in gatherableList)
            {
                if (gatherablesByGatherId.TryGetValue(g, out var gatherable)
                    && gatherable.GatheringData.IsHidden
                    && !Items.Contains(gatherable))
                    Items.Add(gatherable);
            }
        }

        if (nodeRow?.TerritoryType.ValueNullable?.RowId <= 0)
            return;

        foreach (var item in Items)
            AddNodeToItem(item);
    }

    public int CompareTo(GatheringNode? obj)
        => Id.CompareTo(obj?.Id ?? 0);
}
