using System.ComponentModel.DataAnnotations;

namespace NuciNotifications.Client.Requests
{
    /// <summary>
    /// Describes a file attached to an email notification.
    /// </summary>
    public sealed class EmailAttachmentRequest
    {
        /// <summary>
        /// Gets or sets the file name without directory separators or control characters.
        /// </summary>
        [Required]
        [StringLength(255)]
        [RegularExpression(@"\A[^/\\\p{Cc}]+\z")]
        public string FileName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the non-empty Base64-encoded file content.
        /// </summary>
        [Required]
        [Base64String]
        public string Content { get; set; } = null!;
    }
}