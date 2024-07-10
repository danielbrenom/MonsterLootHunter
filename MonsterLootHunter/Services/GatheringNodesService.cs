using Dalamud.Plugin.Services;
using Lumina.Excel.GeneratedSheets;
using MonsterLootHunter.Data;
using MonsterLootHunter.Data.Gatherable;

namespace MonsterLootHunter.Services;

public class GatheringNodesService
{
    private readonly IPluginLog _pluginLog;
    private readonly IDataManager _dataManager;
    private readonly ItemManagerService _itemManagerService;
    private Dictionary<uint, Gatherable> _gatherables = new();
    private Dictionary<uint, Gatherable> _gatherablesByGatherId = new();
    private Dictionary<uint, GatheringNode> _gatheringNodes = new();
    private ServiceState _currentState = ServiceState.Loading;

    public GatheringNodesService(IDataManager dataManager, ItemManagerService itemManagerService, IPluginLog pluginLog)
    {
        _dataManager = dataManager;
        _itemManagerService = itemManagerService;
        _pluginLog = pluginLog;
        StartupService();
    }

    public async Task CheckGatherable(ulong itemId, LootData currentLootData)
    {
        while (_currentState == ServiceState.Loading)
            await Task.Delay(TimeSpan.FromSeconds(2));

        if (_currentState == ServiceState.Error)
            return;

        var possibleGatherable = _gatherables.FirstOrDefault(g => g.Key == itemId);
        if (possibleGatherable.Value is null)
            return;

        currentLootData.LootLocations.AddRange(
            possibleGatherable.Value.NodeList.Select(node => new LootDrops
            {
                MobName = possibleGatherable.Value.Name,
                MobLocation = $"{node.Name} - {node.Territory?.PlaceName.Value?.Name.ToString() ?? string.Empty}",
                Node = new LootNode
                {
                    TerritoryId = node.Territory?.RowId ?? uint.MinValue,
                    MapId = node.Territory?.Map.Row ?? uint.MinValue,
                    LootPosition = [node.IntegralXCoord, node.IntegralYCoord],
                    Kind = node.GatheringKind,
                    PositionRadius = node.Radius
                }
            }));
    }

    private void StartupService()
    {
        Task.Run(() =>
        {
            try
            {
                _gatherables = _dataManager.GetExcelSheet<GatheringItem>()?
                                           .Where(g => g.Item != 0 && g.Item < 1000000)
                                           .GroupBy(g => g.Item)
                                           .Select(group => group.First())
                                           .ToDictionary(g => (uint)g.Item, g =>
                                            {
                                                var possibleItem = _itemManagerService.RetrieveItem((uint)g.Item) ?? new Item();
                                                return new Gatherable(possibleItem, g);
                                            }) ?? new Dictionary<uint, Gatherable>();

                _gatherablesByGatherId = _gatherables.Values.ToDictionary(g => g.GatheringId, g => g);

                // Create GatheringItemPoint dictionary.
                var tmpGatheringItemPoint = _dataManager.GetExcelSheet<GatheringItemPoint>()!
                                                        .GroupBy(row => row.GatheringPoint.Row)
                                                        .ToDictionary(group => group.Key, group => group.Select(g => g.RowId).Distinct().ToList());

                var tmpGatheringPoints = _dataManager.GetExcelSheet<GatheringPoint>()!
                                                     .Where(row => row.PlaceName.Row > 0)
                                                     .GroupBy(row => row.GatheringPointBase.Row)
                                                     .ToDictionary(group => group.Key, group => group.Select(g => g.RowId).Distinct().ToList());

                _gatheringNodes = _dataManager.GetExcelSheet<GatheringPointBase>()?
                                              .Where(b => b.GatheringType.Row < 4)
                                              .Select(b => new GatheringNode(_dataManager, _gatherablesByGatherId, tmpGatheringPoints, tmpGatheringItemPoint, b))
                                              .Where(n => n.Items.Count > 0)
                                              .ToDictionary(n => n.Id, n => n)
                                  ?? new Dictionary<uint, GatheringNode>();
                _currentState = ServiceState.Success;
            }
            catch (Exception e)
            {
                _pluginLog.Error(e.Message);
                _currentState = ServiceState.Error;
            }
        });
    }
}

internal enum ServiceState
{
    Loading,
    Success,
    Error
}
