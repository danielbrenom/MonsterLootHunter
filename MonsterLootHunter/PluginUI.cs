using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Logging;
using Dalamud.Utility;
using ImGuiNET;
using ImGuiScene;
using Lumina.Excel.GeneratedSheets;
using MonsterLootHunter.Helpers;

namespace MonsterLootHunter
{
    public class PluginUI : IDisposable
    {
        private readonly Configuration configuration;
        private readonly IEnumerable<Item> _items;
        private Dictionary<ItemSearchCategory, List<Item>> _sortedCategoriesAndItems;

        // this extra bool exists for ImGui, since you can't ref a property
        private bool visible;

        public bool Visible
        {
            get => visible;
            set => visible = value;
        }

        private bool settingsVisible;

        public bool SettingsVisible
        {
            get => settingsVisible;
            set => settingsVisible = value;
        }

        private string _searchString = string.Empty;

        public string SearchString
        {
            get => _searchString;
            set => _searchString = value;
        }

        private bool _searchHistoryOpen = false;

        public bool SearchHistoryOpen
        {
            get => _searchHistoryOpen;
            set => _searchHistoryOpen = value;
        }

        private Item _selectedItem;
        private TextureWrap _selectedItemIcon;
        private List<KeyValuePair<ItemSearchCategory, List<Item>>> _enumerableCategoriesAndItems;

        // passing in the image here just for simplicity
        public PluginUI(Configuration configuration)
        {
            this.configuration = configuration;
            _items = Plugin.DataManager.GetExcelSheet<Item>()!;
            _sortedCategoriesAndItems = SortCategoriesAndItems();
        }

        public void Dispose()
        {
        }

        public void Draw()
        {
            // This is our only draw handler attached to UIBuilder, so it needs to be
            // able to draw any windows we might have open.
            // Each method checks its own visibility/state to ensure it only draws when
            // it actually makes sense.
            // There are other ways to do this, but it is generally best to keep the number of
            // draw delegates as low as possible.

            DrawMainWindow();
            DrawSettingsWindow();
        }

        private bool DrawMainWindow()
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

            _enumerableCategoriesAndItems = _sortedCategoriesAndItems.ToList();

            var scale = ImGui.GetIO().FontGlobalScale;
            ImGui.SetNextWindowSize(new Vector2(375, 330) * scale, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(375, 330) * scale, new Vector2(float.MaxValue, float.MaxValue) * scale);
            if (!ImGui.Begin("Monster Loot Hunter", ref visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.End();
                return Visible;
            }

            ImGui.BeginChild("lootListColumn", new Vector2(267, 0) * scale, true);
            ImGui.SetNextItemWidth((-32 * ImGui.GetIO().FontGlobalScale) - ImGui.GetStyle().ItemSpacing.X);
            ImGuiOverrides.InputTextWithHint("##searchString", "Search for loot", ref _searchString, 256);
            ImGui.Separator();

            ImGui.BeginChild("itemTree", new Vector2(0, -2.0f * ImGui.GetFrameHeightWithSpacing()), false, ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.AlwaysHorizontalScrollbar);
            var itemTextSize = ImGui.CalcTextSize(string.Empty);

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

            ImGui.EndChild();
            ImGui.EndChild();
            ImGui.End();
            return Visible;
        }

        private void DrawSettingsWindow()
        {
            if (!SettingsVisible)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(232, 75), ImGuiCond.Always);
            if (ImGui.Begin("A Wonderful Configuration Window", ref settingsVisible,
                            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                // can't ref a property, so use a local copy
                var configValue = configuration.SomePropertyToBeSavedAndWithADefault;
                if (ImGui.Checkbox("Random Config Bool", ref configValue))
                {
                    configuration.SomePropertyToBeSavedAndWithADefault = configValue;
                    // can save immediately on change, if you don't want to provide a "Save and Close" button
                    configuration.Save();
                }
            }

            ImGui.End();
        }

        private void ChangeSelectedItem(uint itemId, bool noHistory = false)
        {
            _selectedItem = _items.Single(i => i.RowId == itemId);

            var iconId = _selectedItem.Icon;
            var iconTexFile = Plugin.DataManager.GetIcon(iconId);
            _selectedItemIcon?.Dispose();
            _selectedItemIcon = Plugin.PluginInterface.UiBuilder.LoadImageRaw(iconTexFile.GetRgbaImageData(), iconTexFile.Header.Width, iconTexFile.Header.Height, 4);
        }

        private Dictionary<ItemSearchCategory, List<Item>> SortCategoriesAndItems()
        {
            try
            {
                var itemSearchCategories = Plugin.DataManager.GetExcelSheet<ItemSearchCategory>();


                var sortedCategories = itemSearchCategories.Where(c => c.Category > 0).OrderBy(c => c.Category).ThenBy(c => c.Order);

                var sortedCategoriesDict = new Dictionary<ItemSearchCategory, List<Item>>();

                var leatherSuffixes = new List<string> { "skin", "hide" };
                var leatherPattern = string.Join("|", leatherSuffixes.Select(System.Text.RegularExpressions.Regex.Escape));
                var leatherRegex = new System.Text.RegularExpressions.Regex(leatherPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                var reagentsSuffixes = new List<string> { "secretion" };
                var reagentsPattern = string.Join("|", reagentsSuffixes.Select(System.Text.RegularExpressions.Regex.Escape));
                var reagentsRegex = new System.Text.RegularExpressions.Regex(reagentsPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                foreach (var c in sortedCategories)
                {
                    switch (c.Name)
                    {
                        case "Leather":
                            sortedCategoriesDict.Add(c, _items.Where(i => i.ItemSearchCategory.Row == c.RowId).Where(i => leatherRegex.IsMatch(i.Name)).OrderBy(i => i.Name.ToString()).ToList());
                            break;
                        case "Reagents":
                            sortedCategoriesDict.Add(c, _items.Where(i => i.ItemSearchCategory.Row == c.RowId && reagentsRegex.IsMatch(i.Name)).OrderBy(i => i.Name.ToString()).ToList());
                            break;
                        default:
                            continue;
                    }
                }

                return sortedCategoriesDict;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Error loading category list.");
                return default;
            }
        }
    }
}