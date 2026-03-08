using System.Reflection;
using LogAggregator.Core.Domain;

namespace LogAggregator.Core.Infrastructure.Services
{
    public class PluginLoader
    {
        public IEnumerable<ILogParser> LoadFromDirectory(string pluginPath)
        {
            if (!Directory.Exists(pluginPath))
                yield break;

            foreach (var dll in Directory.GetFiles(pluginPath, "*.dll"))
            {
                Assembly assembly;
                try { assembly = Assembly.LoadFrom(dll); }
                catch (Exception ex)
                {
                    // bad or incompatible DLL, skip it, don't crash startup
                    System.Diagnostics.Debug.WriteLine($"[PluginLoader] Could not load '{dll}': {ex.Message}");
                    continue;
                }

                IEnumerable<Type> parserTypes;
                try
                {
                    // find any concrete class that implements ILogParser
                    parserTypes = assembly.GetTypes()
                        .Where(t => typeof(ILogParser).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
                }
                catch (ReflectionTypeLoadException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PluginLoader] Type load error in '{dll}': {ex.Message}");
                    continue;
                }

                foreach (var type in parserTypes)
                {
                    ILogParser? instance = null;
                    try { instance = Activator.CreateInstance(type) as ILogParser; }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[PluginLoader] Cannot instantiate '{type.Name}': {ex.Message}");
                    }

                    if (instance != null)
                        yield return instance;
                }
            }
        }
    }
}
