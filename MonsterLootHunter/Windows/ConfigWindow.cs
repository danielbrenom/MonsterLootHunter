using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using MonsterLootHunter.Logic;
using MonsterLootHunter.Services;
using MonsterLootHunter.Utils;

namespace MonsterLootHunter.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration _configuration;
    private readonly ItemFetchService _itemFetchService;
    private readonly float _scale;
    private float _minScale;
    private float _maxScale;
    private bool _contextIntegration;
    private bool _legacyViewer;
    private bool _preferWikiData;
    private bool _appendData;

    public ConfigWindow(Configuration configuration, ItemFetchService itemFetchService)
        : base(WindowConstants.ConfigWindowName, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoResize)
    {
        _configuration = configuration;
        _itemFetchService = itemFetchService;
        _scale = ImGui.GetIO().FontGlobalScale;
        CheckConfiguration();
    }

    public override void PreDraw()
    {
        Size = new Vector2(450, 150) * _scale;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(400, 300),
            MaximumSize = new Vector2(400, 300) * 1.5f
        };
        SizeCondition = ImGuiCond.FirstUseEver;
        base.PreDraw();
    }

    private void CheckConfiguration()
    {
        _contextIntegration = _configuration.ContextMenuIntegration;
        _legacyViewer = _configuration.UseLegacyViewer;
        _preferWikiData = _configuration.PreferWikiData;
        _appendData = _configuration.AppendInternalData;
        _minScale = Math.Min(_configuration.MinimumWindowScale, 1f);
        _maxScale = Math.Min(_configuration.MaximumWindowScale, 2f);
    }

    public override void Draw()
    {
        ImGui.BeginChild("configurations", new Vector2(0, -1f) * _scale);
        if (ImGui.Checkbox("Context menu integration", ref _contextIntegration))
            _configuration.ContextMenuIntegration = _contextIntegration;
        if (ImGui.Checkbox("Use legacy viewer", ref _legacyViewer))
            _configuration.UseLegacyViewer = _legacyViewer;
        if (ImGui.Checkbox("Use wiki data for gatherables", ref _preferWikiData))
            _configuration.PreferWikiData = _preferWikiData;

        if (_preferWikiData)
        {
            ImGui.SetCursorPosX(25 * _scale);
            if (ImGui.Checkbox("Include internal gatherable data", ref _appendData))
                _configuration.AppendInternalData = _appendData;
            ImGui.SetCursorPosX(25 * _scale);
            ImGui.TextColored(new Vector4(234f, 217f, 28f, 255f).NormalizeToUnitRange(), "Select item again for change to take effect.");
            ImGui.SetCursorPosX(25 * _scale);
            ImGui.TextColored(new Vector4(234f, 217f, 28f, 255f).NormalizeToUnitRange(), "New data may provide map marker.");
        }

        ImGui.Text("Window scale values");
        ImGui.Text("Minimum size scale");
        ImGui.SameLine();
        ImGui.DragFloat("##min_size", ref _minScale, 0.5f, 0.5f, 1f, "%.1f", ImGuiSliderFlags.None);
        ImGui.Text("Maximum size scale");
        ImGui.SameLine();
        ImGui.DragFloat("##max_size", ref _maxScale, 0.5f, 1.5f, 2f, "%.1f", ImGuiSliderFlags.None);

        if (ImGui.Button("Clear cached loot data"))
            _itemFetchService.ClearStoredLootData();
        ImGui.TextColored(new Vector4(234f, 217f, 28f, 255f).NormalizeToUnitRange(), "Keep in mind that clearing the cached data will make data");
        ImGui.TextColored(new Vector4(234f, 217f, 28f, 255f).NormalizeToUnitRange(), "all loot be fetched again from the wiki. Use this preferably");
        ImGui.TextColored(new Vector4(234f, 217f, 28f, 255f).NormalizeToUnitRange(), "when the plugin has been updated and data is not loading.");

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
