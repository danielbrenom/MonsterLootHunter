using System.Globalization;
using System.Text.RegularExpressions;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Extensions;
using MonsterLootHunter.Data.Gatherable;
using MonsterLootHunter.Utils;
using MapType = FFXIVClientStructs.FFXIV.Client.UI.Agent.MapType;

namespace MonsterLootHunter.Services;

public partial class MapManagerService(IDataManager dataManager, IGameGui gameGui, IPluginLog pluginLog)
{
    [GeneratedRegex(@"(\d+\.?\d*)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex CoordinatesRegex();

    private readonly ExcelSheet<TerritoryType> _cachedTerritories = dataManager.GetExcelSheet<TerritoryType>();
    private readonly ExcelSheet<GatheringType> _icons = dataManager.GetExcelSheet<GatheringType>();

    public bool CheckLocation(string locationName, out TerritoryType? territoryType)
    {
        locationName = locationName.Contains('-') ? locationName.Split('-')[1].Trim() : locationName.Trim();
        territoryType = _cachedTerritories.FirstOrNull(t => t.PlaceName.IsValid && t.PlaceName.Value.Name.ExtractText().Contains(locationName, StringComparison.InvariantCultureIgnoreCase));
        return territoryType is not null;
    }

    public void MarkMapFlag(TerritoryType? location, string position)
    {
        try
        {
            if (!location.HasValue)
                return;

            var pureLocation = location.Value;
            var flagParsed = CoordinatesRegex().Matches(position);
            var x = float.Parse(flagParsed[0].Value, CultureInfo.InvariantCulture);
            var y = float.Parse(flagParsed[1].Value, CultureInfo.InvariantCulture);
            var mapPayload = new MapLinkPayload(pureLocation.RowId, pureLocation.Map.Value.RowId, x, y, 0.0f);
            gameGui.OpenMapWithMapLink(mapPayload);
        }
        catch (Exception e)
        {
            pluginLog.Error("Not able to mark location {0} on map. With error: {1}", location?.Name.ExtractText() ?? string.Empty, e.Message);
        }
    }

    public unsafe void MarkMapFlag(uint location, (int, int) coordinates, string lootName, GatheringKind kind, int radius)
    {
        var mapInstance = AgentMap.Instance();

        try
        {
            var (x, y) = coordinates;
            var territory = _cachedTerritories.FirstOrDefault(t => t.RowId == location);
            var map = territory.Map.Value;

            var icon = GetIcon(kind);
            if (mapInstance is null)
                return;

            mapInstance->TempMapMarkerCount = 0;
            mapInstance->AddGatheringTempMarker(MapUtils.IntegerToInternal(x, map.SizeFactor),
                                                MapUtils.IntegerToInternal(y, map.SizeFactor),
                                                radius, icon, 4u, lootName);
            mapInstance->OpenMap(territory.Map.RowId, territory.RowId, lootName, MapType.GatheringLog);
        }
        catch (Exception e)
        {
            pluginLog.Error("Not able to mark location {0} on map. With error: {1}", location.ToString(), e.Message);
        }
    }

    private uint GetIcon(GatheringKind kind)
    {
        return kind switch
        {
            GatheringKind.Miner => (uint)_icons.GetRow(0).IconMain,
            GatheringKind.Mining => (uint)_icons.GetRow(0).IconMain,
            GatheringKind.Quarrying => (uint)_icons.GetRow(1).IconMain,
            GatheringKind.Botanist => (uint)_icons.GetRow(2).IconMain,
            GatheringKind.Logging => (uint)_icons.GetRow(2).IconMain,
            GatheringKind.Harvesting => (uint)_icons.GetRow(3).IconMain,
            GatheringKind.Spearfishing => (uint)_icons.GetRow(4).IconMain,
            GatheringKind.Fisher => 60465,
            GatheringKind.Multiple => (uint)_icons.GetRow(0).IconMain,
            GatheringKind.Unknown => (uint)_icons.GetRow(0).IconMain,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }
}
