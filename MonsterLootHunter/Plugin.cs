using System;
using Dalamud.Data;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Plugin;

namespace MonsterLootHunter
{
    public class Plugin : IDalamudPlugin
    {
        public string Name => "Monster Loot Hunter";
        private const string commandName = "/monsterloot";

        [PluginService] internal static DalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] internal static CommandManager CommandManager { get; private set; } = null!;
        [PluginService] internal static DataManager DataManager { get; private set; } = null!;
        [PluginService] internal static GameGui GameGui { get; private set; } = null!;
        [PluginService] internal static ChatGui ChatGui { get; private set; } = null!;
        private Configuration Configuration { get; init; }
        private PluginUI PluginUi { get; init; }

        public Plugin()
        {
            Configuration = (Configuration)PluginInterface.GetPluginConfig() ?? new Configuration();
            Configuration.Initialize(PluginInterface);
            PluginUi = new PluginUI(Configuration);
            CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Texto de ajuda para mostrar a caça"
            });
            PluginInterface.UiBuilder.Draw += DrawUi;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUi;
        }

        public void Dispose()
        {
            PluginUi.Dispose();
            CommandManager.RemoveHandler(commandName);
            PluginInterface.UiBuilder.Draw -= DrawUi;
            PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUi;
            GC.SuppressFinalize(this);
        }

        private void OnCommand(string command, string args)
        {
            PluginUi.Visible = true;
        }

        private void DrawUi()
        {
            PluginUi.Draw();
        }

        private void DrawConfigUi()
        {
            PluginUi.SettingsVisible = true;
        }
    }
}