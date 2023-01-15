using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Data;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
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
    private Item _selectedItem;
    private TextureWrap _selectedItemIcon;
    private List<KeyValuePair<ItemSearchCategory, List<Item>>> _enumerableCategoriesAndItems;
    private LootData _lootData;
    private readonly float _scale;
    private readonly CancellationTokenSource _tokenSource;

    #region Props

    private string _searchString = string.Empty;

    public string SearchString
    {
        get => _searchString;
        set => _searchString = value;
    }

    private string _lastSearchString = string.Empty;

    public string LastSearchString
    {
        get => _lastSearchString;
        set => _lastSearchString = value;
    }

    #endregion

    public PluginUi() : base(PluginConstants.MainWindowName, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
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
            PluginServices.GetService<ItemManagerService>().GetEnumerableItems(_searchString, _searchString != _lastSearchString);

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

                if (ImGui.IsItemClicked())
                {
                    ChangeSelectedItem(item.RowId);
                }
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
            var pluginWindow = PluginServices.Instance.WindowSystem.GetWindow(PluginConstants.ConfigWindowName);
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

            #region Dropped By Table

            var tableHeight = ImGui.GetContentRegionAvail().Y / 2 - ImGui.GetTextLineHeightWithSpacing() * 2;
            ImGui.Text("Dropped by");
            ImGui.BeginChild("droppedBy", new Vector2(0.0f, tableHeight));
            ImGui.Columns(4, "droppedByColumns");
            ImGui.SetColumnWidth(0, 200.0f);
            ImGui.SetColumnWidth(1, 200.0f);
            ImGui.SetColumnWidth(2, 100.0f);
            ImGui.SetColumnWidth(3, 40.0f);
            ImGui.Separator();
            ImGui.Text("Mob Name");
            ImGui.NextColumn();
            ImGui.Text("Location");
            ImGui.NextColumn();
            ImGui.Text("Position");
            ImGui.NextColumn();
            ImGui.NextColumn();
            ImGui.Separator();


            var mobList = _lootData?.LootLocations.OrderBy(m => m.MobName).ToList();
            if (mobList != null && mobList.Any())
            {
                foreach (var mob in mobList)
                {
                    var index = mobList.IndexOf(mob);
                    ImGui.Text(mob.MobName);
                    ImGui.NextColumn();
                    ImGui.Text(mob.MobLocation);
                    ImGui.NextColumn();
                    ImGui.Text(mob.MobFlag);
                    ImGui.NextColumn();
                    if (!string.IsNullOrEmpty(mob.MobFlag) && mob.MobFlag != "N/A")
                    {
                        ImGui.PushFont(UiBuilder.IconFont);
                        if (ImGui.Button($"{(char)FontAwesomeIcon.MapMarkerAlt}##listing{index}", new Vector2(25 * _scale, ImGui.GetItemRectSize().Y * _scale)))
                            PluginServices.GetService<MapManagerService>().MarkMapFlag(mob.MobLocation, mob.MobFlag);

                        ImGui.PopFont();
                    }

                    ImGui.NextColumn();
                    ImGui.Separator();
                }
            }
            else
            {
                ImGui.Text("The Wiki doesn't have");
                ImGui.NextColumn();
                ImGui.Text("this information");
            }

            ImGui.EndChild();
            ImGui.Separator();

            #endregion

            #region Purchased From Table

            ImGui.Text("Purchased From");
            ImGui.BeginChild("purchasedFrom", new Vector2(0.0f, tableHeight));
            ImGui.Columns(4, "purchasedFromColumns");
            ImGui.SetColumnWidth(2, 100.0f);
            ImGui.Separator();
            ImGui.Text("Vendor");
            ImGui.NextColumn();
            ImGui.Text("Location");
            ImGui.NextColumn();
            ImGui.Text("Position");
            ImGui.NextColumn();
            ImGui.Text("Price");
            ImGui.NextColumn();
            ImGui.Separator();

            var vendorList = _lootData?.LootPurchaseLocations.OrderBy(v => v.Vendor).ToList();
            if (vendorList != null && vendorList.Any())
            {
                foreach (var vendor in vendorList)
                {
                    ImGui.Text(vendor.Vendor);
                    ImGui.NextColumn();
                    ImGui.Text(vendor.Location);
                    ImGui.NextColumn();
                    ImGui.Text(vendor.FlagPosition);
                    ImGui.NextColumn();
                    ImGui.Text($"{vendor.Cost} {vendor.CostType}");
                    ImGui.NextColumn();
                    ImGui.Separator();
                }
            }
            else
            {
                ImGui.Text("The Wiki doesn't have ");
                ImGui.NextColumn();
                ImGui.Text("this information");
            }

            ImGui.EndChild();
            ImGui.Separator();

            #endregion
        }

        ImGui.SetCursorPosY(ImGui.GetWindowContentRegionMax().Y - ImGui.GetTextLineHeightWithSpacing());
        ImGui.Text("Data provided by FFXIV Console Games Wiki (https://ffxiv.consolegameswiki.com)");

        ImGui.EndChild();
    }

    protected internal void ChangeSelectedItem(uint itemId)
    {
        _selectedItem = PluginServices.GetService<ItemManagerService>().RetrieveItem(itemId);
        var iconId = _selectedItem.Icon;
        var iconTexFile = PluginServices.GetService<DataManager>().GetIcon(iconId);
        _selectedItemIcon?.Dispose();
        _selectedItemIcon = PluginServices.Instance.PluginInterface.UiBuilder
                                          .LoadImageRaw(iconTexFile?.GetRgbaImageData() ?? System.Array.Empty<byte>(),
                                                        iconTexFile.Header.Width,
                                                        iconTexFile.Header.Height,
                                                        4);
        Task.Run(async () =>
        {
            try
            {
                _lootData = default;
                var token = _tokenSource.Token;
                _lootData = await PluginServices.GetService<ScrapperClient>().GetLootData(_selectedItem.Name, token)
                                                .ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
            }
            catch (System.OperationCanceledException e)
            {
                PluginLog.Error(e, "Request for loot info failed", _selectedItem.Name);
            }
        });
    }

    public void Dispose()
    {
        _selectedItemIcon?.Dispose();
        System.GC.SuppressFinalize(this);
    }
}