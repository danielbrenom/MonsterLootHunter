using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using MonsterLootHunter.Logic;
using MonsterLootHunter.Services;
using MonsterLootHunter.Utils;

namespace MonsterLootHunter.Windows;

public partial class PluginUi : Window, IDisposable
{
    private bool UiDrawn { get; set; }
    private readonly PluginServiceFactory _pluginServiceFactory;
    private readonly Vector2 _itemTextSize;
    private readonly float _scale;
    

    public PluginUi(PluginServiceFactory serviceFactory) : base(Plugin.Version.Length > 0 ? $"{WindowConstants.MainWindowName} v{Plugin.Version}###MonsterLootHunterMain" : WindowConstants.MainWindowName
                                                                , ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        _pluginServiceFactory = serviceFactory;
        _scale = ImGui.GetIO().FontGlobalScale;
        _itemTextSize = ImGui.CalcTextSize(string.Empty);

        _configuration = _pluginServiceFactory.Create<Configuration>();
        _huntListService = _pluginServiceFactory.Create<HuntListService>();
        _itemManagerService = _pluginServiceFactory.Create<ItemManagerService>();
        _materialTableRenderer = new MaterialTableRenderer(_pluginServiceFactory.Create<MapManagerService>(), _scale, _itemTextSize);

        _selectedItem = new Item();
        _tokenSource = new CancellationTokenSource();
        _enumerableCategoriesAndItems = new List<KeyValuePair<ItemSearchCategory, List<Item>>>();

        #region Window Settings

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(800, 600) * _scale,
            MaximumSize = new Vector2(800, 600) * _scale * 1.5f
        };
        SizeCondition = ImGuiCond.FirstUseEver;

        #endregion
    }

    public override void Draw()
    {
        ImGui.BeginTabBar("main_tabbar");
        if (ImGui.BeginTabItem("Loot Hunter"))
        {
            DrawMainUi();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Hunt List"))
        {
            DrawHuntList();
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }
}