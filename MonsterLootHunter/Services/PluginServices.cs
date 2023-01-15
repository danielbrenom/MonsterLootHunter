using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud;
using Dalamud.Interface.Windowing;
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
    public WindowSystem WindowSystem { get; private set; }
    
    private PluginServices()
    {
        _services = new Dictionary<Type, IServiceType>();
    }

    public static PluginServices Initialize(DalamudPluginInterface pluginInterface, WindowSystem windowSystem)
    {
        _pluginServices = new PluginServices();
        Instance.WindowSystem = windowSystem;
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
        foreach (var (type, service) in Instance._services)
        {
            var disposeMethod = type.GetMethod("Dispose");
            if (disposeMethod != null && type.GetInterfaceMap(typeof(IDisposable)).TargetMethods.Contains(disposeMethod)) 
                ((IDisposable)service).Dispose();
        }

        Instance = null;
    }
}