using System.Reflection;
using BumaSoft.DependencyWizard.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BumaSoft.DependencyWizard.Tests.Services;

[TestFixture]
public class ServiceInjectorTests
{
    private ServiceProvider serviceProvider;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var serviceCollection = new ServiceCollection();
        var assemblies = new[] { Assembly.GetExecutingAssembly() };
        serviceCollection.InjectServices(assemblies);
        serviceProvider = serviceCollection.BuildServiceProvider();
    }

    [Test]
    public void TransientServiceTest()
    {
        var service1 = serviceProvider.GetService<TransientService>();
        var service2 = serviceProvider.GetService<TransientService>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(service1, Is.Not.Null);
            Assert.That(service2, Is.Not.Null);
        }

        if (service1 is null || service2 is null)
            return;

        Assert.That(service1.ReturnSomething(), Is.Not.EqualTo(service2.ReturnSomething()));
    }

    [Test]
    public void ScopedServiceTest()
    {
        var service1Scope1 = serviceProvider.GetService<ScopedService>();
        var service2Scope1 = serviceProvider.GetService<ScopedService>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(service1Scope1, Is.Not.Null);
            Assert.That(service2Scope1, Is.Not.Null);
        }

        if (service1Scope1 is null || service2Scope1 is null)
            return;

        Assert.That(service1Scope1.ReturnSomething(), Is.EqualTo(service2Scope1.ReturnSomething()));

        var scope = serviceProvider.CreateScope();
        var service1Scope2 = scope.ServiceProvider.GetService<ScopedService>();
        var service2Scope2 = scope.ServiceProvider.GetService<ScopedService>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(service1Scope2, Is.Not.Null);
            Assert.That(service2Scope2, Is.Not.Null);
        }

        if (service1Scope2 is null || service2Scope2 is null)
            return;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(service1Scope2.ReturnSomething(), Is.EqualTo(service2Scope2.ReturnSomething()));
            Assert.That(service1Scope1.ReturnSomething(), Is.Not.EqualTo(service1Scope2.ReturnSomething()));
        }

    }

    [Test]
    public void SingletonServiceTest()
    {
        var service1Scope1 = serviceProvider.GetService<SingletonService>();
        var service2Scope1 = serviceProvider.GetService<SingletonService>();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(service1Scope1, Is.Not.Null);
            Assert.That(service2Scope1, Is.Not.Null);
        }

        if (service1Scope1 is null || service2Scope1 is null)
            return;

        Assert.That(service1Scope1.ReturnSomething(), Is.EqualTo(service2Scope1.ReturnSomething()));

        var scope = serviceProvider.CreateScope();
        var service1Scope2 = scope.ServiceProvider.GetService<SingletonService>();
        var service2Scope2 = scope.ServiceProvider.GetService<SingletonService>();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(service1Scope2, Is.Not.Null);
            Assert.That(service2Scope2, Is.Not.Null);
        }

        if (service1Scope2 is null || service2Scope2 is null)
            return;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(service1Scope2.ReturnSomething(), Is.EqualTo(service2Scope2.ReturnSomething()));
            Assert.That(service1Scope1.ReturnSomething(), Is.EqualTo(service1Scope2.ReturnSomething()));
        }
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => serviceProvider.Dispose();
}

public class BaseService
{
    private Guid Something { get; set; } = Guid.NewGuid();

    public Guid ReturnSomething() => Something;
}

[Service(ServiceScope.Transient)]
public class TransientService : BaseService;

[Service(ServiceScope.Scoped)]
public class ScopedService : BaseService;

[Service(ServiceScope.Singleton)]
public class SingletonService : BaseService;
