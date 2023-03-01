using System;
using System.Threading.Tasks;
using Dalamud;
using Dalamud.ContextMenu;
using Dalamud.Game.Gui;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using MonsterLootHunter.Services;
using MonsterLootHunter.Utils;
using MonsterLootHunter.Windows;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace MonsterLootHunter.Logic;

public class ContextMenu : IServiceType, IDisposable
{
    private readonly PluginServiceFactory _pluginServiceFactory;
    private readonly DalamudContextMenu _contextMenu = new();
    private static readonly SeString SearchString = new(new TextPayload("Loot search"));

    public ContextMenu(PluginServiceFactory pluginServiceFactory)
    {
        _pluginServiceFactory = pluginServiceFactory;
        EnableIntegration();
    }

    private void EnableIntegration()
    {
        _contextMenu.OnOpenInventoryContextMenu += AddInventoryItem;
        _contextMenu.OnOpenGameObjectContextMenu += AddGameObjectItem;
    }

    private void DisableIntegration()
    {
        _contextMenu.OnOpenInventoryContextMenu -= AddInventoryItem;
        _contextMenu.OnOpenGameObjectContextMenu -= AddGameObjectItem;
    }

    private void AddInventoryItem(InventoryContextMenuOpenArgs args)
    {
        if (!_pluginServiceFactory.Create<Configuration>().ContextMenuIntegration) return;
        var menuItem = CheckItem(args.ItemId);
        if (menuItem != null) args.AddCustomItem(menuItem);
    }

    private void AddGameObjectItem(GameObjectContextMenuOpenArgs args)
    {
        if (!_pluginServiceFactory.Create<Configuration>().ContextMenuIntegration) return;
        var item = args.ParentAddonName switch
        {
            null => null,
            "RecipeNote" => CheckGameObjectItem("RecipeNote", Offsets.RecipeNoteContextItemId),
            "RecipeTree" => CheckGameObjectItem(AgentById(AgentId.RecipeItemContext), Offsets.AgentItemContextItemId),
            "RecipeMaterialList" => CheckGameObjectItem(AgentById(AgentId.RecipeItemContext), Offsets.AgentItemContextItemId),
            "ItemSearch" => CheckGameObjectItem(args.Agent, Offsets.ItemSearchContextItemId),
            _ => null
        };

        if (item != null) args.AddCustomItem(item);
    }

    private InventoryContextMenuItem CheckItem(uint itemId)
    {
        var pluginWindow = _pluginServiceFactory.Create<WindowService>().GetWindow(WindowConstants.MainWindowName);
        if (pluginWindow is not PluginUi window) return null;
        itemId = itemId > 500000 ? itemId - 500000 : itemId;
        if (!_pluginServiceFactory.Create<ItemManagerService>().CheckSelectedItem(itemId)) return null;
        return new InventoryContextMenuItem(SearchString, _ =>
        {
            window.IsOpen = true;
            Task.Run(async () => await window.ChangeSelectedItem(itemId));
        }, true);
    }

    private GameObjectContextMenuItem CheckGameObjectItem(string name, int offset)
        => CheckGameObjectItem(_pluginServiceFactory.Create<GameGui>().FindAgentInterface(name), offset);

    private unsafe GameObjectContextMenuItem CheckGameObjectItem(nint agent, int offset)
    {
        return agent != nint.Zero ? CheckGameObjectItem(*(uint*)(agent + offset)) : null;
    }

    private GameObjectContextMenuItem CheckGameObjectItem(uint itemId)
    {
        var pluginWindow = _pluginServiceFactory.Create<WindowService>().GetWindow(WindowConstants.MainWindowName);
        if (pluginWindow is not PluginUi window) return null;
        itemId = itemId > 500000 ? itemId - 500000 : itemId;
        if (!_pluginServiceFactory.Create<ItemManagerService>().CheckSelectedItem(itemId)) return null;
        return new GameObjectContextMenuItem(SearchString, _ =>
        {
            window.IsOpen = true;
            Task.Run(async () => await window.ChangeSelectedItem(itemId));
        }, true);
    }

    private unsafe nint AgentById(AgentId id)
    {
        var uiModule = (UIModule*)_pluginServiceFactory.Create<GameGui>().GetUIModule();
        var agents = uiModule->GetAgentModule();
        var agent = agents->GetAgentByInternalId(id);
        return (nint)agent;
    }

    public void Dispose()
    {
        DisableIntegration();
        _contextMenu?.Dispose();
        GC.SuppressFinalize(this);
    }
}