using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dalamud;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Logging;
using Lumina.Excel.GeneratedSheets;

namespace MonsterLootHunter.Logic;

public class MapManager : IServiceType
{
    private List<TerritoryType> CachedTerritories { get; }

    public MapManager()
    {
        CachedTerritories = Plugin.DataManager.GetExcelSheet<TerritoryType>()?.ToList();
    }
    
    public void MarkMapFlag(string locationName, string position)
    {
        try
        {
            var location = CachedTerritories.FirstOrDefault(t => t.PlaceName.Value.Name.ToString().ToLowerInvariant().Contains(locationName.ToLowerInvariant()));
            if (location is null) return;
            var coords = new float[2];
            coords[0] = float.Parse(position.Split(",")[0].Replace("(", "").Replace("x", ""), CultureInfo.InvariantCulture);
            coords[1] = float.Parse(position.Split(",")[1].Replace(")", "").Replace("y", ""), CultureInfo.InvariantCulture);
            var mapPayload = new MapLinkPayload(location.RowId, location.Map.Row, coords[0], coords[1], 0.0f);
            Plugin.GameGui.OpenMapWithMapLink(mapPayload);
        }
        catch (Exception e)
        {
            PluginLog.Error($"Not able to mark location {locationName} on map. With error: {e.Message}");
        }
    }
}