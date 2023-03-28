using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Data;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Utility;
using ImGuiNET;
using ImGuiScene;
using Lumina.Excel.GeneratedSheets;
using MonsterLootHunter.Data;
using MonsterLootHunter.Helpers;
using MonsterLootHunter.Services;
using MonsterLootHunter.Utils;

namespace MonsterLootHunter.Windows;

public class PluginUi : Window, System.IDisposable
{
    private readonly PluginServiceFactory _pluginServiceFactory;
    private Item _selectedItem;
    private TextureWrap _selectedItemIcon;
    private List<KeyValuePair<ItemSearchCategory, List<Item>>> _enumerableCategoriesAndItems;
    private LootData _lootData;
    private readonly float _scale;
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
        _selectedItem = new Item();
        _tokenSource = new CancellationTokenSource();
        _scale = ImGui.GetIO().FontGlobalScale;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(800, 600) * _scale,
            MaximumSize = new Vector2(800, 600) * _scale * 1.5f
        };
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        (_enumerableCategoriesAndItems, _lastSearchString) =
            _pluginServiceFactory.Create<ItemManagerService>().GetEnumerableItems(_searchString, _searchString != _lastSearchString);

        ImGui.BeginChild("lootListColumn", new Vector2(267, 0) * _scale, true);
        ImGui.SetNextItemWidth(ImGui.GetIO().FontGlobalScale - ImGui.GetStyle().ItemSpacing.X);
        ImGui.InputTextWithHint("##searchString", "Search for loot", ref _searchString, 256);
        ImGui.Separator();

        ImGui.BeginChild("itemTree", new Vector2(0, -1.0f * ImGui.GetFrameHeightWithSpacing()), false, ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.AlwaysHorizontalScrollbar);
        var itemTextSize = ImGui.CalcTextSize(string.Empty);

        #region Loot Categories

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

        #endregion

        ImGui.EndChild();

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
            ImGui.BeginTable("obtainedFrom", 4, ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.NoHostExtendX,
                             new Vector2(0f, itemTextSize.Y * 13));

            ImGui.TableSetupScrollFreeze(4, 1);
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 200.0f);
            ImGui.TableSetupColumn("Location", ImGuiTableColumnFlags.WidthFixed, 230.0f);
            ImGui.TableSetupColumn("Position", ImGuiTableColumnFlags.WidthFixed, 100.0f);
            ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 40.0f);
            ImGui.TableHeadersRow();

            var mobList = _lootData?.LootLocations.OrderBy(m => m.MobName).ToList();

            if (mobList != null && mobList.Any())
            {
                foreach (var mob in mobList)
                {
                    var index = mobList.IndexOf(mob);
                    ImGui.TableNextRow(ImGuiTableRowFlags.None, itemTextSize.Y * 1.5f);
                    ImGui.AlignTextToFramePadding();
                    ImGui.TableNextColumn();
                    ImGui.Text(mob.MobName);
                    ImGui.TableNextColumn();
                    ImGui.Text(mob.MobLocation);
                    ImGui.TableNextColumn();
                    ImGui.Text(mob.MobFlag);
                    ImGui.TableNextColumn();
                    if (!string.IsNullOrEmpty(mob.MobFlag) && mob.MobFlag != "N/A")
                    {
                        ImGui.PushFont(UiBuilder.IconFont);
                        if (ImGui.Button($"{(char)FontAwesomeIcon.MapMarkerAlt}##listing{index}", new Vector2(25 * _scale, itemTextSize.Y * _scale * 1.5f)))
                            _pluginServiceFactory.Create<MapManagerService>().MarkMapFlag(mob.MobLocation, mob.MobFlag);

                        ImGui.PopFont();
                    }

                    ImGui.TableNextColumn();
                }
            }
            else
            {
                ImGui.TableNextRow();
                ImGui.AlignTextToFramePadding();
                ImGui.TableNextColumn();
                ImGui.Text("This probably isn't obtained");
                ImGui.TableNextColumn();
                ImGui.Text("this way or the Wiki don't have");
                ImGui.TableNextColumn();
                ImGui.Text("this information");
            }

            ImGui.EndTable();
            ImGui.Separator();

            #endregion

            #region Purchased From Table

            ImGui.Text("Purchased From");
            ImGui.BeginTable("purchasedFrom", 4, ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.NoHostExtendX,
                             new Vector2(0f, itemTextSize.Y * 13));
            ImGui.TableSetupScrollFreeze(4, 1);
            ImGui.TableSetupColumn("Vendor", ImGuiTableColumnFlags.WidthFixed, 200f);
            ImGui.TableSetupColumn("Location", ImGuiTableColumnFlags.WidthFixed, 150f);
            ImGui.TableSetupColumn("Position", ImGuiTableColumnFlags.WidthFixed, 100f);
            ImGui.TableSetupColumn("Price", ImGuiTableColumnFlags.WidthFixed, 200f);
            ImGui.TableHeadersRow();

            var vendorList = _lootData?.LootPurchaseLocations.OrderBy(v => v.Vendor).ToList();
            if (vendorList != null && vendorList.Any())
            {
                foreach (var vendor in vendorList)
                {
                    ImGui.TableNextRow();
                    ImGui.AlignTextToFramePadding();
                    ImGui.TableNextColumn();
                    ImGui.Text(vendor.Vendor);
                    ImGui.TableNextColumn();
                    ImGui.Text(vendor.Location);
                    ImGui.TableNextColumn();
                    ImGui.Text(vendor.FlagPosition);
                    ImGui.TableNextColumn();
                    ImGui.Text($"{vendor.Cost} {vendor.CostType}");
                    ImGui.TableNextColumn();
                }
            }
            else
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("This probably isn't");
                ImGui.TableNextColumn();
                ImGui.Text("obtained from NPCs");
            }

            ImGui.EndTable();
            // ImGui.Separator();

            #endregion
        }

        ImGui.SetCursorPosY(ImGui.GetWindowContentRegionMax().Y - ImGui.GetTextLineHeightWithSpacing());
        ImGui.Text("Data provided by FFXIV Console Games Wiki (https://ffxiv.consolegameswiki.com)");

        ImGui.EndChild();
    }

    protected internal async Task ChangeSelectedItem(uint itemId)
    {
        _selectedItem = _pluginServiceFactory.Create<ItemManagerService>().RetrieveItem(itemId);
        var iconId = _selectedItem.Icon;
        var iconTexFile = _pluginServiceFactory.Create<DataManager>().GetIcon(iconId);
        _selectedItemIcon?.Dispose();
        if (iconTexFile is not null)
            _selectedItemIcon = await _pluginServiceFactory.Create<DalamudPluginInterface>().UiBuilder
                                                           .LoadImageRawAsync(iconTexFile.GetRgbaImageData(),
                                                                              iconTexFile.Header.Width,
                                                                              iconTexFile.Header.Height, 4);
        try
        {
            _lootData = default;
            var token = _tokenSource.Token;
            _lootData = await _pluginServiceFactory.Create<ScrapperClient>().GetLootData(_selectedItem.Name, token)
                                                   .ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
        }
        catch (System.OperationCanceledException e)
        {
            PluginLog.Error(e, "Request for loot $1 info failed", _selectedItem.Name);
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