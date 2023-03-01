using System.Collections.Generic;
using Dalamud.Interface.Windowing;

namespace MonsterLootHunter.Services;

public class WindowService
{
    private readonly WindowSystem _windowSystem;
    private Dictionary<string, Window> RegisteredWindows { get; set; }

    public WindowService(WindowSystem windowSystem)
    {
        _windowSystem = windowSystem;
        RegisteredWindows = new Dictionary<string, Window>();
    }

    public void RegisterWindow(Window window, string windowName)
    {
        _windowSystem.AddWindow(window);
        RegisteredWindows.TryAdd(windowName, window);
    }

    public Window GetWindow(string windowName)
    {
        if (RegisteredWindows.TryGetValue(windowName, out var window))
            return window;
        throw new KeyNotFoundException("Window not registered");
    }

    public void Draw()
    {
        _windowSystem.Draw();
    }

    public void Unregister()
    {
        RegisteredWindows = null;
        _windowSystem.RemoveAllWindows();
    }
}