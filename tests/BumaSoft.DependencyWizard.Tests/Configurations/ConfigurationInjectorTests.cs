using System.Reflection;
using BumaSoft.DependencyWizard.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework.Internal;

namespace BumaSoft.DependencyWizard.Tests.Configurations;

[TestFixture]
public class ConfigurationInjectorTests
{
    private ServiceProvider ServiceProvider { get; set; }
    private IConfigurationRoot Configuration { get; set; }
    private ReloadableMemorySource ConfigurationSource { get; set; }

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var serviceCollection = new ServiceCollection();
        var assemblies = new[] { Assembly.GetExecutingAssembly() };
        var configurationData = new Dictionary<string, string?>();
        ConfigurationSource = new ReloadableMemorySource(configurationData);
        Configuration = new ConfigurationBuilder()
            .Add(ConfigurationSource)
            .Build();

        serviceCollection.InjectConfigurations(Configuration, assemblies);

        ServiceProvider = serviceCollection.BuildServiceProvider();
    }

    [SetUp]
    public void SetUp() {
        ConfigurationSource.Set(typeof(NamedConfiguration).GetConfigurationName() + ":SomeRandomProperty", "some random value");
        ConfigurationSource.Set(typeof(UnnamedConfiguration).GetConfigurationName() + ":SomeRandomProperty", "some other random value");
        Configuration.Reload();
    }

    private static IEnumerable<TestCaseData> GetConfigurationTestCases()
    {
        yield return new TestCaseData(
            typeof(NamedConfiguration),
            "some random value"
        ).SetName("Named configuration");
        yield return new TestCaseData(
            typeof(UnnamedConfiguration),
            "some other random value"
        ).SetName("Unnamed configuration");
    }

    [Test]
    [TestCaseSource(nameof(GetConfigurationTestCases))]
    public void GetConfigurationTest(Type configurationType, string expectedValue)
    {
        var configuration = ServiceProvider.GetRequiredService(configurationType) as BaseConfiguration;

        Assert.That(configuration, Is.Not.Null);
        if (configuration is null)
            return;

        Assert.That(configuration.SomeRandomProperty, Is.EqualTo(expectedValue));
    }

    private static IEnumerable<TestCaseData> BindConfigurationTestCases()
    {
        yield return new TestCaseData(
            typeof(NamedConfiguration),
            "some random value",
            "some modified random value"
        ).SetName("Named configuration");
        yield return new TestCaseData(
            typeof(UnnamedConfiguration),
            "some other random value",
            "some other modified random value"
        ).SetName("Unnamed configuration");
    }

    [Test]
    [TestCaseSource(nameof(BindConfigurationTestCases))]
    public void BindConfigurationTest(Type configurationType, string expectedValue, string newExpectedValue)
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var configuration = scope.ServiceProvider.GetRequiredService(configurationType) as BaseConfiguration;

            Assert.That(configuration, Is.Not.Null);
            if (configuration is null)
                return;

            Assert.That(configuration.SomeRandomProperty, Is.EqualTo(expectedValue));
        }

        ConfigurationSource.Set(configurationType.GetConfigurationName() + ":SomeRandomProperty", newExpectedValue);
        Configuration.Reload();

        using (var scope = ServiceProvider.CreateScope())
        {
            var configuration = scope.ServiceProvider.GetRequiredService(configurationType) as BaseConfiguration;

            Assert.That(configuration, Is.Not.Null);
            if (configuration is null)
                return;

            Assert.That(configuration.SomeRandomProperty, Is.EqualTo(newExpectedValue));
        }
    }

    [Test]
    public void UnnexistingConfigurationTest() =>
        Assert.Throws<InvalidOperationException>(() =>
            ServiceProvider.GetRequiredService<UnnexistingConfiguration>()
        );

    [OneTimeTearDown]
    public void OneTimeTearDown() => ServiceProvider.Dispose();

}

public class BaseConfiguration
{
    public string? SomeRandomProperty { get; set; }
}

[Configuration(Section = "Foo")]
public class NamedConfiguration : BaseConfiguration;

[Configuration]
public class UnnamedConfiguration : BaseConfiguration;

[Configuration]
public class UnnexistingConfiguration : BaseConfiguration;

public class ReloadableMemorySource(Dictionary<string, string?> data) : IConfigurationSource
{
    private readonly Dictionary<string, string?> _data = data;

    public void Set(string key, string? value) => _data[key] = value;
    public IConfigurationProvider Build(IConfigurationBuilder builder) => new ReloadableMemoryProvider(_data);
}

public class ReloadableMemoryProvider(Dictionary<string, string?> data) : ConfigurationProvider
{
    private readonly Dictionary<string, string?> _data = data;

    public override void Load() => Data = new Dictionary<string, string?>(_data, StringComparer.OrdinalIgnoreCase);
}
