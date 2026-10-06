using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using NuciNotifications.Client.Configuration;

namespace NuciNotifications.Client
{
    public static class ServiceCollectionExtensions
    {
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