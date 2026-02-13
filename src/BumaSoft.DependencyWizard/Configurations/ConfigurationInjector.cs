using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BumaSoft.DependencyWizard.Configurations;

public static class ConfigurationInjector
{
    public static IServiceCollection InjectConfigurations(
        this IServiceCollection services, IConfiguration configuration, Assembly[] assemblies
    )
    {
        var configurationTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.GetCustomAttribute<ConfigurationAttribute>() is not null)
            .ToList();

        var injectConfigurationMethod = typeof(ConfigurationInjector)
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .FirstOrDefault(method => method.IsGenericMethodDefinition && method.Name.Contains(nameof(InjectConfiguration)))!;

        foreach (var configurationType in configurationTypes)
        {
            var genericMethod = injectConfigurationMethod!.MakeGenericMethod(configurationType);
            services = (IServiceCollection)genericMethod!.Invoke(null, [services, configuration])!;
        }

        return services;
    }

    private static IServiceCollection InjectConfiguration<TConfiguration>(
        IServiceCollection services, IConfiguration configuration
    ) where TConfiguration : class, new()
    {
        var configurationName = GetConfigurationName<TConfiguration>();
        services.Configure<TConfiguration>(options => configuration.GetRequiredSection(configurationName).Bind(options));
        services.AddScoped(serviceProvider => serviceProvider.GetRequiredService<IOptionsSnapshot<TConfiguration>>().Value);
        return services;
    }

    public static string GetConfigurationName<TConfiguration>() where TConfiguration : class =>
        typeof(TConfiguration).GetConfigurationName();

    public static string GetConfigurationName(this Type type) =>
        type.GetCustomAttribute<ConfigurationAttribute>()?.Section ?? type.Name;
}
