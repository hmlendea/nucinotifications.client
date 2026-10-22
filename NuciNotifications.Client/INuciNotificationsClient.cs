using System.Collections.Generic;
using System.Threading.Tasks;

using NuciNotifications.Client.Requests;

namespace NuciNotifications.Client
{
    /// <summary>
    /// Defines the interface for a client that can send notifications using the NuciNotifications API.
    /// </summary>
    public interface INuciNotificationsClient
    {
        /// <summary>
        /// Sends an email notification request.
        /// </summary>
        /// <param name="recipient">The email address of the recipient.</param>
        /// <param name="subject">The subject of the email.</param>
        /// <param name="body">The body content of the email.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SendEmail(
            string recipient,
            string subject,
            string body);

        /// <summary>
        /// Sends an email notification request.
        /// </summary>
        /// <param name="senderName">The name of the sender.</param>
        /// <param name="recipient">The email address of the recipient.</param>
        /// <param name="subject">The subject of the email.</param>
        /// <param name="body">The body content of the email.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SendEmail(
            string senderName,
            string recipient,
            string subject,
            string body);

        /// <summary>
        /// Sends an email notification with optional attachments.
        /// </summary>
        /// <param name="senderName">The sender name, or null for the default sender.</param>
        /// <param name="recipient">The recipient address.</param>
        /// <param name="subject">The email subject.</param>
        /// <param name="body">The email body.</param>
        /// <param name="attachments">The ordered attachments, or null for no attachments.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SendEmail(
            string senderName,
            string recipient,
            string subject,
            string body,
            IEnumerable<EmailAttachmentRequest> attachments);
    }
}
