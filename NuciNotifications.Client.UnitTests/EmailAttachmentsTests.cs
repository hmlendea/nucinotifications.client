using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Net.Mail;
using System.Text.Json;
using System.Threading.Tasks;

using Moq;

using NUnit.Framework;

using NuciAPI.Client;
using NuciAPI.Responses;

using NuciSecurity.HMAC;

using NuciNotifications.Client.Configuration;
using NuciNotifications.Client.Requests;

namespace NuciNotifications.Client.UnitTests
{
    [TestFixture]
    public sealed class EmailAttachmentsTests
    {
        private Mock<INuciApiClient> apiClient = null!;
        private INuciNotificationsClient client = null!;

        [SetUp]
        public void SetUp()
        {
            apiClient = new(MockBehavior.Strict);
            client = new NuciNotificationsClient(new()
            {
                BaseUrl = "https://test.nucilandia.ro",
                ApiKey = "TestPassword!",
                HmacSharedSecretKey = "Test1234!"
            }, apiClient.Object);
        }

        [TestCase(null, "U29sYWlyZQ==", false)]
        [TestCase("", "U29sYWlyZQ==", false)]
        [TestCase(" ", "U29sYWlyZQ==", false)]
        [TestCase("sun.txt", null, false)]
        [TestCase("sun.txt", "", false)]
        [TestCase("sun.txt", "invalid!", false)]
        [TestCase("../sun.txt", "U29sYWlyZQ==", false)]
        [TestCase(@"..\sun.txt", "U29sYWlyZQ==", false)]
        [TestCase("sun\n.txt", "U29sYWlyZQ==", false)]
        [TestCase("sun.txt\n", "U29sYWlyZQ==", false)]
        [TestCase("sun\0.txt", "U29sYWlyZQ==", false)]
        [TestCase("sun\t.txt", "U29sYWlyZQ==", false)]
        [TestCase("sun.txt", "U29sYWlyZQ==", true)]
        [TestCase("sun.bin", "AAECA/7/", true)]
        public void GivenAttachmentFields_WhenValidating_ThenTheApiContractIsEnforced(
            string? fileName,
            string? content,
            bool isExpectedToBeValid)
        {
            SendEmailRequest request = BuildRequest();
            request.Attachments = [new() { FileName = fileName!, Content = content! }];
            List<ValidationResult> results = [];

            bool isValid = Validator.TryValidateObject(request, new(request), results, true);

            Assert.That(isValid, Is.EqualTo(isExpectedToBeValid));
            Assert.That(results.All(result => result.MemberNames.Contains(nameof(request.Attachments))));
        }

        [TestCase(254, true)]
        [TestCase(255, true)]
        [TestCase(256, false)]
        public void GivenAFileNameLength_WhenValidating_ThenTheApiLimitIsEnforced(
            int length,
            bool isExpectedToBeValid)
        {
            EmailAttachmentRequest attachment = new()
            {
                FileName = new string('a', length),
                Content = "U29sYWlyZQ=="
            };

            Assert.That(
                Validator.TryValidateObject(attachment, new(attachment), [], true),
                Is.EqualTo(isExpectedToBeValid));
        }

        [TestCase("{}")]
        [TestCase("{\"attachments\":null}")]
        [TestCase("{\"attachments\":[]}")]
        public void GivenNoAttachments_WhenSigning_ThenOptionalFormsAreEquivalent(string json)
        {
            SendEmailRequest request = JsonSerializer.Deserialize<SendEmailRequest>(
                json,
                JsonSerializerOptions.Web)!;
            SendEmailRequest absentRequest = new();

            Assert.That(request.AttachmentsSignatureContent, Is.Empty);
            Assert.That(request.Validate(new(request)), Is.Empty);
            Assert.That(
                HmacEncoder.GenerateToken(request, "Test1234!"),
                Is.EqualTo(HmacEncoder.GenerateToken(absentRequest, "Test1234!")));
        }

        [Test]
        public void GivenAttachments_WhenSerialising_ThenTheExactApiJsonIsSignedAndRoundTrips()
        {
            SendEmailRequest request = BuildRequest();
            string expectedContent = "[{\"fileName\":\"sun.txt\",\"content\":\"U29sYWlyZQ==\"}," +
                "{\"fileName\":\"sun.bin\",\"content\":\"AAECA/7/\"}]";
            string json = JsonSerializer.Serialize(request, JsonSerializerOptions.Web);
            SendEmailRequest received = JsonSerializer.Deserialize<SendEmailRequest>(
                json,
                JsonSerializerOptions.Web)!;

            Assert.That(request.AttachmentsSignatureContent, Is.EqualTo(expectedContent));
            Assert.That(json, Does.Contain("\"attachments\":" + expectedContent));
            Assert.That(json, Does.Not.Contain("attachmentsSignatureContent"));
            Assert.That(received.AttachmentsSignatureContent, Is.EqualTo(expectedContent));
            Assert.That(
                HmacEncoder.GenerateToken(received, "Test1234!"),
                Is.EqualTo(HmacEncoder.GenerateToken(request, "Test1234!")));
        }

        [TestCase("name")]
        [TestCase("content")]
        [TestCase("order")]
        [TestCase("remove")]
        public void GivenSignedAttachments_WhenTampering_ThenTheTokenIsInvalid(string change)
        {
            SendEmailRequest request = BuildRequest();
            string token = HmacEncoder.GenerateToken(request, "Test1234!");

            switch (change)
            {
                case "name":
                    request.Attachments![0].FileName = "changed.txt";
                    break;
                case "content":
                    request.Attachments![0].Content = "AAECA/7/";
                    break;
                case "order":
                    request.Attachments = [request.Attachments![1], request.Attachments[0]];
                    break;
                case "remove":
                    request.Attachments = [];
                    break;
            }

            Assert.That(HmacValidator.IsTokenValid(token, request, "Test1234!"), Is.False);
        }

        [TestCase(null)]
        [TestCase("Solaire of Astora")]
        public async Task GivenAttachments_WhenSending_ThenTheOrderedRequestAndAuthorisationArePreserved(
            string? senderName)
        {
            SendEmailRequest expected = BuildRequest();
            SetupResponse(new NuciApiSuccessResponse("OK") { Code = "200" });

            await client.SendEmail(
                senderName!, expected.Recipient, expected.Subject, expected.Body, expected.Attachments!);

            apiClient.Verify(transport => transport.SendRequestAsync<SendEmailRequest, NuciApiSuccessResponse>(
                HttpMethod.Post,
                It.Is<SendEmailRequest>(request =>
                    string.Equals(request.Sender, senderName) &&
                    string.Equals(request.Recipient, expected.Recipient) &&
                    string.Equals(request.Subject, expected.Subject) &&
                    string.Equals(request.Body, expected.Body) &&
                    string.Equals(request.AttachmentsSignatureContent, expected.AttachmentsSignatureContent)),
                It.Is<NuciApiRequestAuthorisationInfo>(authorisation =>
                    string.Equals(authorisation.BearerToken, "TestPassword!") &&
                    string.Equals(authorisation.HmacSharedSecretKey, "Test1234!")),
                "/Email"), Times.Once);
            apiClient.VerifyNoOtherCalls();
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task GivenOptionalAttachments_WhenSending_ThenNullAndEmptyAreAccepted(bool hasArray)
        {
            IEnumerable<EmailAttachmentRequest> attachments = null!;

            if (hasArray)
            {
                attachments = [];
            }

            SetupResponse(new NuciApiSuccessResponse("OK") { Code = "200" });

            await client.SendEmail(null!, "solaire@astora.com", "Praise the Sun!", "Solaire", attachments);

            apiClient.Verify(transport => transport.SendRequestAsync<SendEmailRequest, NuciApiSuccessResponse>(
                HttpMethod.Post,
                It.Is<SendEmailRequest>(request => string.IsNullOrEmpty(request.AttachmentsSignatureContent)),
                It.IsAny<NuciApiRequestAuthorisationInfo>(),
                "/Email"), Times.Once);
        }

        [TestCase("null")]
        [TestCase("name")]
        [TestCase("content")]
        public async Task GivenInvalidAttachments_WhenSending_ThenNoRequestIsDispatched(string invalidField)
        {
            EmailAttachmentRequest attachment = new() { FileName = "sun.txt", Content = "U29sYWlyZQ==" };

            switch (invalidField)
            {
                case "null":
                    attachment = null!;
                    break;
                case "name":
                    attachment.FileName = "../sun.txt";
                    break;
                case "content":
                    attachment.Content = "invalid!";
                    break;
            }

            ArgumentException? exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                client.SendEmail(null!, "solaire@astora.com", "Praise the Sun!", "Solaire", [attachment]));

            Assert.That(exception!.ParamName, Is.EqualTo("attachments"));
            apiClient.VerifyNoOtherCalls();
        }

        [Test]
        public async Task GivenAnAttachmentApiError_WhenSending_ThenTheExistingExceptionContractIsPreserved()
        {
            SetupResponse(new NuciApiErrorResponse("Invalid attachment", "400"));

            SmtpException? exception = await Assert.ThrowsAsync<SmtpException>(() =>
                client.SendEmail(null!, "solaire@astora.com", "Praise the Sun!", "Solaire", BuildRequest().Attachments!));

            Assert.That(exception!.Message, Is.EqualTo("Invalid attachment"));
        }

        private void SetupResponse(NuciApiResponse response)
            => apiClient.Setup(transport => transport.SendRequestAsync<SendEmailRequest, NuciApiSuccessResponse>(
                HttpMethod.Post,
                It.IsAny<SendEmailRequest>(),
                It.IsAny<NuciApiRequestAuthorisationInfo>(),
                "/Email")).ReturnsAsync(response);

        private static SendEmailRequest BuildRequest() => new()
        {
            Recipient = "solaire@astora.com",
            Subject = "Praise the Sun!",
            Body = "Solaire",
            Attachments =
            [
                new() { FileName = "sun.txt", Content = "U29sYWlyZQ==" },
                new() { FileName = "sun.bin", Content = "AAECA/7/" }
            ]
        };
    }
}