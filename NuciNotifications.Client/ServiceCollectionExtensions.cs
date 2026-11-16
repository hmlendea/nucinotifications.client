using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using NuciNotifications.Client.Configuration;

namespace NuciNotifications.Client
{
    /// <summary>
    /// Provides service collection registration helpers for the NuciNotifications client.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Binds and registers the NuciNotifications settings from the named configuration section.
        /// </summary>
        /// <param name="services">The service collection to register the settings in.</param>
        /// <param name="configuration">The configuration that supplies the settings values.</param>
        /// <returns>The same service collection, to allow further registration chaining.</returns>
        public static IServiceCollection AddNuciNotificationsSettings(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<NuciNotificationsSettings>(configuration.GetSection(nameof(NuciNotificationsSettings)));

            services.AddSingleton(serviceProvider =>
                serviceProvider.GetRequiredService<IOptions<NuciNotificationsSettings>>().Value);

            return services;
        }
    }
}