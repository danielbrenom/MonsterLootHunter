using System;
using Dalamud.Data;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
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
    private readonly PluginServiceFactory _pluginServiceFactory;
    private readonly WindowService _windowService;

    public Plugin(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] CommandManager commandManager,
        [RequiredVersion("1.0")] DataManager dataManager,
        [RequiredVersion("1.0")] GameGui gameGui,
        [RequiredVersion("1.0")] ClientState clientState)
    {
        PluginInterface = pluginInterface;
        CommandManager = commandManager;
        var configuration = (Configuration)PluginInterface.GetPluginConfig() ?? new Configuration();
        configuration.Initialize(pluginInterface, clientState.ClientLanguage);
        _windowService = new WindowService(new(WindowConstants.WindowSystemNamespace));
        _pluginServiceFactory = new PluginServiceFactory().RegisterService(pluginInterface)
                                                          .RegisterService(_windowService)
                                                          .RegisterService(configuration)
                                                          .RegisterService(dataManager)
                                                          .RegisterService(gameGui);
        _pluginServiceFactory.RegisterService(_pluginServiceFactory);
        new PluginModule().Register(_pluginServiceFactory);


        _windowService.RegisterWindow(new ConfigWindow(configuration), WindowConstants.ConfigWindowName);
        _windowService.RegisterWindow(new PluginUi(_pluginServiceFactory), WindowConstants.MainWindowName);

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
        var pluginWindow = _windowService.GetWindow(WindowConstants.MainWindowName);
        if (pluginWindow is not PluginUi window) return;
        pluginWindow.IsOpen = true;
        window.SearchString = !args.IsNullOrEmpty() ? args : string.Empty;
    }

    private void DrawUi()
    {
        _windowService.Draw();
    }

    private void DrawConfigUi()
    {
        var pluginWindow = _windowService.GetWindow(WindowConstants.ConfigWindowName);
        if (pluginWindow is not ConfigWindow window) return;
        window.IsOpen = true;
    }

    public void Dispose()
    {
        PluginInterface?.SavePluginConfig(_pluginServiceFactory.Create<Configuration>());
        _pluginServiceFactory.Dispose();
        CommandManager.RemoveHandler(PluginConstants.CommandSlash);
        CommandManager.RemoveHandler(PluginConstants.ShortCommandSlash);
        _windowService.Unregister();
        GC.SuppressFinalize(this);
    }
}