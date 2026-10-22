using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

using NuciAPI.Requests;

using NuciSecurity.HMAC;

namespace NuciNotifications.Client.Requests
{
    /// <summary>
    /// Represents an email notification and its optional attachments.
    /// </summary>
    public class SendEmailRequest : NuciApiRequest, IValidatableObject
    {
        /// <summary>Gets or sets the optional sender name.</summary>
        [HmacOrder(1)]
        public string Sender { get; set; }

        /// <summary>Gets or sets the recipient address.</summary>
        [Required]
        [HmacOrder(2)]
        public string Recipient { get; set; }

        /// <summary>Gets or sets the email subject.</summary>
        [Required]
        [HmacOrder(3)]
        public string Subject { get; set; }

        /// <summary>Gets or sets the email body.</summary>
        [Required]
        [HmacOrder(4)]
        public string Body { get; set; }

        /// <summary>Gets or sets the optional ordered attachment array.</summary>
        [HmacIgnore]
        public EmailAttachmentRequest[] Attachments { get; set; }

        /// <summary>Gets the API-compatible attachment content used for HMAC signing.</summary>
        [JsonIgnore]
        [HmacOrder(5)]
        public string AttachmentsSignatureContent => GetAttachmentsSignatureContent();

        /// <summary>Validates every attachment, including null array elements.</summary>
        /// <param name="validationContext">The validation context.</param>
        /// <returns>The attachment validation errors.</returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Attachments is null)
            {
                yield break;
            }

            foreach (EmailAttachmentRequest attachment in Attachments)
            {
                if (attachment is null)
                {
                    yield return new ValidationResult(
                        "Attachments must not contain null entries.",
                        [nameof(Attachments)]);
                }
                else
                {
                    List<ValidationResult> results = [];
                    Validator.TryValidateObject(attachment, new ValidationContext(attachment), results, true);

                    foreach (ValidationResult result in results)
                    {
                        yield return new ValidationResult(result.ErrorMessage, [nameof(Attachments)]);
                    }
                }
            }
        }

        private string GetAttachmentsSignatureContent()
        {
            if (Attachments is null || Equals(Attachments.Length, 0))
            {
                return string.Empty;
            }

            return JsonSerializer.Serialize(Attachments, JsonSerializerOptions.Web);
        }
    }
}
