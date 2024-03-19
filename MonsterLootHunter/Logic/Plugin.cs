using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using MonsterLootHunter.Services;
using MonsterLootHunter.Utils;
using MonsterLootHunter.Windows;

namespace MonsterLootHunter.Logic;

public class Plugin : IDalamudPlugin
{
    public string Name => PluginConstants.CommandName;

    private DalamudPluginInterface PluginInterface { get; init; }
    private ICommandManager CommandManager { get; init; }
    private readonly PluginServiceFactory _pluginServiceFactory;
    private readonly WindowService _windowService;

    public Plugin(DalamudPluginInterface pluginInterface, ICommandManager commandManager, IDataManager dataManager,
        IGameGui gameGui, IClientState clientState, ITextureProvider textureProvider, IContextMenu contextMenu, IPluginLog pluginLog)
    {
        PluginInterface = pluginInterface;
        CommandManager = commandManager;
        var configuration = (Configuration?)PluginInterface.GetPluginConfig() ?? new Configuration();
        configuration.Initialize(pluginInterface, clientState.ClientLanguage);
        _windowService = new WindowService(new WindowSystem(WindowConstants.WindowSystemNamespace));
        _pluginServiceFactory = new PluginServiceFactory().RegisterService(pluginInterface)
                                                          .RegisterService(_windowService)
                                                          .RegisterService(configuration)
                                                          .RegisterService(dataManager)
                                                          .RegisterService(gameGui)
                                                          .RegisterService(textureProvider)
                                                          .RegisterService(contextMenu)
                                                          .RegisterService(pluginLog);
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
        PluginInterface.UiBuilder.OpenMainUi += _windowService.GetWindow(WindowConstants.MainWindowName).Toggle;
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
        PluginInterface.SavePluginConfig(_pluginServiceFactory.Create<Configuration>());
        _pluginServiceFactory.Dispose();
        CommandManager.RemoveHandler(PluginConstants.CommandSlash);
        CommandManager.RemoveHandler(PluginConstants.ShortCommandSlash);
        _windowService.Unregister();
        GC.SuppressFinalize(this);
    }
}