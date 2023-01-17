﻿using System;
using Dalamud;
using Dalamud.ContextMenu;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Windowing;
using MonsterLootHunter.Services;
using MonsterLootHunter.Utils;
using MonsterLootHunter.Windows;

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
    }

    private void DisableIntegration()
    {
        _contextMenu.OnOpenInventoryContextMenu -= AddInventoryItem;
    }

    private void AddInventoryItem(InventoryContextMenuOpenArgs args)
    {
        if (!_pluginServiceFactory.Create<Configuration>().ContextMenuIntegration) return;
        var menuItem = CheckItem(args.ItemId);
        if (menuItem != null) args.AddCustomItem(menuItem);
    }

    private InventoryContextMenuItem CheckItem(uint itemId)
    {
        var pluginWindow = _pluginServiceFactory.Create<WindowSystem>().GetWindow(PluginConstants.MainWindowName);
        if (pluginWindow is not PluginUi window) return null;
        itemId = itemId > 500000 ? itemId - 500000 : itemId;
        if (!_pluginServiceFactory.Create<ItemManagerService>().CheckSelectedItem(itemId)) return null;
        return new InventoryContextMenuItem(SearchString, _ =>
        {
            window.IsOpen = true;
            window.ChangeSelectedItem(itemId);
        }, true);
    }

    public void Dispose()
    {
        DisableIntegration();
        _contextMenu?.Dispose();
        GC.SuppressFinalize(this);
    }
}