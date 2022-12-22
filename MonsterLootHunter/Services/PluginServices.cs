using System;
using System.Collections.Generic;
using Dalamud;
using Dalamud.Plugin;
using MonsterLootHunter.Logic;

namespace MonsterLootHunter.Services;

public class PluginServices
{
    private readonly Dictionary<Type, IServiceType> _services;
    private static PluginServices _pluginServices;

    public static PluginServices Instance
    {
        get
        {
            if (_pluginServices != null) return _pluginServices;
            throw new TypeInitializationException(typeof(PluginServices).FullName, null);
        }
        private set => _pluginServices = value;
    }

    public DalamudPluginInterface PluginInterface { get; private set; }
    public Configuration Configuration { get; private set; }

    private PluginServices()
    {
        _services = new Dictionary<Type, IServiceType>();
    }

    public static PluginServices Initialize(DalamudPluginInterface pluginInterface)
    {
        _pluginServices = new PluginServices();
        Instance.PluginInterface = pluginInterface;
        Instance.Configuration = (Configuration)Instance.PluginInterface.GetPluginConfig() ?? new Configuration();
        Instance.Configuration.Initialize(pluginInterface);
        return Instance;
    }

    public PluginServices RegisterService<T>() where T : class, IServiceType, new()
    {
        _services.Add(typeof(T), new T());
        return this;
    }

    public PluginServices RegisterService<T>(T instance) where T : class, IServiceType
    {
        _services.Add(typeof(T), instance);
        return this;
    }

    public static T GetService<T>() where T : class, IServiceType
    {
        return Instance._services[typeof(T)] as T;
    }

    public static void Dispose()
    {
        Instance = null;
    }
}