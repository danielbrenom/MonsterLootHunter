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

    private IDalamudPluginInterface PluginInterface { get; init; }
    private ICommandManager CommandManager { get; init; }
    private readonly PluginDependencyContainer _pluginDependencyContainer;
    private readonly WindowService _windowService;

    public Plugin(IDalamudPluginInterface pluginInterface, ICommandManager commandManager, IDataManager dataManager,
                  IGameGui gameGui, IClientState clientState, ITextureProvider textureProvider, IContextMenu contextMenu, IPluginLog pluginLog)
    {
        PluginInterface = pluginInterface;
        CommandManager = commandManager;
        var configuration = (Configuration?)PluginInterface.GetPluginConfig() ?? new Configuration();
        configuration.Initialize(pluginInterface, clientState.ClientLanguage);
        _windowService = new WindowService(new WindowSystem(WindowConstants.WindowSystemNamespace));
        _pluginDependencyContainer = new PluginDependencyContainer().Register(pluginInterface)
                                                                    .Register(_windowService)
                                                                    .Register(configuration)
                                                                    .Register(dataManager)
                                                                    .Register(gameGui)
                                                                    .Register(textureProvider)
                                                                    .Register(contextMenu)
                                                                    .Register(pluginLog)
                                                                    .Register<ConfigWindow>()
                                                                    .Register<PluginUi>();
        PluginModule.Register(_pluginDependencyContainer);
        _pluginDependencyContainer.Resolve();

        _windowService.RegisterWindow(_pluginDependencyContainer.Retrieve<ConfigWindow>(), WindowConstants.ConfigWindowName);
        _windowService.RegisterWindow(_pluginDependencyContainer.Retrieve<PluginUi>(), WindowConstants.MainWindowName);
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
        if (pluginWindow is not PluginUi window)
            return;

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
        if (pluginWindow is not ConfigWindow window)
            return;

        window.IsOpen = true;
    }

    public void Dispose()
    {
        PluginInterface.SavePluginConfig(_pluginDependencyContainer.Retrieve<Configuration>());
        _pluginDependencyContainer.Dispose();
        CommandManager.RemoveHandler(PluginConstants.CommandSlash);
        CommandManager.RemoveHandler(PluginConstants.ShortCommandSlash);
        _windowService.Unregister();
        GC.SuppressFinalize(this);
    }
}
