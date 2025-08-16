using CrossQueue.Hub.Services.Contracts;
using CrossQueue.Hub.Services.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace CrossQueue.Hub.Shared.Extensions
{
    public static class DIExtensions
    {
        public static IServiceCollection ConfigureRabbitCQ(this IServiceCollection services)
        {
            services.AddScoped<IRabbitCrossQueue, RabbitCrossQueue>();
            return services;
        }
    }
}