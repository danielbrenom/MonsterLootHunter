using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ImGuiNET;
using Lumina.Excel.Sheets;
using MonsterLootHunter.Data.Gatherable;
using MonsterLootHunter.Services;
using MonsterLootHunter.Utils;

namespace MonsterLootHunter.Windows;

public class DebugWindow : Window
{
    private readonly IDataManager _dataManager;
    private readonly IPluginLog _pluginLog;
    private readonly MapManagerService _mapManagerService;
    private readonly float _scale;
    private Dictionary<uint, Gatherable> _gatherables = new();
    private Dictionary<uint, Gatherable> _gatherablesByGatherId = new();
    private Dictionary<uint, GatheringNode> _gatheringNodes = new();
    private readonly IEnumerable<Item>? _items;
    private bool _loaded;

    private string _searchString = string.Empty;

    public string SearchString
    {
        get => _searchString;
        set => _searchString = value;
    }

    public DebugWindow(IDataManager dataManager, IPluginLog pluginLog, MapManagerService mapManagerService)
        : base(WindowConstants.DebugWindowName, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        _dataManager = dataManager;
        _pluginLog = pluginLog;
        _mapManagerService = mapManagerService;
        _items = dataManager.GetExcelSheet<Item>();
        _scale = ImGui.GetIO().FontGlobalScale;
    }

    public override void Draw()
    {
        try
        {
            LoadData();
        }
        catch (Exception e)
        {
            _pluginLog.Error(e.Message);
        }

        ImGui.BeginChild("", new Vector2(0, 0) * _scale, true);
        ImGui.Text("Gatherables");

        ImGui.EndChild();
    }

    private bool _loadingStarted = false;

    private void LoadData()
    {
        if (_loaded || _loadingStarted)
            return;

        _loadingStarted = true;
        Task.Run(() =>
        {
            try
            {
                _gatherables = _dataManager.GetExcelSheet<GatheringItem>()
                                           .Where(g => g.Item.RowId != 0 && g.Item.RowId < 1000000)
                                           .GroupBy(g => g.Item)
                                           .Select(group => group.First())
                                           .ToDictionary(g => g.Item.RowId, g =>
                                            {
                                                var possibleItem = _items?.SingleOrDefault(i => g.Item.RowId == i.RowId) ?? new Item();
                                                return new Gatherable(possibleItem, g);
                                            }) ?? new Dictionary<uint, Gatherable>();
                _gatherablesByGatherId = _gatherables.Values.ToDictionary(g => g.GatheringId, g => g);
                // Create GatheringItemPoint dictionary.
                var tmpGatheringItemPoint = _dataManager.GetSubrowExcelSheet<GatheringItemPoint>().SelectMany(g => g)
                                                        .GroupBy(row => row.GatheringPoint.RowId)
                                                        .ToDictionary(group => group.Key, group => group.Select(g => g.RowId).Distinct().ToList());

                var tmpGatheringPoints = _dataManager.GetExcelSheet<GatheringPoint>()!
                                                     .Where(row => row.PlaceName.RowId > 0)
                                                     .GroupBy(row => row.GatheringPointBase.RowId)
                                                     .ToDictionary(group => group.Key, group => group.Select(g => g.RowId).Distinct().ToList());

                _gatheringNodes = _dataManager.GetExcelSheet<GatheringPointBase>()?
                                              .Where(b => b.GatheringType.RowId < 4)
                                              .Select(b => new GatheringNode(_dataManager, _gatherablesByGatherId, tmpGatheringPoints, tmpGatheringItemPoint, b))
                                              .Where(n => n.Items.Count > 0)
                                              .ToDictionary(n => n.Id, n => n)
                                  ?? new Dictionary<uint, GatheringNode>();
            }
            catch (Exception e)
            {
                _pluginLog.Error(e.Message);
            }

            _loaded = true;
        });
    }
}
