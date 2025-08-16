using CrossQueue.Hub.Models;

namespace CrossQueue.Hub.Shared.Models
{
    public class CrossQueueSettings
    {
        public RabbitMQSetting RabbitMQ { get; set; } = new();
    }
}