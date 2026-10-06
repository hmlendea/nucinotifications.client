using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using NUnit.Framework;
using NuciNotifications.Client.Requests;

namespace NuciNotifications.Client.UnitTests;

[TestFixture]
public class SendEmailRequestTests
{
    [Test]
    public void GivenDefaultConstructor_WhenCreatingInstance_ThenAllPropertiesAreNull()
    {
        var request = new SendEmailRequest();

        Assert.That(request.Sender, Is.Null);
        Assert.That(request.Recipient, Is.Null);
        Assert.That(request.Subject, Is.Null);
        Assert.That(request.Body, Is.Null);
    }

    [Test]
    public void GivenValidValues_WhenSettingProperties_ThenPropertiesRetainValues()
    {
        var request = new SendEmailRequest
        {
            Sender = "Test Sender",
            Recipient = "recipient@example.com",
            Subject = "Test Subject",
            Body = "Test Body"
        };

        Assert.That(request.Sender, Is.EqualTo("Test Sender"));
        Assert.That(request.Recipient, Is.EqualTo("recipient@example.com"));
        Assert.That(request.Subject, Is.EqualTo("Test Subject"));
        Assert.That(request.Body, Is.EqualTo("Test Body"));
    }

    [Test]
    public void GivenNullSender_WhenSettingProperties_ThenSenderIsNull()
    {
        var request = new SendEmailRequest
        {
            Sender = null,
            Recipient = "recipient@example.com",
            Subject = "Test Subject",
            Body = "Test Body"
        };

        Assert.That(request.Sender, Is.Null);
    }

    [Test]
    public void GivenEmptyRecipient_WhenValidating_ThenValidationFails()
    {
        var request = new SendEmailRequest
        {
            Sender = "Test Sender",
            Recipient = "",
            Subject = "Test Subject",
            Body = "Test Body"
        };

        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, context, results, true);

        Assert.That(isValid, Is.False);
        Assert.That(results, Has.Some.Matches<ValidationResult>(r => r.MemberNames.Contains("Recipient")));
    }

    [Test]
    public void GivenNullRecipient_WhenValidating_ThenValidationFails()
    {
        var request = new SendEmailRequest
        {
            Sender = "Test Sender",
            Recipient = null,
            Subject = "Test Subject",
            Body = "Test Body"
        };

        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, context, results, true);

        Assert.That(isValid, Is.False);
        Assert.That(results, Has.Some.Matches<ValidationResult>(r => r.MemberNames.Contains("Recipient")));
    }

    [Test]
    public void GivenEmptySubject_WhenValidating_ThenValidationFails()
    {
        var request = new SendEmailRequest
        {
            Sender = "Test Sender",
            Recipient = "recipient@example.com",
            Subject = "",
            Body = "Test Body"
        };

        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, context, results, true);

        Assert.That(isValid, Is.False);
        Assert.That(results, Has.Some.Matches<ValidationResult>(r => r.MemberNames.Contains("Subject")));
    }

    [Test]
    public void GivenNullSubject_WhenValidating_ThenValidationFails()
    {
        var request = new SendEmailRequest
        {
            Sender = "Test Sender",
            Recipient = "recipient@example.com",
            Subject = null,
            Body = "Test Body"
        };

        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, context, results, true);

        Assert.That(isValid, Is.False);
        Assert.That(results, Has.Some.Matches<ValidationResult>(r => r.MemberNames.Contains("Subject")));
    }

    [Test]
    public void GivenEmptyBody_WhenValidating_ThenValidationFails()
    {
        var request = new SendEmailRequest
        {
            Sender = "Test Sender",
            Recipient = "recipient@example.com",
            Subject = "Test Subject",
            Body = ""
        };

        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, context, results, true);

        Assert.That(isValid, Is.False);
        Assert.That(results, Has.Some.Matches<ValidationResult>(r => r.MemberNames.Contains("Body")));
    }

    [Test]
    public void GivenNullBody_WhenValidating_ThenValidationFails()
    {
        var request = new SendEmailRequest
        {
            Sender = "Test Sender",
            Recipient = "recipient@example.com",
            Subject = "Test Subject",
            Body = null
        };

        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, context, results, true);

        Assert.That(isValid, Is.False);
        Assert.That(results, Has.Some.Matches<ValidationResult>(r => r.MemberNames.Contains("Body")));
    }

    [Test]
    public void GivenAllRequiredFields_WhenValidating_ThenValidationPasses()
    {
        var request = new SendEmailRequest
        {
            Sender = "Test Sender",
            Recipient = "recipient@example.com",
            Subject = "Test Subject",
            Body = "Test Body"
        };

        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, context, results, true);

        Assert.That(isValid, Is.True);
        Assert.That(results, Is.Empty);
    }

    [Test]
    public void GivenOnlyRequiredFields_WhenValidating_ThenValidationPasses()
    {
        var request = new SendEmailRequest
        {
            Recipient = "recipient@example.com",
            Subject = "Test Subject",
            Body = "Test Body"
        };

        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, context, results, true);

        Assert.That(isValid, Is.True);
        Assert.That(results, Is.Empty);
    }

    [Test]
    public void GivenWhitespaceRecipient_WhenValidating_ThenValidationFails()
    {
        var request = new SendEmailRequest
        {
            Sender = "Test Sender",
            Recipient = "   ",
            Subject = "Test Subject",
            Body = "Test Body"
        };

        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, context, results, true);

        Assert.That(isValid, Is.False);
        Assert.That(results, Has.Some.Matches<ValidationResult>(r => r.MemberNames.Contains("Recipient")));
    }

    [Test]
    public void GivenSpecialCharactersInFields_WhenSettingProperties_ThenPropertiesRetainValues()
    {
        var request = new SendEmailRequest
        {
            Sender = "Sender <sender@example.com>",
            Recipient = "recipient+tag@example.com",
            Subject = "Subject with \"quotes\" & <tags>",
            Body = "Body with\nnewlines\tand\ttabs"
        };

        Assert.That(request.Sender, Is.EqualTo("Sender <sender@example.com>"));
        Assert.That(request.Recipient, Is.EqualTo("recipient+tag@example.com"));
        Assert.That(request.Subject, Is.EqualTo("Subject with \"quotes\" & <tags>"));
        Assert.That(request.Body, Is.EqualTo("Body with\nnewlines\tand\ttabs"));
    }

    [Test]
    public void GivenLongStrings_WhenSettingProperties_ThenPropertiesRetainValues()
    {
        var longString = new string('a', 10000);
        var request = new SendEmailRequest
        {
            Sender = longString,
            Recipient = longString + "@example.com",
            Subject = longString,
            Body = longString
        };

        Assert.That(request.Sender, Is.EqualTo(longString));
        Assert.That(request.Recipient, Is.EqualTo(longString + "@example.com"));
        Assert.That(request.Subject, Is.EqualTo(longString));
        Assert.That(request.Body, Is.EqualTo(longString));
    }

    [Test]
    public void GivenUnicodeCharacters_WhenSettingProperties_ThenPropertiesRetainValues()
    {
        var request = new SendEmailRequest
        {
            Sender = "Tëst Sëndër",
            Recipient = "recipient@exämple.com",
            Subject = "Tëst Sübject 🎉",
            Body = "Bödy with émojis 🚀 and ünïcödé"
        };

        Assert.That(request.Sender, Is.EqualTo("Tëst Sëndër"));
        Assert.That(request.Recipient, Is.EqualTo("recipient@exämple.com"));
        Assert.That(request.Subject, Is.EqualTo("Tëst Sübject 🎉"));
        Assert.That(request.Body, Is.EqualTo("Bödy with émojis 🚀 and ünïcödé"));
    }
}