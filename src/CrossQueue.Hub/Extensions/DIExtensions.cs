using CrossQueue.Hub.Services.Implementations;
using CrossQueue.Hub.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;

namespace CrossQueue.Hub.Shared.Extensions
{
    public static class DIExtensions
    {
        public static IServiceCollection AddCrossQueueHub(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<CrossQueueSettings>(
                configuration.GetSection("CrossQueueHub"));

            services.AddSingleton(sp =>
            {
                using var scope = sp.CreateScope();

                var settings = scope.ServiceProvider
                    .GetRequiredService<IOptions<CrossQueueSettings>>().Value;

                var logger = sp
                     .GetRequiredService<ILoggerFactory>()
                     .CreateLogger<RabbitMQPubSub>();

                var factory = new ConnectionFactory
                {
                    Uri = new Uri(settings.RabbitMQ.Connection),
                    AutomaticRecoveryEnabled = true,
                };

                using var connection = factory.CreateConnection();

                return new RabbitMQPubSub(connection, settings, logger);
            });
    
            return services;
        }
    }
}