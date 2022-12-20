using System;
using Dalamud.Data;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using MonsterLootHunter.Logic;
using MonsterLootHunter.Windows;

namespace MonsterLootHunter
{
    public class Plugin : IDalamudPlugin
    {
        public string Name => "Monster Loot Hunter";
        private const string CommandName = "/monsterloot";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        private DataManager DataManager { get; init; }
        private GameGui GameGui { get; init; }
        public WindowSystem WindowSystem = new("MonsterLootHunter");

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager,
            [RequiredVersion("1.0")] DataManager dataManager,
            [RequiredVersion("1.0")] GameGui gameGui)
        {
            PluginInterface = pluginInterface;
            CommandManager = commandManager;
            DataManager = dataManager;
            GameGui = gameGui;
            PluginServices.Initialize(PluginInterface)
                          .RegisterService<ItemManager>()
                          .RegisterService<MapManager>();
            
            WindowSystem.AddWindow(new PluginUI(this));
            
            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens loot drop window."
            });
            PluginInterface.UiBuilder.Draw += DrawUi;
        }


        private void OnCommand(string command, string args)
        {
            if (!string.IsNullOrEmpty(args))
            {
                if (uint.TryParse(args, out var itemId))
                    PluginUi.ChangeSelectedItem(itemId);
                else
                    PluginUi.SearchString = args;

                PluginUi.Visible = true;
            }
            else
                PluginUi.Visible = !PluginUi.Visible;
        }

        private void DrawUi()
        {
            if (PluginUi is { Visible: true })
                PluginUi.Visible = PluginUi.Draw();
        }

        private void DrawConfigUi()
        {
            PluginUi.SettingsVisible = true;
        }

        public void Dispose()
        {
            PluginInterface.SavePluginConfig(PluginServices.Configuration);
            PluginUi.Dispose();
            PluginServices.Dispose();
            CommandManager.RemoveHandler(CommandName);
            PluginInterface.UiBuilder.Draw -= DrawUi;
            GC.SuppressFinalize(this);
        }
    }
}