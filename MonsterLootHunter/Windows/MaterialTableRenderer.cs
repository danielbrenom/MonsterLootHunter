using System.Collections.Generic;
using System.Linq;
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

    public MaterialTableRenderer(MapManagerService mapManagerService, float scale, Vector2 textSize)
    {
        _mapManagerService = mapManagerService;
        _scale = scale;
        _textSize = textSize;
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

        if (ImGui.BeginTable("MLH_ObtainedFromTable", 4, ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.NoHostExtendX,
                             new Vector2(0f, _textSize.Y * 13)))
        {
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 200.0f);
            ImGui.TableSetupColumn("Location", ImGuiTableColumnFlags.WidthFixed, 230.0f);
            ImGui.TableSetupColumn("Position", ImGuiTableColumnFlags.WidthFixed, 100.0f);
            ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 40.0f);
            ImGui.TableHeadersRow();

            foreach (var mob in mobList)
            {
                var index = mobList.IndexOf(mob);
                ImGui.TableNextRow(ImGuiTableRowFlags.None, _textSize.Y * 1.5f);
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
        ImGui.SetColumnWidth(2, 100.0f);
        ImGui.SetColumnWidth(3, 40.0f);
        ImGui.Separator();
        ImGui.Text("Name");
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

        if (ImGui.BeginTable("MLH_PurchasedFromTable", 4, ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.NoHostExtendX,
                             new Vector2(0f, _textSize.Y * 13)))
        {
            ImGui.TableSetupColumn("Vendor", ImGuiTableColumnFlags.WidthFixed, 200f);
            ImGui.TableSetupColumn("Location", ImGuiTableColumnFlags.WidthFixed, 150f);
            ImGui.TableSetupColumn("Position", ImGuiTableColumnFlags.WidthFixed, 100f);
            ImGui.TableSetupColumn("Price", ImGuiTableColumnFlags.WidthFixed, 200f);
            ImGui.TableHeadersRow();

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