using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using MonsterLootHunter.Logic;
using MonsterLootHunter.Utils;

namespace MonsterLootHunter.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration _configuration;
    private readonly float _scale;
    private float _minScale;
    private float _maxScale;
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
        CheckConfiguration();
    }

    private void CheckConfiguration()
    {
        _contextIntegration = _configuration.ContextMenuIntegration;
        _legacyViewer = _configuration.UseLegacyViewer;
        _minScale = Math.Min(_configuration.MinimumWindowScale, 1f);
        _maxScale = Math.Min(_configuration.MaximumWindowScale, 2f);
    }

    public override void Draw()
    {
        ImGui.BeginChild("configurations", new Vector2(0, -1f) * _scale);
        if (ImGui.Checkbox("Context menu integration", ref _contextIntegration))
            _configuration.ContextMenuIntegration = _contextIntegration;
        if(ImGui.Checkbox("Use legacy viewer", ref _legacyViewer))
            _configuration.UseLegacyViewer = _legacyViewer;

        ImGui.Text("Window scale values");
        ImGui.Text("Minimum size scale");
        ImGui.SameLine();
        ImGui.DragFloat("##min_size", ref _minScale, 0.5f, 0.5f, 1f, "%.1f", ImGuiSliderFlags.None);
        ImGui.Text("Maximum size scale");
        ImGui.SameLine();
        ImGui.DragFloat("##max_size", ref _maxScale, 0.5f, 1.5f, 2f, "%.1f", ImGuiSliderFlags.None);
        ImGui.TextColored(new Vector4(153f, 56f, 56f, 1.0f), "Reload plugin for scales to take effect");

        if (ImGui.Button("Save and close"))
            IsOpen = false;

        ImGui.EndChild();
    }

    public override void OnClose()
    {
        _configuration.MinimumWindowScale = _minScale;
        _configuration.MaximumWindowScale = _maxScale;
        _configuration.Save();
        base.OnClose();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}