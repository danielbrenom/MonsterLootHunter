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
        private PluginUI PluginUi { get; init; }

        public Plugin()
        {
            PluginServices.Initialize(PluginInterface);
            PluginUi = new PluginUI();
            CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
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
                {
                    PluginUi.ChangeSelectedItem(itemId);
                    PluginUi.Visible = true;
                }
                else
                {
                    PluginUi.SearchString = args;
                    PluginUi.Visible = true;
                }
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
            CommandManager.RemoveHandler(commandName);
            PluginInterface.UiBuilder.Draw -= DrawUi;
            GC.SuppressFinalize(this);
        }
    }
}