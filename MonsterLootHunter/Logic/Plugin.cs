using System;
using Dalamud.Data;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Utility;
using MonsterLootHunter.Helpers;
using MonsterLootHunter.Services;
using MonsterLootHunter.Utils;
using MonsterLootHunter.Windows;

namespace MonsterLootHunter.Logic;

public class Plugin : IDalamudPlugin
{
    public string Name => PluginConstants.CommandName;

    private DalamudPluginInterface PluginInterface { get; init; }
    private CommandManager CommandManager { get; init; }
    private WindowSystem WindowSystem = new(PluginConstants.WindowSystemNamespace);

    public Plugin(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] CommandManager commandManager,
        [RequiredVersion("1.0")] DataManager dataManager,
        [RequiredVersion("1.0")] GameGui gameGui)
    {
        PluginInterface = pluginInterface;
        CommandManager = commandManager;
        PluginServices.Initialize(PluginInterface)
                      .RegisterService(dataManager)
                      .RegisterService(gameGui)
                      .RegisterService<ScrapperClient>()
                      .RegisterService<ItemManagerService>()
                      .RegisterService<MapManagerService>();

        WindowSystem.AddWindow(new PluginUi());

        CommandManager.AddHandler(PluginConstants.CommandSlash, new CommandInfo(OnCommand)
        {
            HelpMessage = PluginConstants.CommandHelperText
        });
        PluginInterface.UiBuilder.Draw += DrawUi;
    }


    private void OnCommand(string command, string args)
    {
        var pluginWindow = WindowSystem.GetWindow(PluginConstants.MainWindowName);
        if (pluginWindow is not PluginUi window) return;
        pluginWindow.IsOpen = true;
        window.SearchString = !args.IsNullOrEmpty() ? args : string.Empty;
    }

    private void DrawUi()
    {
        WindowSystem.Draw();
    }

    private void DrawConfigUi()
    {
        return;
    }

    public void Dispose()
    {
        PluginInterface.SavePluginConfig(PluginServices.Instance.Configuration);
        PluginServices.Dispose();
        CommandManager.RemoveHandler(PluginConstants.CommandSlash);
        WindowSystem.RemoveAllWindows();
        GC.SuppressFinalize(this);
    }
}