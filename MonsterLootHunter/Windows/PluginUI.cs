using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using MonsterLootHunter.Data;
using MonsterLootHunter.Logic;
using MonsterLootHunter.Services;
using MonsterLootHunter.Utils;

namespace MonsterLootHunter.Windows;

public class PluginUi : Window, IDisposable
{
    private readonly IPluginLog _pluginLog;
    private readonly WindowService _windowService;
    private readonly ImageService _imageService;
    private readonly MaterialTableRenderer _materialTableRenderer;
    private readonly Configuration _configuration;
    private readonly ItemManagerService _itemManagerService;
    private readonly ItemFetchService _itemFetchService;
    private IDalamudTextureWrap? _selectedItemIcon;
    private Item? _selectedItem;
    private List<KeyValuePair<SearchCategories, List<Item>>> _enumerableCategoriesAndItems = [];
    private LootData? _lootData;
    private readonly float _scale;
    private Vector2 _itemTextSize;
    private readonly CancellationTokenSource _tokenSource = new();
    private bool Loading { get; set; }

    #region Props

    private string _searchString = string.Empty;

    public string SearchString
    {
        get => _searchString;
        set => _searchString = value;
    }

    private string _lastSearchString = string.Empty;

    #endregion

    public PluginUi(WindowService windowService, Configuration configuration, ImageService imageService, MaterialTableRenderer tableRenderer, ItemManagerService itemManagerService,
                    ItemFetchService itemFetchService, IPluginLog pluginLog)
        : base(WindowConstants.MainWindowName, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        _windowService = windowService;
        _configuration = configuration;
        _imageService = imageService;
        _itemFetchService = itemFetchService;
        _pluginLog = pluginLog;
        _itemManagerService = itemManagerService;
        _materialTableRenderer = tableRenderer;
        _scale = ImGui.GetIO().FontGlobalScale;
        _materialTableRenderer.SetScale(_scale);
        SizeCondition = ImGuiCond.Always;
    }

    public override void PreDraw()
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(800, 600) * _configuration.MinimumWindowScale,
            MaximumSize = new Vector2(800, 600) * _configuration.MaximumWindowScale
        };
        base.PreDraw();
    }

    public override void Draw()
    {
        _itemTextSize = ImGui.CalcTextSize(string.Empty);
        LoadCategoryItemList();

        try
        {
            ImGui.BeginChild("lootListColumn", new Vector2(250, 0) * _scale, true);
            ImGui.SetNextItemWidth(ImGui.GetIO().FontGlobalScale - ImGui.GetStyle().ItemSpacing.X);
            ImGui.InputTextWithHint("##searchString", "Search for loot", ref _searchString, 256);
            ImGui.Separator();

            #region Loot Categories

            ImGui.BeginChild("itemTree", new Vector2(0, -1.0f * ImGui.GetFrameHeightWithSpacing()), false, ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.AlwaysHorizontalScrollbar);

            foreach (var (category, items) in _enumerableCategoriesAndItems)
            {
                if (!ImGui.TreeNode($"{category.Name}##cat{category.RowId}"))
                    continue;

                ImGui.Unindent(ImGui.GetTreeNodeToLabelSpacing());

                for (var i = 0; i < items.Count; i++)
                {
                    if (ImGui.GetCursorPosY() < ImGui.GetScrollY() - _itemTextSize.Y)
                    {
                        // Don't draw items above the scroll region.
                        var y = ImGui.GetCursorPosY();
                        var sy = ImGui.GetScrollY() - _itemTextSize.Y;
                        var spacing = _itemTextSize.Y + ImGui.GetStyle().ItemSpacing.Y;
                        var c = items.Count;

                        while (i < c && y < sy)
                        {
                            y += spacing;
                            i++;
                        }

                        ImGui.SetCursorPosY(y);
                        continue;
                    }

                    if (ImGui.GetCursorPosY() > ImGui.GetScrollY() + ImGui.GetWindowHeight())
                    {
                        // Don't draw item names below the scroll region
                        var remainingItems = items.Count - i;
                        var remainingItemsHeight = _itemTextSize.Y * remainingItems;
                        var remainingGapHeight = ImGui.GetStyle().ItemSpacing.Y * (remainingItems - 1);
                        ImGui.Dummy(new Vector2(1, remainingItemsHeight + remainingGapHeight));
                        break;
                    }

                    var item = items[i];
                    var nodeFlags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;

                    if (item.RowId == _selectedItem?.RowId)
                    {
                        nodeFlags |= ImGuiTreeNodeFlags.Selected;
                    }

                    ImGui.TreeNodeEx($"{item.Name}##item{item.RowId}", nodeFlags);

                    if (!ImGui.IsItemClicked())
                        continue;

                    Loading = true;
                    Task.Run(async () => await ChangeSelectedItem(item.RowId));
                }

                ImGui.Indent(ImGui.GetTreeNodeToLabelSpacing());
                ImGui.TreePop();
            }

            ImGui.EndChild();

            #endregion

            ImGui.Text("Settings: ");
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);

            if (ImGui.Button($"{(char)FontAwesomeIcon.Cog}"))
            {
                var pluginWindow = _windowService.GetWindow(WindowConstants.ConfigWindowName);
                if (pluginWindow is not ConfigWindow window)
                    return;

                window.IsOpen = true;
            }

            ImGui.PopFont();

            ImGui.EndChild();
            ImGui.SameLine();
            ImGui.BeginChild("panelColumn", new Vector2(0, 0), false, ImGuiWindowFlags.NoScrollbar);

            if (_selectedItem?.RowId > 0)
            {
                try
                {
                    _selectedItemIcon = _imageService.GetIconTexture(_selectedItem.Value.Icon);

                    if (_selectedItemIcon != null)
                    {
                        ImGui.Image(_selectedItemIcon.Handle, new Vector2(40, 40));
                    }
                    else
                    {
                        ImGui.SetCursorPos(new Vector2(40, 40));
                    }
                }
                catch (Exception)
                {
                    ImGui.SetCursorPos(new Vector2(40, 40));
                }

                ImGui.SameLine();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImGui.GetFontSize() / 2.0f + 19 * _scale);
                ImGui.Text(_selectedItem?.Name.ToString() ?? string.Empty);

                if (Loading)
                {
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(ImGui.GetCursorPosY() - ImGui.GetFontSize() / 2.0f + 350 * _scale);
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImGui.GetFontSize() / 2.0f + 19 * _scale);
                    ImGui.Text("Loading data");
                    ImGui.SameLine();
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImGui.GetFontSize() / 2.0f + 19 * _scale);
                    ImGui.Text($"{(char)FontAwesomeIcon.Spinner}");
                    ImGui.PopFont();
                }

                ImGui.SameLine();
                var source = $"{(_configuration.PreferWikiData ? "Data" : "Some data")} provided by FFXIV Console Games Wiki (https://ffxiv.consolegameswiki.com)";
                ImGui.SetCursorPosX(ImGui.GetWindowContentRegionMax().X - ImGui.CalcTextSize(source).X);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImGui.GetFontSize() / 2.0f + 50 * _scale);
                ImGui.Text(source);

                #region Obtained From Table

                var mobList = _lootData?.LootLocations.OrderBy(m => m.MobName).ToList();
                if (_configuration.UseLegacyViewer)
                    _materialTableRenderer.RenderLegacyMobTable(mobList, _itemTextSize);
                else
                    _materialTableRenderer.RenderMobTable(mobList, _itemTextSize);

                #endregion

                ImGui.Separator();
                ImGui.Text("** - Data not present in internal game data");

                ImGui.Separator();

                #region Purchased From Table

                var vendorList = _lootData?.LootPurchaseLocations.OrderBy(v => v.Vendor).ToList();
                if (_configuration.UseLegacyViewer)
                    _materialTableRenderer.RenderLegacyVendorTable(vendorList, _itemTextSize);
                else
                    _materialTableRenderer.RenderVendorTable(vendorList, _itemTextSize);

                #endregion
            }

            if (!_configuration.PreferWikiData)
            {
                ImGui.Text("You have only internal data searching enabled. This may impact results");
                ImGui.Text("Go to settings and enable Wiki data searching for gatherables to get improved results.");
            }

            ImGui.EndChild();
        }
        catch (Exception e)
        {
            _pluginLog.Error(e.Message);
        }
    }

    private void LoadCategoryItemList()
    {
        var shouldPerformSearch = _enumerableCategoriesAndItems.Count == 0 || _searchString != _lastSearchString;
        //Prevent loading the list on every draw if it's not necessary
        if (!shouldPerformSearch)
            return;

        (_enumerableCategoriesAndItems, _lastSearchString) =
            _itemManagerService.GetEnumerableItems(_searchString, _searchString != _lastSearchString);
    }

    protected internal async Task ChangeSelectedItem(uint itemId)
    {
        try
        {
            _selectedItem = _itemManagerService.RetrieveItem(itemId);
            if (_selectedItem is null)
                return;

            _lootData = null;
            var token = _tokenSource.Token;
            token.ThrowIfCancellationRequested();
            _lootData = await _itemFetchService.FetchLootData(_selectedItem.Value, token);
        }
        catch (OperationCanceledException e)
        {
            _pluginLog.Error(e, "Request for loot $1 info failed", _selectedItem?.Name ?? string.Empty);
        }
        catch (Exception e)
        {
            _pluginLog.Error(e, "Request for loot $1 info failed", _selectedItem?.Name ?? string.Empty);
        }
        finally
        {
            Loading = false;
        }
    }

    public void Dispose()
    {
        _selectedItemIcon?.Dispose();
        _tokenSource.Dispose();
        GC.SuppressFinalize(this);
    }
}
