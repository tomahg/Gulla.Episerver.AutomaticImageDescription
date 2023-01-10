using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gulla.Episerver.AutomaticImageDescription.Core
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAutomaticImageDescription(this IServiceCollection services)
        {
            return AddAutomaticImageDescription(services, _ => { });
        }

        public static IServiceCollection AddAutomaticImageDescription(this IServiceCollection services, Action<AutomaticImageDescriptionOptions> setupAction)
        {
            services.AddOptions<AutomaticImageDescriptionOptions>().Configure<IConfiguration>((options, configuration) =>
            {
                setupAction(options);
                configuration.GetSection("Gulla:AutomaticImageDescription").Bind(options);
            });

            return services;
        }
    }
}
