using RabbitMQ.Client;

namespace CrossQueue.Hub.Models
{
    public class RabbitMQOptions
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string DefaultExchange { get; set; } = "default-exchange";
        public string DefaultExchangeType { get; set; } = ExchangeType.Fanout;
        public bool Durable { get; set; } = true;

        public int PublishRetryCount { get; set; } = 5;
        public int PublishRetryDelayMs { get; set; } = 200;
        public int ConsumerRetryCount { get; set; } = 5;
        public int ConsumerRetryDelayMs { get; set; } = 500;

        public string DeadLetterExchange { get; set; } = "dlx";
    }
}
