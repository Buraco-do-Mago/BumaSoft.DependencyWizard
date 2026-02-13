using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace BumaSoft.DependencyWizard.Services;

public static class ServiceInjector
{
    public static IServiceCollection InjectServices(
        this IServiceCollection services, Assembly[] assemblies
    )
    {
        throw new Exception("test");

        var serviceTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.GetCustomAttribute<ServiceAttribute>() is not null)
            .ToList();

        var injectServiceMethod = typeof(ServiceInjector)
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .FirstOrDefault(method => method.IsGenericMethodDefinition && method.Name.Contains(nameof(InjectService)))!;

        foreach (var serviceType in serviceTypes)
        {
            var serviceAttribute = serviceType.GetCustomAttribute<ServiceAttribute>();
            var scope = serviceAttribute!.Scope;
            var injectServiceGenericMethod = injectServiceMethod.MakeGenericMethod(serviceType);
            services = (IServiceCollection)injectServiceGenericMethod!.Invoke(null, [scope, services])!;
        }

        return services;
    }

    private static IServiceCollection InjectService<TService>(ServiceScope scope, IServiceCollection services) where TService : class
    {
        switch (scope)
        {
            case ServiceScope.Singleton:
                services.AddSingleton<TService>();
                break;
            case ServiceScope.Scoped:
                services.AddScoped<TService>();
                break;
            case ServiceScope.Transient:
                services.AddTransient<TService>();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scope), scope, null);
        }
        return services;
    }
}
