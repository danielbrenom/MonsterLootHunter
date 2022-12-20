using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud;
using Dalamud.Game.Gui;
using Dalamud.Plugin;
using MonsterLootHunter.Logic;

namespace MonsterLootHunter;

public class PluginServices
{
    private Dictionary<Type, IServiceType> Services;
    private static PluginServices pluginServices;

    public static PluginServices Instance
    {
        get
        {
            if (pluginServices != null) return pluginServices;
            pluginServices = new PluginServices();
            return pluginServices;
        }
    }

    public static DalamudPluginInterface PluginInterface { get; private set; }
    public GameGui GameGui { get; private set; }
    public static ItemManager ItemManager { get; private set; }
    public static Configuration Configuration { get; private set; }
    public static MapManager MapManager { get; private set; }

    private PluginServices()
    {
        Services = new Dictionary<Type, IServiceType>();
    }

    public static PluginServices Initialize(DalamudPluginInterface pluginInterface)
    {
        PluginInterface = pluginInterface;
        ItemManager = new ItemManager();
        Configuration = (Configuration)PluginInterface.GetPluginConfig() ?? new Configuration();
        Configuration.Initialize(pluginInterface);
        MapManager = new MapManager();
        return Instance;
    }

    public PluginServices RegisterService<T>() where T : class, IServiceType, new()
    {
        Services.Add(typeof(T), new T());
        return this;
    }

    public PluginServices RegisterService<T>(T instance) where T : class, IServiceType
    {
        Services.Add(typeof(T), instance);
        return this;
    }

    public T GetService<T>() where T : class, IServiceType
    {
        return Services[typeof(T)] as T;
    }

    public static void Dispose()
    {
        ItemManager = null;
        Configuration = null;
        MapManager = null;
    }
}