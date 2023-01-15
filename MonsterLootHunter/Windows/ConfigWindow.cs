
using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using MonsterLootHunter.Logic;
using MonsterLootHunter.Services;
using MonsterLootHunter.Utils;

namespace MonsterLootHunter.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly float _scale;
    private bool _contextIntegration;
    public ConfigWindow() : base(PluginConstants.ConfigWindowName, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        _scale = ImGui.GetIO().FontGlobalScale;
        Size = new Vector2(250, 150) * _scale;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(250, 150) * _scale,
            MaximumSize = new Vector2(250, 150) * _scale * 1.5f
        };
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        _contextIntegration = PluginServices.Instance.Configuration.ContextMenuIntegration;
        ImGui.BeginChild("configurations", new Vector2(250, -1f) * _scale);
        if (ImGui.Checkbox("Context menu integration", ref _contextIntegration)) 
            PluginServices.Instance.Configuration.ContextMenuIntegration = _contextIntegration;

        if (ImGui.Button("Save and close"))
            IsOpen = false;
        
        ImGui.EndChild();
    }

    public override void OnClose()
    {
        PluginServices.Instance.Configuration.Save();
        base.OnClose();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}