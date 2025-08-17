using CrossQueue.Hub.Models;

namespace CrossQueue.Hub.Shared.Models
{
    public class CrossQueueOptions
    {
        public RabbitMQOptions RabbitMQ { get; set; } = new();
    }
}