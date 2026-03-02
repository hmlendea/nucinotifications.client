namespace NuciNotifications.Client.Configuration
{
    /// <summary>
    /// Represents the settings required to configure the NuciNotifications client.
    /// </summary>
    public sealed class NuciNotificationsSettings
    {
        /// <summary>
        /// Gets or sets the base URL of the NuciNotifications API.
        /// </summary>
        public string BaseUrl { get; set; }

        /// <summary>
        /// Gets or sets the API key used for authenticating requests to the NuciNotifications API.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the HMAC shared secret key used for signing requests to the NuciNotifications API.
        /// </summary>
        public string HmacSharedSecretKey { get; set; }
    }
}
