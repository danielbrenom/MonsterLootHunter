using Dalamud.Interface.Windowing;

namespace MonsterLootHunter.Services;

public class WindowService(WindowSystem windowSystem)
{
    private Dictionary<string, Window> RegisteredWindows { get; set; } = new();

    public void RegisterWindow(Window window, string windowName)
    {
        windowSystem.AddWindow(window);
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
        windowSystem.Draw();
    }

    public void Unregister()
    {
        RegisteredWindows.Clear();
        windowSystem.RemoveAllWindows();
    }
}