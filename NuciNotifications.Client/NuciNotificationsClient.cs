using System;
using System.Net.Http;
using System.Net.Mail;
using System.Threading.Tasks;
using NuciAPI.Client;
using NuciAPI.Responses;
using NuciNotifications.Client.Configuration;
using NuciNotifications.Client.Requests;

namespace NuciNotifications.Client
{
    /// <summary>
    /// Implements the INuciNotificationsClient interface to provide functionality for sending notifications using the NuciNotifications API.
    /// </summary>
    /// <param name="settings">The NuciNotifications settings.</param>
    /// <param name="apiClient">The Nuci API client.</param>
    public class NuciNotificationsClient(
        NuciNotificationsSettings settings,
        INuciApiClient? apiClient = null) : INuciNotificationsClient
    {
        readonly INuciApiClient apiClient = apiClient ?? new NuciApiClient(settings.BaseUrl);

        /// <summary>
        /// Sends an email notification request.
        /// </summary>
        /// <param name="recipient">The email address of the recipient.</param>
        /// <param name="subject">The subject of the email.</param>
        /// <param name="body">The body content of the email.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SendEmail(
            string recipient,
            string subject,
            string body)
            => await SendEmail(null, recipient, subject, body);

        /// <summary>
        /// Sends an email notification request.
        /// </summary>
        /// <param name="senderName">The name of the sender.</param>
        /// <param name="recipient">The email address of the recipient.</param>
        /// <param name="subject">The subject of the email.</param>
        /// <param name="body">The body content of the email.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when recipient, subject, or body is null.</exception>
        /// <exception cref="ArgumentException">Thrown when recipient, subject, or body is empty or whitespace.</exception>
        public async Task SendEmail(
            string senderName,
            string recipient,
            string subject,
            string body)
        {
            ArgumentNullException.ThrowIfNull(recipient);
            ArgumentNullException.ThrowIfNull(subject);
            ArgumentNullException.ThrowIfNull(body);

            if (string.IsNullOrWhiteSpace(recipient))
                throw new ArgumentException("Recipient cannot be empty or whitespace.", nameof(recipient));
            if (string.IsNullOrWhiteSpace(subject))
                throw new ArgumentException("Subject cannot be empty or whitespace.", nameof(subject));
            if (string.IsNullOrWhiteSpace(body))
                throw new ArgumentException("Body cannot be empty or whitespace.", nameof(body));

            NuciApiRequestAuthorisationInfo authorisationInfo = new()
            {
                BearerToken = settings.ApiKey,
                HmacSharedSecretKey = settings.HmacSharedSecretKey
            };

            NuciApiResponse response;

            try
            {
                response =
                    await apiClient.SendRequestAsync<SendEmailRequest, NuciApiSuccessResponse>(
                        HttpMethod.Post,
                        new SendEmailRequest()
                        {
                            Sender = senderName,
                            Recipient = recipient,
                            Subject = subject,
                            Body = body
                        },
                        authorisationInfo,
                        "/Email");
            }
            catch (Exception ex)
            {
                throw new SmtpException("Error while sending the e-mail notification.", ex);
            }

            if (!response.IsSuccessful)
            {
                throw new SmtpException(((NuciApiErrorResponse)response).Message);
            }
        }
    }
}
