using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using MonsterLootHunter.Logic;
using MonsterLootHunter.Utils;

namespace MonsterLootHunter.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration _configuration;
    private readonly float _scale;
    private bool _contextIntegration;
    private bool _legacyViewer;

    public ConfigWindow(Configuration configuration) : base(WindowConstants.ConfigWindowName, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoResize)
    {
        _configuration = configuration;
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
        _contextIntegration = _configuration.ContextMenuIntegration;
        _legacyViewer = _configuration.UseLegacyViewer;
        ImGui.BeginChild("configurations", new Vector2(250, -1f) * _scale);
        if (ImGui.Checkbox("Context menu integration", ref _contextIntegration))
            _configuration.ContextMenuIntegration = _contextIntegration;
        if(ImGui.Checkbox("Use legacy viewer", ref _legacyViewer))
            _configuration.UseLegacyViewer = _legacyViewer;

        if (ImGui.Button("Save and close"))
            IsOpen = false;

        ImGui.EndChild();
    }

    public override void OnClose()
    {
        _configuration.Save();
        base.OnClose();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}