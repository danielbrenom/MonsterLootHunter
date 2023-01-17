using System;
using Dalamud.Data;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Utility;
using MonsterLootHunter.Services;
using MonsterLootHunter.Utils;
using MonsterLootHunter.Windows;

namespace MonsterLootHunter.Logic;

public class Plugin : IDalamudPlugin
{
    public string Name => PluginConstants.CommandName;

    private DalamudPluginInterface PluginInterface { get; init; }
    private CommandManager CommandManager { get; init; }
    private readonly WindowSystem _windowSystem = new(PluginConstants.WindowSystemNamespace);
    private readonly PluginServiceFactory _pluginServiceFactory;

    public Plugin(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] CommandManager commandManager,
        [RequiredVersion("1.0")] DataManager dataManager,
        [RequiredVersion("1.0")] GameGui gameGui)
    {
        PluginInterface = pluginInterface;
        CommandManager = commandManager;
        var configuration = (Configuration)PluginInterface.GetPluginConfig() ?? new Configuration();
        configuration.Initialize(pluginInterface);
        _pluginServiceFactory = new PluginServiceFactory().RegisterService(pluginInterface)
                                                          .RegisterService(_windowSystem)
                                                          .RegisterService(configuration)
                                                          .RegisterService(dataManager)
                                                          .RegisterService(gameGui);
        _pluginServiceFactory.RegisterService(_pluginServiceFactory);
        new PluginModule().Register(_pluginServiceFactory);

        _windowSystem.AddWindow(new ConfigWindow(_pluginServiceFactory));
        _windowSystem.AddWindow(new PluginUi(_pluginServiceFactory));

        CommandManager.AddHandler(PluginConstants.CommandSlash, new CommandInfo(OnCommand)
        {
            HelpMessage = PluginConstants.CommandHelperText
        });
        CommandManager.AddHandler(PluginConstants.ShortCommandSlash, new CommandInfo(OnCommand)
        {
            HelpMessage = PluginConstants.CommandHelperText
        });

        PluginInterface.UiBuilder.Draw += DrawUi;
        PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUi;
    }


    private void OnCommand(string command, string args)
    {
        var pluginWindow = _windowSystem.GetWindow(PluginConstants.MainWindowName);
        if (pluginWindow is not PluginUi window) return;
        pluginWindow.IsOpen = true;
        window.SearchString = !args.IsNullOrEmpty() ? args : string.Empty;
    }

    private void DrawUi()
    {
        _windowSystem.Draw();
    }

    private void DrawConfigUi()
    {
        var pluginWindow = _windowSystem.GetWindow(PluginConstants.ConfigWindowName);
        if (pluginWindow is not ConfigWindow window) return;
        window.IsOpen = true;
    }

    public void Dispose()
    {
        PluginInterface?.SavePluginConfig(_pluginServiceFactory.Create<Configuration>());
        _pluginServiceFactory.Dispose();
        CommandManager.RemoveHandler(PluginConstants.CommandSlash);
        CommandManager.RemoveHandler(PluginConstants.ShortCommandSlash);
        _windowSystem.RemoveAllWindows();
        GC.SuppressFinalize(this);
    }
}