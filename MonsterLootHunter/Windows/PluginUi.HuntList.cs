using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Utility;
using ImGuiNET;
using MonsterLootHunter.Data;
using MonsterLootHunter.Services;

namespace MonsterLootHunter.Windows;

public partial class PluginUi
{
    private readonly HuntListService _huntListService;
    private string _selectedList;
    private IList<HuntListItem> _selectedListItems;
    private string _searchListString = string.Empty;
    private string _searchEditItem = string.Empty;
    private bool EditingList { get; set; }
    private bool ShowItemSelector { get; set; }
    private HuntList EditableElement { get; set; } = new("placeholder");
    private HuntListItem EditableHuntItem { get; set; } = new();

    private int _editableQuantity;

    private void DrawHuntList()
    {
        var huntLists = _searchListString.IsNullOrEmpty() ? _huntListService.GetHuntListNames() : _huntListService.GetHuntListNames().Where(ln => ln.Contains(_searchListString, StringComparison.OrdinalIgnoreCase));

        #region Selection Column

        ImGui.BeginChild("huntListColumn", new Vector2(200, 0) * _scale, true);
        ImGui.InputTextWithHint("##listsearchString", "Filter...", ref _searchListString, 200);
        ImGui.Separator();

        #region Hunt Lists Selector

        ImGui.BeginChild("itemTree", new Vector2(0, -1.0f * ImGui.GetFrameHeightWithSpacing()), false, ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.AlwaysHorizontalScrollbar);
        foreach (var list in huntLists)
        {
            if (!ImGui.Selectable(list)) continue;
            ChangeSelectedList(list);
        }

        ImGui.EndChild();

        #endregion

        ImGui.PushFont(UiBuilder.IconFont);
        if (ImGui.Button($"{(char)FontAwesomeIcon.Plus}"))
        {
            EditList(null);
        }

        ImGui.SameLine();
        if (_selectedList.IsNullOrEmpty())
            ImGui.BeginDisabled();
        if (ImGui.Button($"{(char)FontAwesomeIcon.PencilAlt}"))
        {
            EditList(_huntListService.GetHuntList(_selectedList));
        }

        if (_selectedList.IsNullOrEmpty())
            ImGui.EndDisabled();
        ImGui.PopFont();

        ImGui.EndChild();

        #endregion


        ImGui.SameLine();
        ImGui.BeginChild("panelColumn", new Vector2(0, 0), true, ImGuiWindowFlags.NoScrollbar);
        if (!_selectedList.IsNullOrEmpty() && !EditingList)
            RenderList();
        else if (EditingList)
            RenderEditor();

        ImGui.EndChild();
    }

    private void EditList(HuntList huntList)
    {
        EditingList = true;
        EditableElement = huntList ?? new("New list");
    }

    private void ChangeSelectedList(string listName)
    {
        EditingList = false;
        _selectedList = listName;
        _selectedListItems = _huntListService.GetHuntList(_selectedList).HuntListItems.ToList();
    }

    private void RenderList()
    {
        ImGui.SetCursorPos(new Vector2(ImGui.GetCursorPosY() - ImGui.GetFontSize() / 2.0f + 20 * _scale,
                                       ImGui.GetCursorPosY() - ImGui.GetFontSize() / 2.0f + 19 * _scale));
        ImGui.Text(_selectedList);

        #region Table

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImGui.GetFontSize() / 2.0f + 20 * _scale);
        if (!ImGui.BeginTable("droppedBy", 3, ImGuiTableFlags.BordersInner | ImGuiTableFlags.NoHostExtendX)) return;
        ImGui.TableSetupColumn("options", ImGuiTableColumnFlags.WidthFixed, 100f);
        ImGui.TableSetupColumn("name", ImGuiTableColumnFlags.WidthFixed, 200f);
        ImGui.TableSetupColumn("location", ImGuiTableColumnFlags.WidthFixed, 150f);
        ImGui.Separator();
        if (_selectedListItems.Any())
        {
            foreach (var huntListItem in _selectedListItems)
            {
                ImGui.TableNextRow();
                ImGui.AlignTextToFramePadding();
                ImGui.TableNextColumn();
                var internalIndex = _selectedListItems.IndexOf(huntListItem);
                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button($"{(char)FontAwesomeIcon.Trash}##item{internalIndex}", new Vector2(25 * _scale, 25 * _scale)))
                {
                }

                ImGui.PopFont();
                ImGui.TableNextColumn();
                ImGui.Text(huntListItem.Name);
                ImGui.TableNextColumn();

                var itemEnabled = huntListItem.Enabled;
                // if (ImGui.Checkbox($"###itemEnable{internalIndex}", ref itemEnabled))
                // {
                //     huntListItem.Enabled = itemEnabled;
                //     // UpdateItemFromList(_selectedList, internalIndex);
                // }
                ImGui.Text("TESTE");
                // ImGui.Text(item.Name);
                ImGui.TableNextColumn();


                // ImGui.TableNextColumn();
                // if (ImGui.DragInt($"##quantity{internalIndex}", ref itemQuantity, 1f, 1, 99, "%d", ImGuiSliderFlags.AlwaysClamp))
                //     huntListItem.Quantity = itemQuantity < 0 ? 0 : itemQuantity;
            }
        }

        ImGui.EndTable();

        #endregion
    }

    private void RenderEditor()
    {
        ImGui.SetCursorPos(new Vector2(ImGui.GetCursorPosX() - ImGui.GetFontSize() / 2.0f + 20 * _scale,
                                       ImGui.GetCursorPosY() - ImGui.GetFontSize() / 2.0f + 19 * _scale));
        var editableElementName = EditableElement.Name;
        ImGui.InputText("###editableName", ref editableElementName, 100);

        #region Table

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImGui.GetFontSize() / 2.0f + 20 * _scale);
        if (!ImGui.BeginTable("droppedBy", 5, ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.NoHostExtendX,
                              new Vector2(500 * _scale, _itemTextSize.Y * 13))) return;
        ImGui.TableSetupColumn("Name###name", ImGuiTableColumnFlags.WidthFixed, 200f);
        ImGui.TableSetupColumn("###enabled", ImGuiTableColumnFlags.WidthFixed, 50f);
        ImGui.TableSetupColumn("Quantity###quantity", ImGuiTableColumnFlags.WidthFixed, 50f);
        ImGui.TableSetupColumn("Owned###owned", ImGuiTableColumnFlags.WidthFixed, 50f);
        ImGui.TableSetupColumn("###options", ImGuiTableColumnFlags.WidthFixed, 100f);
        ImGui.TableHeadersRow();
        if (_selectedListItems.Any())
        {
            foreach (var huntListItem in _selectedListItems)
            {
                ImGui.TableNextRow();
                ImGui.AlignTextToFramePadding();
                ImGui.TableNextColumn();
                var internalIndex = _selectedListItems.IndexOf(huntListItem);

                if (huntListItem.Editing)
                {
                    if (ImGui.Button($"{huntListItem.Name}###editName{internalIndex}"))
                    {
                        ShowItemSelector = true;
                    }
                }
                else
                    ImGui.Text(huntListItem.Name);

                ImGui.TableNextColumn();

                var itemEnabled = huntListItem.Enabled;
                if (huntListItem.Editing)
                    ImGui.BeginDisabled();
                if (ImGui.Checkbox($"###itemEnable{internalIndex}", ref itemEnabled))
                {
                    huntListItem.Enabled = itemEnabled;
                    // UpdateItemFromList(_selectedList, internalIndex);
                }

                if (huntListItem.Editing)
                    ImGui.EndDisabled();

                ImGui.TableNextColumn();
                if (huntListItem.Editing)
                    ImGui.InputInt("###quantity", ref _editableQuantity,1,1, ImGuiInputTextFlags.None);
                else
                    ImGui.Text(huntListItem.Quantity.ToString());

                ImGui.TableNextColumn();
                ImGui.Text(huntListItem.OwnedQuantity.ToString());

                ImGui.TableNextColumn();

                #region Options

                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button($"{(char)FontAwesomeIcon.Trash}##item{internalIndex}", new Vector2(25 * _scale, 25 * _scale)))
                {
                }

                ImGui.SameLine();
                if (huntListItem.Editing)
                {
                    if (ImGui.Button($"{(char)FontAwesomeIcon.Save}##saveitem{internalIndex}", new Vector2(25 * _scale, 25 * _scale)))
                    {
                        huntListItem.Editing = false;
                        huntListItem.Quantity = _editableQuantity;
                    }
                }
                else
                {
                    if (ImGui.Button($"{(char)FontAwesomeIcon.PencilAlt}##edititem{internalIndex}", new Vector2(25 * _scale, 25 * _scale)))
                    {
                        huntListItem.Editing = true;
                        EditableHuntItem = huntListItem;
                        _editableQuantity = huntListItem.Quantity;
                    }
                }

                ImGui.PopFont();

                #endregion
            }
        }

        ImGui.EndTable();

        #endregion

        #region Item Selector

        if (ShowItemSelector)
        {
            LoadCategoryItemList();
            ImGui.SameLine();
            ImGui.BeginChild("###itemSelector", new Vector2(0f, _itemTextSize.Y * 13), true);
            ImGui.SetNextItemWidth(ImGui.GetIO().FontGlobalScale - ImGui.GetStyle().ItemSpacing.X);
            ImGui.InputTextWithHint("##searchEditItem", "Search item", ref _searchEditItem, 256);
            ImGui.Separator();
            if (ImGui.BeginListBox("###editItemSelection"))
            {
                foreach (var item in _enumerableCategoriesAndItems.SelectMany(l => l.Value)
                                                                  .Where(i => i.Name.ToString().Contains(_searchEditItem, StringComparison.InvariantCultureIgnoreCase)))
                {
                    var isSelected = EditableHuntItem.ItemId == item.RowId;
                    if (ImGui.Selectable(item.Name, isSelected))
                        ChangeSelectedHuntItem(item.RowId);

                    // Set the initial focus when opening the combo (scrolling + keyboard navigation focus)
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }

                ImGui.EndListBox();
            }

            ImGui.EndChild();
        }

        #endregion

        ImGui.SetCursorPos(new Vector2(ImGui.GetCursorPosX() - ImGui.GetFontSize() / 2.0f + 100 * _scale,
                                       ImGui.GetCursorPosY() - ImGui.GetFontSize() / 2.0f + 5 * _scale));
    }

    private void ChangeSelectedHuntItem(uint id)
    {
        var item = _itemManagerService.RetrieveItem(id);
        EditableHuntItem.ItemId = item.RowId;
        EditableHuntItem.Name = item.Name;
        ShowItemSelector = false;
        //Check the qty in inventory
    }
}