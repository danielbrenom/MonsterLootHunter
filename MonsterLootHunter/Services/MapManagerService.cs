using System.Globalization;
using System.Text.RegularExpressions;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using MonsterLootHunter.Data.Gatherable;
using MonsterLootHunter.Utils;
using MapType = FFXIVClientStructs.FFXIV.Client.UI.Agent.MapType;

namespace MonsterLootHunter.Services;

public partial class MapManagerService
{
    [GeneratedRegex(@"(\d+\.?\d*)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex CoordinatesRegex();

    private readonly IGameGui _gameGui;
    private readonly IPluginLog _pluginLog;
    private List<TerritoryType> CachedTerritories { get; }
    private readonly ExcelSheet<GatheringType> _icons;

    public MapManagerService(IDataManager dataManager, IGameGui gameGui, IPluginLog pluginLog)
    {
        _gameGui = gameGui;
        _pluginLog = pluginLog;
        CachedTerritories = dataManager.GetExcelSheet<TerritoryType>().ToList();
        _icons = dataManager.GetExcelSheet<GatheringType>();
    }

    public bool CheckLocation(string locationName, out TerritoryType? territoryType)
    {
        locationName = locationName.Contains('-') ? locationName.Split('-')[0].Trim() : locationName.Trim();
        territoryType = CachedTerritories.FirstOrDefault(t => t.PlaceName.IsValid && t.PlaceName.Value.Name.ToString().Contains(locationName, StringComparison.InvariantCultureIgnoreCase));
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
            _gameGui.OpenMapWithMapLink(mapPayload);
        }
        catch (Exception e)
        {
            _pluginLog.Error("Not able to mark location {0} on map. With error: {1}", location?.Name.ToString() ?? string.Empty, e.Message);
        }
    }

    public unsafe void MarkMapFlag(uint location, (int, int) coordinates, string lootName, GatheringKind kind, int radius)
    {
        var mapInstance = AgentMap.Instance();

        try
        {
            var (x, y) = coordinates;
            var territory = CachedTerritories.FirstOrDefault(t => t.RowId == location);
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
            _pluginLog.Error("Not able to mark location {0} on map. With error: {1}", location.ToString(), e.Message);
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
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }
}
