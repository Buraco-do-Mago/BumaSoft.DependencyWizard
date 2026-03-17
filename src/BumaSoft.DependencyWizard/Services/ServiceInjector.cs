using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace BumaSoft.DependencyWizard.Services;

public static class ServiceInjector
{
    public static IServiceCollection InjectServices(
        this IServiceCollection services, Assembly[] assemblies
    )
    {
        var serviceTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.GetCustomAttribute<ServiceAttribute>() is not null)
            .ToList();

        var injectServiceMethod = typeof(ServiceInjector)
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .FirstOrDefault(method => method.IsGenericMethodDefinition && method.Name.Contains(nameof(InjectService)))!;

        var injectServiceAsAbstractionMethod = typeof(ServiceInjector)
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .FirstOrDefault(method => method.IsGenericMethodDefinition && method.Name.Contains(nameof(InjectServiceAsAbstraction)))!;

        var errors = new List<string>();

        foreach (var serviceType in serviceTypes)
        {
            var serviceAttribute = serviceType.GetCustomAttribute<ServiceAttribute>();
            var scope = serviceAttribute!.Scope;

            if (serviceAttribute.InjectionType is not null && serviceAttribute.InjectionMode is InjectionMode.Concrete)
                errors.Add($"Service {serviceType.FullName} cannot be injected as concrete type because it has a specified injection type.");

            if (serviceAttribute.InjectionType is null && serviceAttribute.InjectionMode is InjectionMode.Abstraction or InjectionMode.Both)
                errors.Add($"Service {serviceType.FullName} cannot be injected as abstraction because it does not have a specified injection type.");

            if (serviceAttribute.InjectionMode is InjectionMode.Concrete or InjectionMode.Both)
            {
                var injectServiceGenericMethod = injectServiceMethod.MakeGenericMethod(serviceType);
                services = (IServiceCollection)injectServiceGenericMethod!.Invoke(null, [scope, services])!;
            }

            if (serviceAttribute.InjectionMode is InjectionMode.Abstraction or InjectionMode.Both)
            {
                var injectServiceAsAbstractionGenericMethod = injectServiceAsAbstractionMethod.MakeGenericMethod(serviceAttribute.InjectionType!, serviceType);
                services = (IServiceCollection)injectServiceAsAbstractionGenericMethod!.Invoke(null, [scope, services])!;
            }
        }

        if (errors.Count != 0)
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors));

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

    private static IServiceCollection InjectServiceAsAbstraction<TInjectedService, TService>(ServiceScope scope, IServiceCollection services)
        where TInjectedService : class
        where TService : class, TInjectedService
    {
        switch (scope)
        {
            case ServiceScope.Singleton:
                services.AddSingleton<TInjectedService, TService>();
                break;
            case ServiceScope.Scoped:
                services.AddScoped<TInjectedService, TService>();
                break;
            case ServiceScope.Transient:
                services.AddTransient<TInjectedService, TService>();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scope), scope, null);
        }
        return services;
    }
}
