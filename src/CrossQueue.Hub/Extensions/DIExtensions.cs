using CrossQueue.Hub.Services.Implementations;
using CrossQueue.Hub.Services.Interfaces;
using CrossQueue.Hub.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CrossQueue.Hub.Shared.Extensions
{
    public static class DIExtensions
    {
        public static IServiceCollection AddCrossQueueHubRabbitMqBus(this IServiceCollection services, 
            Action<CrossQueueOptions> configure)
        {
            services.Configure(configure);
            services.AddSingleton<IRabbitMQConnection, RabbitMQConnection>();
            services.AddSingleton<IRabbitMQPubSub, RabbitMQPubSub>();

            return services;
        }

        public static IServiceCollection AddCrossQueueHubRabbitMqBus(this IServiceCollection services, 
            IConfiguration configuration, string sectionName = "CrossQueueHub")
        {
            services.Configure<CrossQueueOptions>(configuration.GetSection(sectionName));
            services.AddSingleton<IRabbitMQConnection, RabbitMQConnection>();
            services.AddSingleton<IRabbitMQPubSub, RabbitMQPubSub>();

            return services;
        }
    }
}