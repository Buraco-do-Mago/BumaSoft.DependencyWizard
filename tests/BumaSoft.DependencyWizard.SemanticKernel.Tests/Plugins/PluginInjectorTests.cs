using System.Reflection;
using BumaSoft.DependencyWizard.SemanticKernel.Plugins;
using BumaSoft.DependencyWizard.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace BumaSoft.DependencyWizard.SemanticKernel.Tests.Plugins;

[TestFixture]
public class PluginInjectorTests
{
    private ServiceProvider ServiceProvider { get; set; }

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var serviceCollection = new ServiceCollection();
        var assemblies = new[] { Assembly.GetExecutingAssembly() };
        serviceCollection.AddKernel().InjectPlugins(serviceCollection, assemblies);
        ServiceProvider = serviceCollection.BuildServiceProvider();
    }

    [Test]
    public void PluginInjectorTests_KernelShouldHaveAllPlugins()
    {
        var kernel = ServiceProvider.GetRequiredService<Kernel>();
        var plugins = kernel.Plugins;
        ValidatePlugin<TransientPlugin>(plugins);
        ValidatePlugin<ScopedPlugin>(plugins);
        ValidatePlugin<SingletonPlugin>(plugins);
    }

    private static void ValidatePlugin<TPlugin>(KernelPluginCollection plugins)
    {
        var pluginName = typeof(TPlugin).Name;
        var plugin = plugins.FirstOrDefault(p => p.Name.Contains(pluginName));
        Assert.That(plugin, Is.Not.Null, $"Plugin {pluginName} was not injected into the kernel.");
        Assert.That(plugin.FunctionCount, Is.EqualTo(1), $"Plugin {pluginName} should have exactly one function.");
    }

    [Test]
    public void TransientPlugin_ShouldReturnDifferentUserIds()
    {
        var plugin1 = ServiceProvider.GetRequiredService<TransientPlugin>();
        var plugin2 = ServiceProvider.GetRequiredService<TransientPlugin>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(plugin1, Is.Not.Null);
            Assert.That(plugin2, Is.Not.Null);
        }

        var userId1 = plugin1.GetUserId();
        var userId2 = plugin2.GetUserId();

        Assert.That(userId1, Is.Not.EqualTo(userId2), "TransientPlugin should return different user IDs.");
    }

    [Test]
    public void ScopedPlugin_ShouldReturnSameUserIdWithinScope()
    {
        using var scope = ServiceProvider.CreateScope();
        var plugin1 = scope.ServiceProvider.GetRequiredService<ScopedPlugin>();
        var plugin2 = scope.ServiceProvider.GetRequiredService<ScopedPlugin>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(plugin1, Is.Not.Null);
            Assert.That(plugin2, Is.Not.Null);
        }

        var userId1 = plugin1.GetUserId();
        var userId2 = plugin2.GetUserId();

        Assert.That(userId1, Is.EqualTo(userId2), "ScopedPlugin should return the same user ID within the same scope.");
    }

    [Test]
    public void ScopedPlugin_ShouldReturnDifferentUserIdsAcrossScopes()
    {
        using var scope1 = ServiceProvider.CreateScope();
        var plugin1 = scope1.ServiceProvider.GetRequiredService<ScopedPlugin>();

        using var scope2 = ServiceProvider.CreateScope();
        var plugin2 = scope2.ServiceProvider.GetRequiredService<ScopedPlugin>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(plugin1, Is.Not.Null);
            Assert.That(plugin2, Is.Not.Null);
        }

        var userId1 = plugin1.GetUserId();
        var userId2 = plugin2.GetUserId();

        Assert.That(userId1, Is.Not.EqualTo(userId2), "ScopedPlugin should return different user IDs across different scopes.");
    }

    [Test]
    public void SingletonPlugin_ShouldReturnSameUserId()
    {
        var plugin1 = ServiceProvider.GetRequiredService<SingletonPlugin>();
        var plugin2 = ServiceProvider.GetRequiredService<SingletonPlugin>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(plugin1, Is.Not.Null);
            Assert.That(plugin2, Is.Not.Null);
        }

        var userId1 = plugin1.GetUserId();
        var userId2 = plugin2.GetUserId();

        Assert.That(userId1, Is.EqualTo(userId2), "SingletonPlugin should return the same user ID.");
    }

    [Test]
    public void SingletonPlugin_ShouldReturnSameUserIdAcrossScopes()
    {
        var plugin1 = ServiceProvider.GetRequiredService<SingletonPlugin>();
        using var scope = ServiceProvider.CreateScope();
        var plugin2 = scope.ServiceProvider.GetRequiredService<SingletonPlugin>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(plugin1, Is.Not.Null);
            Assert.That(plugin2, Is.Not.Null);
        }

        var userId1 = plugin1.GetUserId();
        var userId2 = plugin2.GetUserId();

        Assert.That(userId1, Is.EqualTo(userId2), "SingletonPlugin should return the same user ID across different scopes.");
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => ServiceProvider.Dispose();
}

public class BasePlugin
{
    public Guid UserId { get; set; } = Guid.NewGuid();

    [KernelFunction("get-user-id")]
    [Description("Returns the user's Id.")]
    public Guid GetUserId() => UserId;
}

[Plugin(ServiceScope.Transient)]
public class TransientPlugin : BasePlugin;

[Plugin(ServiceScope.Scoped)]
public class ScopedPlugin : BasePlugin;

[Plugin(ServiceScope.Singleton)]
public class SingletonPlugin : BasePlugin;
