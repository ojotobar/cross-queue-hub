namespace CrossQueue.Hub.Services.Interfaces
{
    public interface IRabbitMQPubSub
    {
        Task PublishAsync<T>(T message, string? exchange = null, string routingKey = "");
        void Subscribe<T>(string queue, Func<T, Task> handler, string? exchange = null, string routingKey = "");
    }
}
