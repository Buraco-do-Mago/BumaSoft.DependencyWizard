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

        var errors = new List<string>();

        foreach (var serviceType in serviceTypes)
        {
            var serviceAttribute = serviceType.GetCustomAttribute<ServiceAttribute>()!;
            var scope = serviceAttribute.Scope;
            var injectionMode = serviceAttribute.InjectionMode;

            if (serviceAttribute.InjectionType is not null && injectionMode is InjectionMode.Concrete)
                errors.Add($"Service {serviceType.FullName} cannot be injected as concrete type because it has a specified injection type.");

            if (serviceAttribute.InjectionType is null && injectionMode is InjectionMode.Abstraction or InjectionMode.Both)
                errors.Add($"Service {serviceType.FullName} cannot be injected as abstraction because it does not have a specified injection type.");

            if (errors.Count != 0)
                continue;

            services.InjectService(scope, injectionMode, serviceType, serviceAttribute.InjectionType);
        }

        if (errors.Count != 0)
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors));

        return services;
    }

    private static IServiceCollection InjectService(this IServiceCollection services, ServiceScope scope, InjectionMode mode, Type service, Type? abstractionType) => mode switch
    {
        InjectionMode.Concrete => services.InjectConcreteService(scope, service),
        InjectionMode.Abstraction => services.InjectAbstractionService(scope, service, abstractionType!),
        InjectionMode.Both => services.InjectBothServices(scope, service, abstractionType!),
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
    };

    private static IServiceCollection InjectConcreteService(this IServiceCollection services, ServiceScope scope, Type service)
    {
        switch (scope)
        {
            case ServiceScope.Singleton:
                services.AddSingleton(service);
                break;
            case ServiceScope.Scoped:
                services.AddScoped(service);
                break;
            case ServiceScope.Transient:
                services.AddTransient(service);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scope), scope, null);
        }

        return services;
    }

    private static IServiceCollection InjectAbstractionService(this IServiceCollection services, ServiceScope scope, Type service, Type abstractionType)
    {
        switch (scope)
        {
            case ServiceScope.Singleton:
                services.AddSingleton(abstractionType, service);
                break;
            case ServiceScope.Scoped:
                services.AddScoped(abstractionType, service);
                break;
            case ServiceScope.Transient:
                services.AddTransient(abstractionType, service);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scope), scope, null);
        }

        return services;
    }

    private static IServiceCollection InjectBothServices(this IServiceCollection services, ServiceScope scope, Type service, Type abstractionType)
    {
        switch (scope)
        {
            case ServiceScope.Singleton:
                services.AddSingleton(service);
                services.AddSingleton(abstractionType, serviceProvider => serviceProvider.GetRequiredService(service));
                break;
            case ServiceScope.Scoped:
                services.AddScoped(service);
                services.AddScoped(abstractionType, serviceProvider => serviceProvider.GetRequiredService(service));
                break;
            case ServiceScope.Transient:
                services.AddTransient(service);
                services.AddTransient(abstractionType, serviceProvider => serviceProvider.GetRequiredService(service));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scope), scope, null);
        }

        return services;
    }
}
