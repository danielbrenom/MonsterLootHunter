﻿using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using MonsterLootHunter.Data;
using MonsterLootHunter.Helpers;
using MonsterLootHunter.Logic;
using MonsterLootHunter.Services;
using MonsterLootHunter.Utils;

namespace MonsterLootHunter.Windows;

public class PluginUi : Window, System.IDisposable
{
    private readonly PluginServiceFactory _pluginServiceFactory;
    private readonly Configuration _configuration;
    private readonly MaterialTableRenderer _materialTableRenderer;
    private Item _selectedItem;
    private IDalamudTextureWrap _selectedItemIcon;
    private List<KeyValuePair<ItemSearchCategory, List<Item>>> _enumerableCategoriesAndItems;
    private LootData _lootData;
    private readonly float _scale;
    private readonly Vector2 itemTextSize;
    private readonly CancellationTokenSource _tokenSource;
    private bool Loading { get; set; }

    #region Props

    private string _searchString = string.Empty;

    public string SearchString
    {
        get => _searchString;
        set => _searchString = value;
    }

    private string _lastSearchString = string.Empty;

    #endregion

    public PluginUi(PluginServiceFactory serviceFactory) : base(WindowConstants.MainWindowName, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        _pluginServiceFactory = serviceFactory;
        _configuration = _pluginServiceFactory.Create<Configuration>();
        _selectedItem = new Item();
        _tokenSource = new CancellationTokenSource();
        _scale = ImGui.GetIO().FontGlobalScale;
        itemTextSize = ImGui.CalcTextSize(string.Empty);
        _materialTableRenderer = new MaterialTableRenderer(_pluginServiceFactory.Create<MapManagerService>(), _scale, itemTextSize);
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(800, 600) * _scale,
            MaximumSize = new Vector2(800, 600) * _scale * 1.5f
        };
        SizeCondition = ImGuiCond.FirstUseEver;
        _enumerableCategoriesAndItems = new List<KeyValuePair<ItemSearchCategory, List<Item>>>();
    }

    public override void Draw()
    {
        LoadCategoryItemList();

        ImGui.BeginChild("lootListColumn", new Vector2(267, 0) * _scale, true);
        ImGui.SetNextItemWidth(ImGui.GetIO().FontGlobalScale - ImGui.GetStyle().ItemSpacing.X);
        ImGui.InputTextWithHint("##searchString", "Search for loot", ref _searchString, 256);
        ImGui.Separator();

        #region Loot Categories

        ImGui.BeginChild("itemTree", new Vector2(0, -1.0f * ImGui.GetFrameHeightWithSpacing()), false, ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.AlwaysHorizontalScrollbar);
        foreach (var (category, items) in _enumerableCategoriesAndItems)
        {
            if (!ImGui.TreeNode(category.Name + "##cat" + category.RowId)) continue;
            ImGui.Unindent(ImGui.GetTreeNodeToLabelSpacing());

            for (var i = 0; i < items.Count; i++)
            {
                if (ImGui.GetCursorPosY() < ImGui.GetScrollY() - itemTextSize.Y)
                {
                    // Don't draw items above the scroll region.
                    var y = ImGui.GetCursorPosY();
                    var sy = ImGui.GetScrollY() - itemTextSize.Y;
                    var spacing = itemTextSize.Y + ImGui.GetStyle().ItemSpacing.Y;
                    var c = items.Count;
                    while (i < c && y < sy)
                    {
                        y += spacing;
                        i++;
                    }

                    ImGui.SetCursorPosY(y);
                    continue;
                }

                if (ImGui.GetCursorPosY() > ImGui.GetScrollY() + ImGui.GetWindowHeight())
                {
                    // Don't draw item names below the scroll region
                    var remainingItems = items.Count - i;
                    var remainingItemsHeight = itemTextSize.Y * remainingItems;
                    var remainingGapHeight = ImGui.GetStyle().ItemSpacing.Y * (remainingItems - 1);
                    ImGui.Dummy(new Vector2(1, remainingItemsHeight + remainingGapHeight));
                    break;
                }

                var item = items[i];
                var nodeFlags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;

                if (item.RowId == _selectedItem?.RowId)
                {
                    nodeFlags |= ImGuiTreeNodeFlags.Selected;
                }

                ImGui.TreeNodeEx(item.Name + "##item" + item.RowId, nodeFlags);

                if (!ImGui.IsItemClicked()) continue;
                Loading = true;
                Task.Run(async () => await ChangeSelectedItem(item.RowId));
            }

            ImGui.Indent(ImGui.GetTreeNodeToLabelSpacing());
            ImGui.TreePop();
        }

        ImGui.EndChild();

        #endregion

        ImGui.Text("Settings: ");
        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        if (ImGui.Button($"{(char)FontAwesomeIcon.Cog}"))
        {
            var pluginWindow = _pluginServiceFactory.Create<WindowService>().GetWindow(WindowConstants.ConfigWindowName);
            if (pluginWindow is not ConfigWindow window) return;
            window.IsOpen = true;
        }

        ImGui.PopFont();

        ImGui.EndChild();
        ImGui.SameLine();
        ImGui.BeginChild("panelColumn", new Vector2(0, 0), false, ImGuiWindowFlags.NoScrollbar);
        if (_selectedItem?.RowId > 0)
        {
            if (_selectedItemIcon != null)
            {
                ImGui.Image(_selectedItemIcon.ImGuiHandle, new Vector2(40, 40));
            }
            else
            {
                ImGui.SetCursorPos(new Vector2(40, 40));
            }

            ImGui.SameLine();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImGui.GetFontSize() / 2.0f + 19 * _scale);
            ImGui.Text(_selectedItem?.Name ?? string.Empty);
            if (Loading)
            {
                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetCursorPosY() - ImGui.GetFontSize() / 2.0f + 350 * _scale);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImGui.GetFontSize() / 2.0f + 19 * _scale);
                ImGui.Text("Loading data");
                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImGui.GetFontSize() / 2.0f + 19 * _scale);
                ImGui.Text($"{(char)FontAwesomeIcon.Spinner}");
                ImGui.PopFont();
            }

            #region Obtained From Table

            ImGui.Text("Obtained From");
            var mobList = _lootData?.LootLocations.OrderBy(m => m.MobName).ToList();
            if (_configuration.UseLegacyViewer)
                _materialTableRenderer.RenderLegacyMobTable(mobList);
            else
                _materialTableRenderer.RenderMobTable(mobList);

            #endregion

            ImGui.Separator();

            #region Purchased From Table

            ImGui.Text("Purchased From");
            var vendorList = _lootData?.LootPurchaseLocations.OrderBy(v => v.Vendor).ToList();
            if (_configuration.UseLegacyViewer)
                _materialTableRenderer.RenderLegacyVendorTable(vendorList);
            else
                _materialTableRenderer.RenderVendorTable(vendorList);

            #endregion
        }

        ImGui.SetCursorPosY(ImGui.GetWindowContentRegionMax().Y - ImGui.GetTextLineHeightWithSpacing());
        ImGui.Text("Data provided by FFXIV Console Games Wiki (https://ffxiv.consolegameswiki.com)");

        ImGui.EndChild();
    }

    private void LoadCategoryItemList()
    {
        var shouldPerformSearch = !_enumerableCategoriesAndItems.Any() || _searchString != _lastSearchString;
        //Prevent loading the list on every draw if it's not necessary
        if (!shouldPerformSearch) return;
        (_enumerableCategoriesAndItems, _lastSearchString) =
            _pluginServiceFactory.Create<ItemManagerService>().GetEnumerableItems(_searchString, _searchString != _lastSearchString);
    }

    protected internal async Task ChangeSelectedItem(uint itemId)
    {
        _selectedItem = _pluginServiceFactory.Create<ItemManagerService>().RetrieveItem(itemId);
        var iconId = _selectedItem.Icon;
        _selectedItemIcon?.Dispose();
        _selectedItemIcon = _pluginServiceFactory.Create<ITextureProvider>().GetIcon(iconId);
        try
        {
            _lootData = default;
            var token = _tokenSource.Token;
            var itemName = _configuration.UsingAnotherLanguage() ? await _pluginServiceFactory.Create<GarlandClient>().GetItemName(_selectedItem.RowId, token) : _selectedItem.Name.ToString();

            _lootData = await _pluginServiceFactory.Create<ScrapperClient>()
                                                   .GetLootData(itemName, token)
                                                   .ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
        }
        catch (System.OperationCanceledException e)
        {
            _pluginServiceFactory.Create<IPluginLog>().Error(e, "Request for loot $1 info failed", _selectedItem.Name);
        }
        finally
        {
            Loading = false;
        }
    }

    public void Dispose()
    {
        _selectedItemIcon?.Dispose();
        _tokenSource?.Dispose();
        System.GC.SuppressFinalize(this);
    }
}