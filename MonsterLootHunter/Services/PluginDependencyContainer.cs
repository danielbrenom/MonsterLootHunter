namespace MonsterLootHunter.Services;

public class PluginDependencyContainer
{
    private readonly List<Type> _registeredTypes = [];
    private readonly Dictionary<Type, object> _services = new();

    public PluginDependencyContainer Register<T>() where T : class
    {
        if (!_registeredTypes.Contains(typeof(T)))
            _registeredTypes.Add(typeof(T));
        return this;
    }

    public PluginDependencyContainer Register<T>(T instance) where T : class
    {
        _registeredTypes.Add(typeof(T));
        _services.TryAdd(typeof(T), instance);
        return this;
    }

    public void Resolve()
    {
        foreach (var registeredType in _registeredTypes)
        {
            _services.TryAdd(registeredType, CreateDependency(registeredType));
        }
    }

    public T Retrieve<T>() where T : class
    {
        if (_services.TryGetValue(typeof(T), out var service))
            return (T)service;

        return (T)CreateDependency(typeof(T));
    }

    private object CreateDependency(Type type)
    {
        if (_services.TryGetValue(type, out var service))
            return service;

        var defaultConstructor = type.GetConstructors()[0];
        var defaultParams = defaultConstructor.GetParameters();
        var parameters = defaultParams.Select(param => CreateDependency(param.ParameterType)).ToArray();
        return defaultConstructor.Invoke(parameters);
    }

    public void Dispose()
    {
        foreach (var (type, service) in _services)
        {
            try
            {
                var disposeMethod = type.GetMethod("Dispose");
                if (disposeMethod != null && type.GetInterfaceMap(typeof(IDisposable)).TargetMethods.Contains(disposeMethod))
                    ((IDisposable)service).Dispose();
            }
            catch (ArgumentException)
            {
                //Non-disposable services should not interfere
            }
        }
    }
}
