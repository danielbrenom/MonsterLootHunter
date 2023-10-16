using System.Linq.Expressions;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;
using MonsterLootHunter.Data;
using MonsterLootHunter.Services;

namespace MonsterLootHunter.Windows;

public class MaterialTableRenderer
{
    private readonly MapManagerService _mapManagerService;
    private readonly float _scale;
    private readonly Vector2 _textSize;
    private Func<LootDrops, object> _sortPropFunc;
    private Func<LootPurchase, object> _sortVendorPropFunc;
    private ImGuiSortDirection _sortDirection;
    private ImGuiSortDirection _sortVendorDirection;

    public MaterialTableRenderer(MapManagerService mapManagerService, float scale, Vector2 textSize)
    {
        _mapManagerService = mapManagerService;
        _scale = scale;
        _textSize = textSize;
        _sortDirection = ImGuiSortDirection.Ascending;
        _sortVendorDirection = ImGuiSortDirection.Ascending;
    }

    public void RenderMobTable(IList<LootDrops> mobList)
    {
        if (mobList is null || !mobList.Any())
        {
            ImGui.BeginChild("MLH_ObtainedFrom_Empty", new Vector2(0f, _textSize.Y * 13), true);
            ImGui.Text("This probably isn't obtained this way or the Wiki don't have this information");
            ImGui.EndChild();
            return;
        }

        if (ImGui.BeginTable("MLH_ObtainedFromTable", 5, ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.Reorderable | ImGuiTableFlags.Sortable,
                             new Vector2(0f, _textSize.Y * 13)))
        {
            ImGui.TableSetupScrollFreeze(0,1);
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoSort, 200.0f, (uint)LootSortId.Name);
            ImGui.TableSetupColumn("Level", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoSort, 50.0f, (uint)LootSortId.Level);
            ImGui.TableSetupColumn("Location", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultSort, 230.0f, (uint)LootSortId.Location);
            ImGui.TableSetupColumn("Position", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoSort, 100.0f, (uint)LootSortId.Flag);
            ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoSort, 40.0f, (uint)LootSortId.Action);
            ImGui.TableHeadersRow();
            var tableSortSpecs = ImGui.TableGetSortSpecs();
            if (tableSortSpecs.SpecsDirty)
            {
                //TODO: Check how to generate the whole expression not only the prop selector
                var pExpression = Expression.Parameter(typeof(LootDrops));
                _sortDirection = tableSortSpecs.Specs.SortDirection;
                var expression = Enum.Parse<LootSortId>(tableSortSpecs.Specs.ColumnUserID.ToString()) switch
                {
                    LootSortId.Name => Expression.Lambda<Func<LootDrops, object>>(Expression.Property(pExpression, "MobName"), pExpression),
                    LootSortId.Location => Expression.Lambda<Func<LootDrops, object>>(Expression.Property(pExpression, "MobLocation"), pExpression),
                    LootSortId.Level => null,
                    LootSortId.Flag => null,
                    LootSortId.Action => null,
                    _ => Expression.Lambda<Func<LootDrops, object>>(Expression.Property(pExpression, "MobName"), pExpression),
                };
                _sortPropFunc = expression?.Compile();

                tableSortSpecs.SpecsDirty = false;
            }

            if (_sortPropFunc is not null)
                mobList = _sortDirection == ImGuiSortDirection.Ascending ? mobList.OrderBy(_sortPropFunc).ToList() : mobList.OrderByDescending(_sortPropFunc).ToList();

            foreach (var mob in mobList)
            {
                var index = mobList.IndexOf(mob);
                ImGui.TableNextRow(ImGuiTableRowFlags.None, _textSize.Y * 1.5f);
                ImGui.AlignTextToFramePadding();
                ImGui.TableNextColumn();
                ImGui.Text(mob.MobName);
                ImGui.TableNextColumn();
                ImGui.Text(mob.MobLevel);
                ImGui.TableNextColumn();
                ImGui.Text(mob.MobLocation);
                ImGui.TableNextColumn();
                ImGui.Text(mob.MobFlag);
                ImGui.TableNextColumn();
                if (!string.IsNullOrEmpty(mob.MobFlag) && mob.MobFlag != "N/A")
                {
                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button($"{(char)FontAwesomeIcon.MapMarkerAlt}##listing{index}", new Vector2(25 * _scale, _textSize.Y * _scale * 1.5f)))
                        _mapManagerService.MarkMapFlag(mob.MobLocation, mob.MobFlag);

                    ImGui.PopFont();
                }

                ImGui.TableNextColumn();
            }

            ImGui.EndTable();
        }
        else
        {
            ImGui.Text("An error occurred trying to render the table, try again or in the settings enable the legacy viewer");
        }
    }

    public void RenderLegacyMobTable(IList<LootDrops> mobList)
    {
        if (mobList is null || !mobList.Any())
        {
            ImGui.BeginChild("MLH_ObtainedFrom_Legacy_Empty", new Vector2(0f, _textSize.Y * 13), true);
            ImGui.Text("This probably isn't obtained this way or the Wiki don't have this information");
            ImGui.EndChild();
            return;
        }

        ImGui.BeginChild("MLH_ObtainedFrom_Legacy", new Vector2(0f, _textSize.Y * 13));
        ImGui.Columns(4, "ObtainedFrom_LegacyColumns");
        ImGui.SetColumnWidth(0, 200.0f);
        ImGui.SetColumnWidth(1, 230.0f);
        ImGui.SetColumnWidth(2, 50.0f);
        ImGui.SetColumnWidth(3, 100.0f);
        ImGui.SetColumnWidth(4, 40.0f);
        ImGui.Separator();
        ImGui.Text("Name");
        ImGui.NextColumn();
        ImGui.Text("Level");
        ImGui.NextColumn();
        ImGui.Text("Location");
        ImGui.NextColumn();
        ImGui.Text("Position");
        ImGui.NextColumn();
        ImGui.NextColumn();
        ImGui.Separator();

        foreach (var mob in mobList)
        {
            var index = mobList.IndexOf(mob);
            ImGui.Text(mob.MobName);
            ImGui.NextColumn();
            ImGui.Text(mob.MobLevel);
            ImGui.NextColumn();
            ImGui.Text(mob.MobLocation);
            ImGui.NextColumn();
            ImGui.Text(mob.MobFlag);
            ImGui.NextColumn();
            if (!string.IsNullOrEmpty(mob.MobFlag) && mob.MobFlag != "N/A")
            {
                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button($"{(char)FontAwesomeIcon.MapMarkerAlt}##listing{index}", new Vector2(25 * _scale, ImGui.GetItemRectSize().Y * _scale)))
                    _mapManagerService.MarkMapFlag(mob.MobLocation, mob.MobFlag);
                ImGui.PopFont();
            }

            ImGui.NextColumn();
            ImGui.Separator();
        }

        ImGui.EndChild();
    }

    public void RenderVendorTable(IList<LootPurchase> vendorList)
    {
        if (vendorList is null || !vendorList.Any())
        {
            ImGui.BeginChild("MLH_PurchasedFrom_Empty", new Vector2(0f, _textSize.Y * 13), true);
            ImGui.Text("This probably isn't obtained this way or the Wiki don't have this information");
            ImGui.EndChild();
            return;
        }

        if (ImGui.BeginTable("MLH_PurchasedFromTable", 4, ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.Reorderable | ImGuiTableFlags.Sortable,
                             new Vector2(0f, _textSize.Y * 13)))
        {
            ImGui.TableSetupScrollFreeze(0,1);
            ImGui.TableSetupColumn("Vendor", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoSort, 200f, (uint)LootSortId.Name);
            ImGui.TableSetupColumn("Location", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultSort, 150f, (uint)LootSortId.Location);
            ImGui.TableSetupColumn("Position", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoSort, 100f, (uint)LootSortId.Flag);
            ImGui.TableSetupColumn("Price", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoSort, 200f, (uint)LootSortId.Action);
            ImGui.TableHeadersRow();

            var tableSortSpecs = ImGui.TableGetSortSpecs();
            if (tableSortSpecs.SpecsDirty)
            {
                var pExpression = Expression.Parameter(typeof(LootPurchase));
                _sortVendorDirection = tableSortSpecs.Specs.SortDirection;
                var expression = Enum.Parse<LootSortId>(tableSortSpecs.Specs.ColumnUserID.ToString()) switch
                {
                    LootSortId.Name => Expression.Lambda<Func<LootPurchase, object>>(Expression.Property(pExpression, "Vendor"), pExpression),
                    LootSortId.Location => Expression.Lambda<Func<LootPurchase, object>>(Expression.Property(pExpression, "Location"), pExpression),
                    LootSortId.Level => null,
                    LootSortId.Flag => null,
                    LootSortId.Action => null,
                    _ => Expression.Lambda<Func<LootPurchase, object>>(Expression.Property(pExpression, "Vendor"), pExpression),
                };
                _sortVendorPropFunc = expression?.Compile();

                tableSortSpecs.SpecsDirty = false;
            }

            if (_sortVendorPropFunc is not null)
                vendorList = _sortVendorDirection == ImGuiSortDirection.Ascending ? vendorList.OrderBy(_sortVendorPropFunc).ToList() : vendorList.OrderByDescending(_sortVendorPropFunc).ToList();

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

            ImGui.EndTable();
        }
        else
        {
            ImGui.Text("An error occurred trying to render the table, try again or in the settings set to use legacy viewer");
        }
    }

    public void RenderLegacyVendorTable(IList<LootPurchase> vendorList)
    {
        if (vendorList is null || !vendorList.Any())
        {
            ImGui.BeginChild("MLH_PurchasedFrom_Legacy_Empty", new Vector2(0f, _textSize.Y * 13), true);
            ImGui.Text("This probably isn't obtained this way or the Wiki don't have this information");
            ImGui.EndChild();
            return;
        }

        ImGui.BeginChild("MLH_PurchasedFromTable_Legacy", new Vector2(0f, _textSize.Y * 13));
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

        ImGui.EndChild();
        ImGui.Separator();
    }
}