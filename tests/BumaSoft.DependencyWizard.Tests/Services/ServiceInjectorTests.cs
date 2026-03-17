using System.Reflection;
using BumaSoft.DependencyWizard.Services;
using BumaSoft.DependencyWizard.Tests.InvalidServices;
using BumaSoft.DependencyWizard.Tests.ValidServices;
using Microsoft.Extensions.DependencyInjection;

namespace BumaSoft.DependencyWizard.Tests.Services;

[TestFixture]
public class ServiceInjectorTests
{
    private ServiceProvider validServiceProvider;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var serviceCollection = new ServiceCollection();
        var assemblies = new[] { typeof(ScopedService).Assembly };
        serviceCollection.InjectServices(assemblies);
        validServiceProvider = serviceCollection.BuildServiceProvider();
    }

    [Test]
    public void TransientServiceTest()
    {
        var service1 = validServiceProvider.GetService<TransientService>();
        var service2 = validServiceProvider.GetService<TransientService>();

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
        var service1Scope1 = validServiceProvider.GetService<ScopedService>();
        var service2Scope1 = validServiceProvider.GetService<ScopedService>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(service1Scope1, Is.Not.Null);
            Assert.That(service2Scope1, Is.Not.Null);
        }

        if (service1Scope1 is null || service2Scope1 is null)
            return;

        Assert.That(service1Scope1.ReturnSomething(), Is.EqualTo(service2Scope1.ReturnSomething()));

        var scope = validServiceProvider.CreateScope();
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
        var service1Scope1 = validServiceProvider.GetService<SingletonService>();
        var service2Scope1 = validServiceProvider.GetService<SingletonService>();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(service1Scope1, Is.Not.Null);
            Assert.That(service2Scope1, Is.Not.Null);
        }

        if (service1Scope1 is null || service2Scope1 is null)
            return;

        Assert.That(service1Scope1.ReturnSomething(), Is.EqualTo(service2Scope1.ReturnSomething()));

        var scope = validServiceProvider.CreateScope();
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

    [Test]
    public void AbstractionServiceTest()
    {
        var service = validServiceProvider.GetService<IAbstractionService>();
        var concreteService = validServiceProvider.GetService<AbstractionService>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(service, Is.Not.Null);
            Assert.That(concreteService, Is.Null, "AbstractionService should NOT be registered as concrete.");
        }
    }

    [Test]
    public void BothServiceTest()
    {
        var serviceFromInterface = validServiceProvider.GetService<IBothService>();
        var serviceFromConcrete = validServiceProvider.GetService<BothService>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(serviceFromInterface, Is.Not.Null);
            Assert.That(serviceFromConcrete, Is.Not.Null);
            Assert.That(serviceFromInterface, Is.SameAs(serviceFromConcrete), "Both registrations should point to the same instance in the same scope.");
        }
    }

    [Test]
    public void Validation_Errors_Are_Reported()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().InjectServices([typeof(InvalidAbstractionService).Assembly]));

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Does.Contain("cannot be injected as abstraction because it does not have a specified injection type"));
            Assert.That(exception.Message, Does.Contain("cannot be injected as concrete type because it has a specified injection type"));
        });
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => validServiceProvider.Dispose();
}
