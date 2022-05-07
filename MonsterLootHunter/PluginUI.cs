using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface;
using Dalamud.Logging;
using Dalamud.Utility;
using ImGuiNET;
using ImGuiScene;
using Lumina.Excel.GeneratedSheets;
using MonsterLootHunter.Data;
using MonsterLootHunter.Helpers;
using MonsterLootHunter.Utils;

namespace MonsterLootHunter
{
    public class PluginUI : IDisposable
    {
        private readonly Configuration _configuration;
        private readonly IEnumerable<Item> _items;
        private Dictionary<ItemSearchCategory, List<Item>> _sortedCategoriesAndItems;
        private Item _selectedItem;
        private TextureWrap _selectedItemIcon;
        private List<KeyValuePair<ItemSearchCategory, List<Item>>> _enumerableCategoriesAndItems;
        private List<TerritoryType> _territoryTypes;
        private LootData _lootData;

        #region Props

        private bool _visible;

        public bool Visible
        {
            get => _visible;
            set => _visible = value;
        }

        private bool _settingsVisible;

        public bool SettingsVisible
        {
            get => _settingsVisible;
            set => _settingsVisible = value;
        }

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
        private bool _contextMenuIntegration;

        public bool ContextMenuIntegration
        {
            get => _contextMenuIntegration;
            set => _contextMenuIntegration = value;
        }

        #endregion

        public PluginUI(Configuration configuration)
        {
            _configuration = configuration;
            _items = Plugin.DataManager.GetExcelSheet<Item>();
            _sortedCategoriesAndItems = SortCategoriesAndItems();
            _territoryTypes = GetTerritories();
            _selectedItem = new Item();
        }

        public bool Draw()
        {
            if (!Visible)
            {
                return false;
            }

            if (_sortedCategoriesAndItems == null)
            {
                _sortedCategoriesAndItems = SortCategoriesAndItems();
                return true;
            }

            _enumerableCategoriesAndItems ??= _sortedCategoriesAndItems.ToList();

            if (_searchString != _lastSearchString)
            {
                _enumerableCategoriesAndItems = string.IsNullOrEmpty(_searchString)
                    ? _sortedCategoriesAndItems.ToList()
                    : _sortedCategoriesAndItems
                     .Select(kv => new KeyValuePair<ItemSearchCategory, List<Item>>(
                                 kv.Key,
                                 kv.Value
                                   .Where(i =>
                                              i.Name.ToString().ToUpperInvariant().Contains(_searchString.ToUpperInvariant(), StringComparison.InvariantCulture))
                                   .ToList()))
                     .Where(kv => kv.Value.Count > 0)
                     .ToList();
                _lastSearchString = _searchString;
            }

            var scale = ImGui.GetIO().FontGlobalScale;
            ImGui.SetNextWindowSize(new Vector2(375, 330) * scale, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(375, 330) * scale, new Vector2(float.MaxValue, float.MaxValue) * scale);
            if (!ImGui.Begin("Monster Loot Hunter", ref _visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.End();
                return _visible;
            }

            ImGui.BeginChild("lootListColumn", new Vector2(267, 0) * scale, true);
            ImGui.SetNextItemWidth(-32 * ImGui.GetIO().FontGlobalScale - ImGui.GetStyle().ItemSpacing.X);
            ImGui.InputTextWithHint("##searchString", "Search for loot", ref _searchString, 256);
            ImGui.Separator();

            ImGui.BeginChild("itemTree", new Vector2(0, -2.0f * ImGui.GetFrameHeightWithSpacing()), false, ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.AlwaysHorizontalScrollbar);
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
            
            #region Checkboxes

            var contextMenuIntegration = _configuration.ContextMenuIntegration;
            if (ImGui.Checkbox("Context menu integration", ref contextMenuIntegration))
            {
                _configuration.ContextMenuIntegration = contextMenuIntegration;
                // Plugin.PluginInterface.SavePluginConfig(_configuration);
                _configuration.Save();
                Plugin.ChatGui.Print($"Menu context integrations is {contextMenuIntegration} and configuration is {_configuration.ContextMenuIntegration}");
            }
            #endregion
            
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
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImGui.GetFontSize() / 2.0f + 19 * scale);
                ImGui.Text(_selectedItem?.Name ?? string.Empty);
                if (ImGui.BeginTabBar("tabBar"))
                {
                    if (ImGui.BeginTabItem("Drop Table##dropTableTab"))
                    {
                        #region Dropped By Table

                        var tableHeight = (ImGui.GetContentRegionAvail().Y / 2) - (ImGui.GetTextLineHeightWithSpacing() * 2);
                        ImGui.Text("Dropped by");
                        ImGui.BeginChild("droppedBy", new Vector2(0.0f, tableHeight));
                        ImGui.Columns(4, "droppedByColumns");
                        ImGui.SetColumnWidth(0, 200.0f);
                        ImGui.SetColumnWidth(1, 200.0f);
                        ImGui.SetColumnWidth(2, 200.0f);
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
                                    if (ImGui.Button($"{(char)FontAwesomeIcon.MapPin}##listing{index}", new Vector2(24 * ImGui.GetIO().FontGlobalScale, ImGui.GetItemRectSize().Y)))
                                    {
                                        MarkMapFlag(mob.MobLocation, mob.MobFlag);
                                    }

                                    ImGui.PopFont();
                                }

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

                        #region Purchased From Table

                        ImGui.Text("Purchased From");
                        ImGui.BeginChild("purchasedFrom", new Vector2(0.0f, tableHeight));
                        ImGui.Columns(4, "purchasedFromColumns");
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

                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }
            }

            ImGui.SetCursorPosY(ImGui.GetWindowContentRegionMax().Y - ImGui.GetTextLineHeightWithSpacing());
            ImGui.Text("Data provided by FFXIV Console Games Wiki (https://ffxiv.consolegameswiki.com)");

            ImGui.EndChild();
            ImGui.End();
            return _visible;
        }

        private void DrawSettingsWindow()
        {
            if (!SettingsVisible)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(232, 75), ImGuiCond.Always);
            if (ImGui.Begin("Configuration", ref _settingsVisible,
                            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                // can't ref a property, so use a local copy
                
            }

            ImGui.End();
        }

        internal void ChangeSelectedItem(uint itemId)
        {
            _selectedItem = _items.Single(i => i.RowId == itemId);
            var iconId = _selectedItem.Icon;
            var iconTexFile = Plugin.DataManager.GetIcon(iconId);
            _selectedItemIcon?.Dispose();
            _selectedItemIcon = Plugin.PluginInterface.UiBuilder.LoadImageRaw(iconTexFile?.GetRgbaImageData() ?? Array.Empty<byte>(), iconTexFile.Header.Width, iconTexFile.Header.Height, 4);
            RefreshLootData();
        }

        private Dictionary<ItemSearchCategory, List<Item>> SortCategoriesAndItems()
        {
            try
            {
                var itemSearchCategories = Plugin.DataManager.GetExcelSheet<ItemSearchCategory>();
                if (itemSearchCategories is null) return default;
                var sortedCategories = itemSearchCategories.Where(c => c.Category > 0).OrderBy(c => c.Category).ThenBy(c => c.Order);
                var sortedCategoriesDict = new Dictionary<ItemSearchCategory, List<Item>>();

                foreach (var c in sortedCategories)
                {
                    switch (c.Name)
                    {
                        case LootIdentifierConstants.Leather:
                            sortedCategoriesDict.Add(c, _items.Where(i => i.ItemSearchCategory.Row == c.RowId).Where(i => LootIdentifierConstants.LeatherRegex.IsMatch(i.Name)).OrderBy(i => i.Name.ToString()).ToList());
                            break;
                        case LootIdentifierConstants.Reagents:
                        case LootIdentifierConstants.Bone:
                        case LootIdentifierConstants.Ingredients:
                            sortedCategoriesDict.Add(c, _items.Where(i => i.ItemSearchCategory.Row == c.RowId).OrderBy(i => i.Name.ToString()).ToList());
                            break;
                        default:
                            continue;
                    }
                }

                return sortedCategoriesDict;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Error loading category list.");
                return default;
            }
        }

        private static List<TerritoryType> GetTerritories()
        {
            return Plugin.DataManager.GetExcelSheet<TerritoryType>().ToList();
        }

        private void MarkMapFlag(string locationName, string position)
        {
            try
            {
                var location = _territoryTypes.FirstOrDefault(t => t.PlaceName.Value.Name.ToString().ToLowerInvariant().Contains(locationName.ToLowerInvariant()));
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
                Plugin.ChatGui.Print($"Not able to mark location {locationName} on map.");
            }
        }

        private void RefreshLootData()
        {
            Task.Run(async () =>
            {
                _lootData = default;
                _lootData = await ScrapperClient.GetLootData(_selectedItem.Name, CancellationToken.None)
                                                .ConfigureAwait(false);
            });
        }

        public void Dispose()
        {
            _selectedItemIcon?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}