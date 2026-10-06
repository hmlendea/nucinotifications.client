using System;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NuciAPI.Client;
using NuciAPI.Responses;
using NuciNotifications.Client;
using NuciNotifications.Client.Configuration;
using NuciNotifications.Client.Requests;

namespace NuciNotifications.Client.UnitTests;

[TestFixture]
public class NuciNotificationsClientTests
{
    private Mock<INuciApiClient> _mockApiClient = null!;
    private NuciNotificationsSettings _settings = null!;
    private NuciNotificationsClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _mockApiClient = new Mock<INuciApiClient>();
        _settings = new NuciNotificationsSettings
        {
            BaseUrl = "https://api.example.com",
            ApiKey = "test-api-key",
            HmacSharedSecretKey = "test-hmac-secret"
        };
        _client = new NuciNotificationsClient(_settings, _mockApiClient.Object);
    }

    [Test]
    public async Task GivenValidParameters_WhenSendEmailWithoutSenderName_ThenCallsApiClientWithCorrectRequest()
    {
        var response = new NuciApiSuccessResponse("OK") { Code = "200" };
        _mockApiClient.Setup(x => x.SendRequestAsync<SendEmailRequest, NuciApiSuccessResponse>(
                HttpMethod.Post,
                It.Is<SendEmailRequest>(r => r.Recipient == "test@example.com" && r.Subject == "Test Subject" && r.Body == "Test Body" && r.Sender == null),
                It.IsAny<NuciApiRequestAuthorisationInfo>(),
                "/Email"))
            .ReturnsAsync(response);

        await _client.SendEmail("test@example.com", "Test Subject", "Test Body");

        _mockApiClient.Verify(x => x.SendRequestAsync<SendEmailRequest, NuciApiSuccessResponse>(
            HttpMethod.Post,
            It.Is<SendEmailRequest>(r => r.Recipient == "test@example.com" && r.Subject == "Test Subject" && r.Body == "Test Body" && r.Sender == null),
            It.Is<NuciApiRequestAuthorisationInfo>(a => a.BearerToken == "test-api-key" && a.HmacSharedSecretKey == "test-hmac-secret"),
            "/Email"), Times.Once);
    }

    [Test]
    public async Task GivenValidParameters_WhenSendEmailWithSenderName_ThenCallsApiClientWithCorrectRequest()
    {
        var response = new NuciApiSuccessResponse("OK") { Code = "200" };
        _mockApiClient.Setup(x => x.SendRequestAsync<SendEmailRequest, NuciApiSuccessResponse>(
                HttpMethod.Post,
                It.Is<SendEmailRequest>(r => r.Recipient == "test@example.com" && r.Subject == "Test Subject" && r.Body == "Test Body" && r.Sender == "Test Sender"),
                It.IsAny<NuciApiRequestAuthorisationInfo>(),
                "/Email"))
            .ReturnsAsync(response);

        await _client.SendEmail("Test Sender", "test@example.com", "Test Subject", "Test Body");

        _mockApiClient.Verify(x => x.SendRequestAsync<SendEmailRequest, NuciApiSuccessResponse>(
            HttpMethod.Post,
            It.Is<SendEmailRequest>(r => r.Recipient == "test@example.com" && r.Subject == "Test Subject" && r.Body == "Test Body" && r.Sender == "Test Sender"),
            It.Is<NuciApiRequestAuthorisationInfo>(a => a.BearerToken == "test-api-key" && a.HmacSharedSecretKey == "test-hmac-secret"),
            "/Email"), Times.Once);
    }

    [Test]
    public async Task GivenApiClientThrowsException_WhenSendEmail_ThenThrowsSmtpExceptionWithOriginalException()
    {
        var originalException = new HttpRequestException("Network error");
        _mockApiClient.Setup(x => x.SendRequestAsync<SendEmailRequest, NuciApiSuccessResponse>(
                HttpMethod.Post,
                It.IsAny<SendEmailRequest>(),
                It.IsAny<NuciApiRequestAuthorisationInfo>(),
                "/Email"))
            .ThrowsAsync(originalException);

        var act = async () => await _client.SendEmail("test@example.com", "Test Subject", "Test Body");

        var exception = await act.Should().ThrowAsync<SmtpException>()
            .WithMessage("Error while sending the e-mail notification.");

        exception.Which.InnerException.Should().Be(originalException);
    }

    [Test]
    public async Task GivenApiReturnsUnsuccessfulResponse_WhenSendEmail_ThenThrowsSmtpExceptionWithErrorMessage()
    {
        var errorResponse = new NuciApiErrorResponse("Bad Request", "400");
        _mockApiClient.Setup(x => x.SendRequestAsync<SendEmailRequest, NuciApiSuccessResponse>(
                HttpMethod.Post,
                It.IsAny<SendEmailRequest>(),
                It.IsAny<NuciApiRequestAuthorisationInfo>(),
                "/Email"))
            .ReturnsAsync(errorResponse);

        var act = async () => await _client.SendEmail("test@example.com", "Test Subject", "Test Body");

        await act.Should().ThrowAsync<SmtpException>()
            .WithMessage("Bad Request");
    }

    [Test]
    public async Task GivenApiReturnsAuthenticationFailure_WhenSendEmail_ThenThrowsSmtpException()
    {
        var errorResponse = new NuciApiErrorResponse("Authentication Failure", "401");
        _mockApiClient.Setup(x => x.SendRequestAsync<SendEmailRequest, NuciApiSuccessResponse>(
                HttpMethod.Post,
                It.IsAny<SendEmailRequest>(),
                It.IsAny<NuciApiRequestAuthorisationInfo>(),
                "/Email"))
            .ReturnsAsync(errorResponse);

        var act = async () => await _client.SendEmail("test@example.com", "Test Subject", "Test Body");

        await act.Should().ThrowAsync<SmtpException>()
            .WithMessage("Authentication Failure");
    }

    [Test]
    public async Task GivenApiReturnsUnauthorised_WhenSendEmail_ThenThrowsSmtpException()
    {
        var errorResponse = new NuciApiErrorResponse("Unauthorised", "403");
        _mockApiClient.Setup(x => x.SendRequestAsync<SendEmailRequest, NuciApiSuccessResponse>(
                HttpMethod.Post,
                It.IsAny<SendEmailRequest>(),
                It.IsAny<NuciApiRequestAuthorisationInfo>(),
                "/Email"))
            .ReturnsAsync(errorResponse);

        var act = async () => await _client.SendEmail("test@example.com", "Test Subject", "Test Body");

        await act.Should().ThrowAsync<SmtpException>()
            .WithMessage("Unauthorised");
    }

    [Test]
    public async Task GivenApiReturnsRateLimited_WhenSendEmail_ThenThrowsSmtpException()
    {
        var errorResponse = new NuciApiErrorResponse("Rate Limited", "429");
        _mockApiClient.Setup(x => x.SendRequestAsync<SendEmailRequest, NuciApiSuccessResponse>(
                HttpMethod.Post,
                It.IsAny<SendEmailRequest>(),
                It.IsAny<NuciApiRequestAuthorisationInfo>(),
                "/Email"))
            .ReturnsAsync(errorResponse);

        var act = async () => await _client.SendEmail("test@example.com", "Test Subject", "Test Body");

        await act.Should().ThrowAsync<SmtpException>()
            .WithMessage("Rate Limited");
    }

    [Test]
    public async Task GivenApiReturnsInternalServerError_WhenSendEmail_ThenThrowsSmtpException()
    {
        var errorResponse = new NuciApiErrorResponse("Internal Server Error", "500");
        _mockApiClient.Setup(x => x.SendRequestAsync<SendEmailRequest, NuciApiSuccessResponse>(
                HttpMethod.Post,
                It.IsAny<SendEmailRequest>(),
                It.IsAny<NuciApiRequestAuthorisationInfo>(),
                "/Email"))
            .ReturnsAsync(errorResponse);

        var act = async () => await _client.SendEmail("test@example.com", "Test Subject", "Test Body");

        await act.Should().ThrowAsync<SmtpException>()
            .WithMessage("Internal Server Error");
    }

    [Test]
    public async Task GivenNullRecipient_WhenSendEmail_ThenThrowsArgumentNullException()
    {
        var act = async () => await _client.SendEmail(null!, "Test Subject", "Test Body");

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("recipient");
    }

    [Test]
    public async Task GivenEmptyRecipient_WhenSendEmail_ThenThrowsArgumentException()
    {
        var act = async () => await _client.SendEmail("", "Test Subject", "Test Body");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("recipient");
    }

    [Test]
    public async Task GivenNullSubject_WhenSendEmail_ThenThrowsArgumentNullException()
    {
        var act = async () => await _client.SendEmail("test@example.com", null!, "Test Body");

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("subject");
    }

    [Test]
    public async Task GivenEmptySubject_WhenSendEmail_ThenThrowsArgumentException()
    {
        var act = async () => await _client.SendEmail("test@example.com", "", "Test Body");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("subject");
    }

    [Test]
    public async Task GivenNullBody_WhenSendEmail_ThenThrowsArgumentNullException()
    {
        var act = async () => await _client.SendEmail("test@example.com", "Test Subject", null!);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("body");
    }

    [Test]
    public async Task GivenEmptyBody_WhenSendEmail_ThenThrowsArgumentException()
    {
        var act = async () => await _client.SendEmail("test@example.com", "Test Subject", "");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("body");
    }

    [Test]
    public async Task GivenWhitespaceRecipient_WhenSendEmail_ThenThrowsArgumentException()
    {
        var act = async () => await _client.SendEmail("   ", "Test Subject", "Test Body");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("recipient");
    }

    [Test]
    public async Task GivenWhitespaceSubject_WhenSendEmail_ThenThrowsArgumentException()
    {
        var act = async () => await _client.SendEmail("test@example.com", "   ", "Test Body");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("subject");
    }

    [Test]
    public async Task GivenWhitespaceBody_WhenSendEmail_ThenThrowsArgumentException()
    {
        var act = async () => await _client.SendEmail("test@example.com", "Test Subject", "   ");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("body");
    }

    [Test]
    public async Task GivenSpecialCharactersInParameters_WhenSendEmail_ThenCallsApiClientCorrectly()
    {
        var response = new NuciApiSuccessResponse("OK") { Code = "200" };
        _mockApiClient.Setup(x => x.SendRequestAsync<SendEmailRequest, NuciApiSuccessResponse>(
                HttpMethod.Post,
                It.IsAny<SendEmailRequest>(),
                It.IsAny<NuciApiRequestAuthorisationInfo>(),
                "/Email"))
            .ReturnsAsync(response);

        await _client.SendEmail("test+tag@example.com", "Subject with \"quotes\" & <html>", "Body with \n newlines & unicode: 🎉");

        _mockApiClient.Verify(x => x.SendRequestAsync<SendEmailRequest, NuciApiSuccessResponse>(
            HttpMethod.Post,
            It.Is<SendEmailRequest>(r => r.Recipient == "test+tag@example.com" && r.Subject == "Subject with \"quotes\" & <html>" && r.Body == "Body with \n newlines & unicode: 🎉"),
            It.IsAny<NuciApiRequestAuthorisationInfo>(),
            "/Email"), Times.Once);
    }

    [Test]
    public async Task GivenLongStrings_WhenSendEmail_ThenCallsApiClientCorrectly()
    {
        var response = new NuciApiSuccessResponse("OK") { Code = "200" };
        var longSubject = new string('a', 1000);
        var longBody = new string('b', 10000);
        _mockApiClient.Setup(x => x.SendRequestAsync<SendEmailRequest, NuciApiSuccessResponse>(
                HttpMethod.Post,
                It.IsAny<SendEmailRequest>(),
                It.IsAny<NuciApiRequestAuthorisationInfo>(),
                "/Email"))
            .ReturnsAsync(response);

        await _client.SendEmail("test@example.com", longSubject, longBody);

        _mockApiClient.Verify(x => x.SendRequestAsync<SendEmailRequest, NuciApiSuccessResponse>(
            HttpMethod.Post,
            It.Is<SendEmailRequest>(r => r.Subject.Length == 1000 && r.Body.Length == 10000),
            It.IsAny<NuciApiRequestAuthorisationInfo>(),
            "/Email"), Times.Once);
    }

    [Test]
    public async Task GivenUnicodeCharacters_WhenSendEmail_ThenCallsApiClientCorrectly()
    {
        var response = new NuciApiSuccessResponse("OK") { Code = "200" };
        _mockApiClient.Setup(x => x.SendRequestAsync<SendEmailRequest, NuciApiSuccessResponse>(
                HttpMethod.Post,
                It.IsAny<SendEmailRequest>(),
                It.IsAny<NuciApiRequestAuthorisationInfo>(),
                "/Email"))
            .ReturnsAsync(response);

        await _client.SendEmail("用户@例子.测试", "测试主题 🎉", "测试正文 🌟");

        _mockApiClient.Verify(x => x.SendRequestAsync<SendEmailRequest, NuciApiSuccessResponse>(
            HttpMethod.Post,
            It.Is<SendEmailRequest>(r => r.Recipient == "用户@例子.测试" && r.Subject == "测试主题 🎉" && r.Body == "测试正文 🌟"),
            It.IsAny<NuciApiRequestAuthorisationInfo>(),
            "/Email"), Times.Once);
    }

    [Test]
    public async Task GivenNullSenderName_WhenSendEmailWithSenderName_ThenPassesNullToRequest()
    {
        var response = new NuciApiSuccessResponse("OK") { Code = "200" };
        _mockApiClient.Setup(x => x.SendRequestAsync<SendEmailRequest, NuciApiSuccessResponse>(
                HttpMethod.Post,
                It.Is<SendEmailRequest>(r => r.Sender == null),
                It.IsAny<NuciApiRequestAuthorisationInfo>(),
                "/Email"))
            .ReturnsAsync(response);

        await _client.SendEmail(null!, "test@example.com", "Test Subject", "Test Body");

        _mockApiClient.Verify(x => x.SendRequestAsync<SendEmailRequest, NuciApiSuccessResponse>(
            HttpMethod.Post,
            It.Is<SendEmailRequest>(r => r.Sender == null),
            It.IsAny<NuciApiRequestAuthorisationInfo>(),
            "/Email"), Times.Once);
    }

    [Test]
    public async Task GivenEmptySenderName_WhenSendEmailWithSenderName_ThenPassesEmptyStringToRequest()
    {
        var response = new NuciApiSuccessResponse("OK") { Code = "200" };
        _mockApiClient.Setup(x => x.SendRequestAsync<SendEmailRequest, NuciApiSuccessResponse>(
                HttpMethod.Post,
                It.Is<SendEmailRequest>(r => r.Sender == ""),
                It.IsAny<NuciApiRequestAuthorisationInfo>(),
                "/Email"))
            .ReturnsAsync(response);

        await _client.SendEmail("", "test@example.com", "Test Subject", "Test Body");

        _mockApiClient.Verify(x => x.SendRequestAsync<SendEmailRequest, NuciApiSuccessResponse>(
            HttpMethod.Post,
            It.Is<SendEmailRequest>(r => r.Sender == ""),
            It.IsAny<NuciApiRequestAuthorisationInfo>(),
            "/Email"), Times.Once);
    }

    [Test]
    public async Task GivenApiClientThrowsTaskCanceledException_WhenSendEmail_ThenThrowsSmtpException()
    {
        var originalException = new TaskCanceledException("Request timeout");
        _mockApiClient.Setup(x => x.SendRequestAsync<SendEmailRequest, NuciApiSuccessResponse>(
                HttpMethod.Post,
                It.IsAny<SendEmailRequest>(),
                It.IsAny<NuciApiRequestAuthorisationInfo>(),
                "/Email"))
            .ThrowsAsync(originalException);

        var act = async () => await _client.SendEmail("test@example.com", "Test Subject", "Test Body");

        var exception = await act.Should().ThrowAsync<SmtpException>()
            .WithMessage("Error while sending the e-mail notification.");

        exception.Which.InnerException.Should().Be(originalException);
    }

    [Test]
    public async Task GivenApiReturnsServiceUnavailable_WhenSendEmail_ThenThrowsSmtpException()
    {
        var errorResponse = new NuciApiErrorResponse("Service Unavailable", "503");
        _mockApiClient.Setup(x => x.SendRequestAsync<SendEmailRequest, NuciApiSuccessResponse>(
                HttpMethod.Post,
                It.IsAny<SendEmailRequest>(),
                It.IsAny<NuciApiRequestAuthorisationInfo>(),
                "/Email"))
            .ReturnsAsync(errorResponse);

        var act = async () => await _client.SendEmail("test@example.com", "Test Subject", "Test Body");

        await act.Should().ThrowAsync<SmtpException>()
            .WithMessage("Service Unavailable");
    }
}