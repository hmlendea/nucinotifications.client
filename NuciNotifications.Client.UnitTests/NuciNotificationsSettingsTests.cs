using System;
using NUnit.Framework;
using NuciNotifications.Client.Configuration;

namespace NuciNotifications.Client.UnitTests;

[TestFixture]
public class NuciNotificationsSettingsTests
{
    [Test]
    public void GivenDefaultConstructor_WhenCreatingInstance_ThenAllPropertiesAreNull()
    {
        var settings = new NuciNotificationsSettings();

        Assert.That(settings.BaseUrl, Is.Null);
        Assert.That(settings.ApiKey, Is.Null);
        Assert.That(settings.HmacSharedSecretKey, Is.Null);
    }

    [Test]
    public void GivenValidValues_WhenSettingProperties_ThenPropertiesRetainValues()
    {
        var settings = new NuciNotificationsSettings
        {
            BaseUrl = "https://api.example.com",
            ApiKey = "test-api-key",
            HmacSharedSecretKey = "test-hmac-secret"
        };

        Assert.That(settings.BaseUrl, Is.EqualTo("https://api.example.com"));
        Assert.That(settings.ApiKey, Is.EqualTo("test-api-key"));
        Assert.That(settings.HmacSharedSecretKey, Is.EqualTo("test-hmac-secret"));
    }

    [Test]
    public void GivenEmptyStrings_WhenSettingProperties_ThenPropertiesAreEmptyStrings()
    {
        var settings = new NuciNotificationsSettings
        {
            BaseUrl = "",
            ApiKey = "",
            HmacSharedSecretKey = ""
        };

        Assert.That(settings.BaseUrl, Is.EqualTo(""));
        Assert.That(settings.ApiKey, Is.EqualTo(""));
        Assert.That(settings.HmacSharedSecretKey, Is.EqualTo(""));
    }

    [Test]
    public void GivenWhitespaceStrings_WhenSettingProperties_ThenPropertiesRetainWhitespace()
    {
        var settings = new NuciNotificationsSettings
        {
            BaseUrl = "   ",
            ApiKey = "  ",
            HmacSharedSecretKey = "\t"
        };

        Assert.That(settings.BaseUrl, Is.EqualTo("   "));
        Assert.That(settings.ApiKey, Is.EqualTo("  "));
        Assert.That(settings.HmacSharedSecretKey, Is.EqualTo("\t"));
    }

    [Test]
    public void GivenNullValues_WhenSettingProperties_ThenPropertiesAreNull()
    {
        var settings = new NuciNotificationsSettings
        {
            BaseUrl = null,
            ApiKey = null,
            HmacSharedSecretKey = null
        };

        Assert.That(settings.BaseUrl, Is.Null);
        Assert.That(settings.ApiKey, Is.Null);
        Assert.That(settings.HmacSharedSecretKey, Is.Null);
    }

    [Test]
    public void GivenSettingsInstance_WhenModifyingProperties_ThenChangesAreReflected()
    {
        var settings = new NuciNotificationsSettings
        {
            BaseUrl = "https://original.com",
            ApiKey = "original-key",
            HmacSharedSecretKey = "original-secret"
        };

        settings.BaseUrl = "https://modified.com";
        settings.ApiKey = "modified-key";
        settings.HmacSharedSecretKey = "modified-secret";

        Assert.That(settings.BaseUrl, Is.EqualTo("https://modified.com"));
        Assert.That(settings.ApiKey, Is.EqualTo("modified-key"));
        Assert.That(settings.HmacSharedSecretKey, Is.EqualTo("modified-secret"));
    }
}