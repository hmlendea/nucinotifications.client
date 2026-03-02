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
    public class NuciNotificationsClient(
        NuciNotificationsSettings settings) : INuciNotificationsClient
    {
        readonly NuciApiClient apiClient = new(settings.BaseUrl);

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
        public async Task SendEmail(
            string senderName,
            string recipient,
            string subject,
            string body)
        {
            NuciApiRequestAuthorisationInfo authorisationInfo = new()
            {
                BearerToken = settings.ApiKey,
                HmacSharedSecretKey = settings.HmacSharedSecretKey
            };

            NuciApiResponse response =
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

            if (!response.IsSuccessful)
            {
                throw new SmtpException(((NuciApiErrorResponse)response).Message);
            }
        }
    }
}
