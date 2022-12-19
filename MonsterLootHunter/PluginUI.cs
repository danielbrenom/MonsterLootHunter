using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface;
using Dalamud.Utility;
using ImGuiNET;
using ImGuiScene;
using Lumina.Excel.GeneratedSheets;
using MonsterLootHunter.Data;
using MonsterLootHunter.Helpers;

namespace MonsterLootHunter
{
    public class PluginUI : IDisposable
    {
        private Item _selectedItem;
        private TextureWrap _selectedItemIcon;
        private List<KeyValuePair<ItemSearchCategory, List<Item>>> _enumerableCategoriesAndItems;
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

        public PluginUI()
        {
            _selectedItem = new Item();
        }

        public bool Draw()
        {
            if (!Visible)
            {
                return false;
            }

            (_enumerableCategoriesAndItems, _lastSearchString) =
                PluginServices.ItemManager.GetEnumerableItems(_searchString, _searchString != _lastSearchString);

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
                                PluginServices.MapManager.MarkMapFlag(mob.MobLocation, mob.MobFlag);
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
            }

            ImGui.SetCursorPosY(ImGui.GetWindowContentRegionMax().Y - ImGui.GetTextLineHeightWithSpacing());
            ImGui.Text("Data provided by FFXIV Console Games Wiki (https://ffxiv.consolegameswiki.com)");

            ImGui.EndChild();
            ImGui.End();
            return _visible;
        }

        internal void ChangeSelectedItem(uint itemId)
        {
            _selectedItem = PluginServices.ItemManager.RetrieveItem(itemId);
            var iconId = _selectedItem.Icon;
            var iconTexFile = Plugin.DataManager.GetIcon(iconId);
            _selectedItemIcon?.Dispose();
            _selectedItemIcon = Plugin.PluginInterface.UiBuilder.LoadImageRaw(iconTexFile?.GetRgbaImageData() ?? Array.Empty<byte>(), iconTexFile.Header.Width, iconTexFile.Header.Height, 4);
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