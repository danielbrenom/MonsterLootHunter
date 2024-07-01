using Dalamud;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using MonsterLootHunter.Services;
using MonsterLootHunter.Utils;
using MonsterLootHunter.Windows;

namespace MonsterLootHunter.Logic;

public class ContextMenu : IServiceType, IDisposable
{
    private readonly IContextMenu _contextMenu;
    private readonly IGameGui _gameGui;
    private readonly WindowService _windowService;
    private readonly Configuration _configuration;
    private readonly ItemManagerService _itemManagerService;
    private static readonly SeString SearchString = new(new TextPayload("Loot search"));

    public ContextMenu(IContextMenu contextMenu, IGameGui gameGui, WindowService windowService, Configuration configuration, ItemManagerService itemManagerService)
    {
        _contextMenu = contextMenu;
        _gameGui = gameGui;
        _windowService = windowService;
        _configuration = configuration;
        _itemManagerService = itemManagerService;
        EnableIntegration();
    }

    private void EnableIntegration()
    {
        _contextMenu.OnMenuOpened += AddInventoryItem;
    }

    private void DisableIntegration()
    {
        _contextMenu.OnMenuOpened -= AddInventoryItem;
    }

    private void AddInventoryItem(IMenuOpenedArgs args)
    {
        if (!_configuration.ContextMenuIntegration)
            return;

        var menuItem = CreateMenuItem(args);
        if (menuItem != null)
            args.AddMenuItem(menuItem);
    }

    private MenuItem? CreateMenuItem(IMenuArgs args)
    {
        return args.Target switch
        {
            MenuTargetDefault => CreateGameObjectItem(args),
            MenuTargetInventory inventory => CreateMenuItem(inventory.TargetItem?.ItemId),
            _ => null
        };
    }

    private MenuItem? CreateMenuItem(ulong? itemId)
    {
        var pluginWindow = _windowService.GetWindow(WindowConstants.MainWindowName);
        if (pluginWindow is not PluginUi window || itemId is null)
            return null;

        itemId = itemId > 500000 ? itemId - 500000 : itemId;
        if (!_itemManagerService.CheckSelectedItem(itemId.Value))
            return null;

        return new MenuItem
        {
            Name = SearchString,
            PrefixChar = 'M',
            OnClicked = _ =>
            {
                window.IsOpen = true;
                Task.Run(async () => await window.ChangeSelectedItem(itemId.Value));
            }
        };
    }

    private MenuItem? CreateGameObjectItem(IMenuArgs args)
    {
        return args.AddonName switch
        {
            null => null,
            "RecipeNote" => CheckGameObjectItem("RecipeNote", Offsets.RecipeNoteContextItemId),
            "RecipeTree" => CheckGameObjectItem(AgentById(AgentId.RecipeItemContext), Offsets.AgentItemContextItemId),
            "RecipeMaterialList" => CheckGameObjectItem(AgentById(AgentId.RecipeItemContext), Offsets.AgentItemContextItemId),
            "ItemSearch" => CheckGameObjectItem(args.AgentPtr, Offsets.ItemSearchContextItemId),
            _ => null
        };
    }

    private MenuItem? CheckGameObjectItem(string name, int offset)
        => CheckGameObjectItem(_gameGui.FindAgentInterface(name), offset);

    private unsafe MenuItem? CheckGameObjectItem(nint agent, int offset)
        => agent != nint.Zero ? CreateMenuItem(*(uint*)(agent + offset)) : null;

    private unsafe nint AgentById(AgentId id)
    {
        var uiModule = (UIModule*)_gameGui.GetUIModule();
        if (uiModule is null)
            return nint.Zero;

        var agents = uiModule->GetAgentModule();
        if (agents is null)
            return nint.Zero;

        var agent = agents->GetAgentByInternalId(id);
        return (nint)agent;
    }

    public void Dispose()
    {
        DisableIntegration();
        GC.SuppressFinalize(this);
    }
}
