using System.Reflection;
using BumaSoft.DependencyWizard.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace BumaSoft.DependencyWizard.SemanticKernel.Plugins;

public static class PluginInjector
{
    public static IKernelBuilder InjectPlugins(
        this IKernelBuilder kernelBuilder,
        IServiceCollection services,
        Assembly[] assemblies
    )
    {
        var pluginTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.GetCustomAttribute<PluginAttribute>() is not null)
            .ToList();

        var injectPluginMethod = typeof(PluginInjector)
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .FirstOrDefault(method => method.IsGenericMethodDefinition && method.Name.Contains(nameof(InjectPlugin)))!;

        foreach (var pluginType in pluginTypes)
        {
            var pluginAttribute = pluginType.GetCustomAttribute<PluginAttribute>();
            var scope = pluginAttribute!.Scope;
            var injectPluginGenericMethod = injectPluginMethod.MakeGenericMethod(pluginType);
            kernelBuilder = (IKernelBuilder)injectPluginGenericMethod!.Invoke(null, [scope, kernelBuilder, services])!;
        }

        return kernelBuilder;
    }

    private static IKernelBuilder InjectPlugin<TPlugin>(ServiceScope scope, IKernelBuilder kernelBuilder, IServiceCollection services) where TPlugin : class
    {
        kernelBuilder.Plugins.AddFromType<TPlugin>();
        switch (scope)
        {
            case ServiceScope.Singleton:
                services.AddSingleton<TPlugin>();
                break;
            case ServiceScope.Scoped:
                services.AddScoped<TPlugin>();
                break;
            case ServiceScope.Transient:
                services.AddTransient<TPlugin>();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scope), scope, null);
        }
        return kernelBuilder;
    }
}

