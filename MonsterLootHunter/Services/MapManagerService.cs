using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud;
using Dalamud.Data;
using Dalamud.Game.Gui;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Logging;
using Lumina.Excel.GeneratedSheets;

namespace MonsterLootHunter.Services;

public partial class MapManagerService : IServiceType
{
    [GeneratedRegex("(\\d+\\.?\\d*)")]
    private static partial Regex CoordinatesRegex();
    
    private readonly GameGui _gameGui;
    private List<TerritoryType> CachedTerritories { get; }

    public MapManagerService(DataManager dataManager, GameGui gameGui)
    {
        _gameGui = gameGui;
        CachedTerritories = dataManager.GetExcelSheet<TerritoryType>()?.ToList();
    }
    
    public void MarkMapFlag(string locationName, string position)
    {
        try
        {
            locationName = locationName.Contains('-') ? locationName.Split('-')[0] : locationName;
            var location = CachedTerritories.FirstOrDefault(t => t.PlaceName.Value != null && t.PlaceName.Value.Name.ToString().ToLowerInvariant().Contains(locationName.ToLowerInvariant()));
            if (location is null) return;
            var flagParsed = CoordinatesRegex().Matches(position);
            var coords = new float[2];
            coords[0] = float.Parse(flagParsed[0].Value, CultureInfo.InvariantCulture);
            coords[1] = float.Parse(flagParsed[1].Value, CultureInfo.InvariantCulture);
            var mapPayload = new MapLinkPayload(location.RowId, location.Map.Row, coords[0], coords[1], 0.0f);
            _gameGui.OpenMapWithMapLink(mapPayload);
        }
        catch (Exception e)
        {
            PluginLog.Error($"Not able to mark location {locationName} on map. With error: {e.Message}");
        }
    }
}