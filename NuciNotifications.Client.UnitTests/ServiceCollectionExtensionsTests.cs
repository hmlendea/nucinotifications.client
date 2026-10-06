using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using NuciNotifications.Client;
using NuciNotifications.Client.Configuration;

namespace NuciNotifications.Client.UnitTests;

[TestFixture]
public class ServiceCollectionExtensionsTests
{
    private IServiceCollection _services = null!;
    private IConfiguration _configuration = null!;

    [SetUp]
    public void SetUp()
    {
        _services = new ServiceCollection();
        var configDict = new Dictionary<string, string?>
        {
            ["NuciNotificationsSettings:BaseUrl"] = "https://api.example.com",
            ["NuciNotificationsSettings:ApiKey"] = "test-api-key",
            ["NuciNotificationsSettings:HmacSharedSecretKey"] = "test-hmac-secret"
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();
    }

    [Test]
    public void GivenValidConfiguration_WhenAddingNuciNotificationsSettings_ThenReturnsServiceCollection()
    {
        var result = _services.AddNuciNotificationsSettings(_configuration);

        Assert.That(result, Is.SameAs(_services));
    }

    [Test]
    public void GivenValidConfiguration_WhenAddingNuciNotificationsSettings_ThenRegistersIOptions()
    {
        _services.AddNuciNotificationsSettings(_configuration);
        var provider = _services.BuildServiceProvider();

        var options = provider.GetService<IOptions<NuciNotificationsSettings>>();

        Assert.That(options, Is.Not.Null);
        Assert.That(options.Value, Is.Not.Null);
    }

    [Test]
    public void GivenValidConfiguration_WhenAddingNuciNotificationsSettings_ThenRegistersSettingsSingleton()
    {
        _services.AddNuciNotificationsSettings(_configuration);
        var provider = _services.BuildServiceProvider();

        var settings = provider.GetService<NuciNotificationsSettings>();

        Assert.That(settings, Is.Not.Null);
    }

    [Test]
    public void GivenValidConfiguration_WhenAddingNuciNotificationsSettings_ThenSettingsHaveCorrectValues()
    {
        _services.AddNuciNotificationsSettings(_configuration);
        var provider = _services.BuildServiceProvider();

        var settings = provider.GetRequiredService<NuciNotificationsSettings>();

        Assert.That(settings.BaseUrl, Is.EqualTo("https://api.example.com"));
        Assert.That(settings.ApiKey, Is.EqualTo("test-api-key"));
        Assert.That(settings.HmacSharedSecretKey, Is.EqualTo("test-hmac-secret"));
    }

    [Test]
    public void GivenValidConfiguration_WhenAddingNuciNotificationsSettings_ThenIOptionsAndSingletonAreSameInstance()
    {
        _services.AddNuciNotificationsSettings(_configuration);
        var provider = _services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<NuciNotificationsSettings>>();
        var settings = provider.GetRequiredService<NuciNotificationsSettings>();

        Assert.That(settings, Is.SameAs(options.Value));
    }

    [Test]
    public void GivenMissingConfigurationSection_WhenAddingNuciNotificationsSettings_ThenSettingsHaveNullValues()
    {
        var emptyConfig = new ConfigurationBuilder().Build();
        _services.AddNuciNotificationsSettings(emptyConfig);
        var provider = _services.BuildServiceProvider();

        var settings = provider.GetRequiredService<NuciNotificationsSettings>();

        Assert.That(settings.BaseUrl, Is.Null);
        Assert.That(settings.ApiKey, Is.Null);
        Assert.That(settings.HmacSharedSecretKey, Is.Null);
    }

    [Test]
    public void GivenPartialConfiguration_WhenAddingNuciNotificationsSettings_ThenOnlyProvidedValuesAreSet()
    {
        var partialConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NuciNotificationsSettings:BaseUrl"] = "https://partial.example.com"
            })
            .Build();

        _services.AddNuciNotificationsSettings(partialConfig);
        var provider = _services.BuildServiceProvider();

        var settings = provider.GetRequiredService<NuciNotificationsSettings>();

        Assert.That(settings.BaseUrl, Is.EqualTo("https://partial.example.com"));
        Assert.That(settings.ApiKey, Is.Null);
        Assert.That(settings.HmacSharedSecretKey, Is.Null);
    }

    [Test]
    public void GivenNullConfiguration_WhenAddingNuciNotificationsSettings_ThenThrowsNullReferenceException()
    {
        Assert.Throws<NullReferenceException>(() =>
            _services.AddNuciNotificationsSettings(null!));
    }

    [Test]
    public void GivenNullServices_WhenAddingNuciNotificationsSettings_ThenThrowsArgumentNullException()
    {
        // Extension method on null instance - framework throws ArgumentNullException
        IServiceCollection? nullServices = null;
        Assert.Throws<ArgumentNullException>(() =>
            nullServices!.AddNuciNotificationsSettings(_configuration));
    }

    [Test]
    public void GivenMultipleCalls_WhenAddingNuciNotificationsSettings_ThenRegistrationsAreIdempotent()
    {
        _services.AddNuciNotificationsSettings(_configuration);
        _services.AddNuciNotificationsSettings(_configuration);
        var provider = _services.BuildServiceProvider();

        var settings1 = provider.GetRequiredService<NuciNotificationsSettings>();
        var settings2 = provider.GetRequiredService<NuciNotificationsSettings>();

        Assert.That(settings1, Is.SameAs(settings2));
    }

    [Test]
    public void GivenConfigurationWithEnvironmentVariables_WhenAddingNuciNotificationsSettings_ThenValuesAreBound()
    {
        var envConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NuciNotificationsSettings:BaseUrl"] = "https://env.example.com",
                ["NuciNotificationsSettings:ApiKey"] = "env-api-key",
                ["NuciNotificationsSettings:HmacSharedSecretKey"] = "env-hmac-secret"
            })
            .Build();

        _services.AddNuciNotificationsSettings(envConfig);
        var provider = _services.BuildServiceProvider();

        var settings = provider.GetRequiredService<NuciNotificationsSettings>();

        Assert.That(settings.BaseUrl, Is.EqualTo("https://env.example.com"));
        Assert.That(settings.ApiKey, Is.EqualTo("env-api-key"));
        Assert.That(settings.HmacSharedSecretKey, Is.EqualTo("env-hmac-secret"));
    }

    [Test]
    public void GivenConfigurationWithSpecialCharacters_WhenAddingNuciNotificationsSettings_ThenValuesAreBoundCorrectly()
    {
        var specialConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NuciNotificationsSettings:BaseUrl"] = "https://api.example.com:8080/path",
                ["NuciNotificationsSettings:ApiKey"] = "key-with-special_chars.123",
                ["NuciNotificationsSettings:HmacSharedSecretKey"] = "secret+with=special&chars"
            })
            .Build();

        _services.AddNuciNotificationsSettings(specialConfig);
        var provider = _services.BuildServiceProvider();

        var settings = provider.GetRequiredService<NuciNotificationsSettings>();

        Assert.That(settings.BaseUrl, Is.EqualTo("https://api.example.com:8080/path"));
        Assert.That(settings.ApiKey, Is.EqualTo("key-with-special_chars.123"));
        Assert.That(settings.HmacSharedSecretKey, Is.EqualTo("secret+with=special&chars"));
    }
}