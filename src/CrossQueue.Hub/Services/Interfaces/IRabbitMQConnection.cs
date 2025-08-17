using RabbitMQ.Client;

namespace CrossQueue.Hub.Services.Interfaces
{
    public interface IRabbitMQConnection : IDisposable
    {
        IConnection GetConnection();
        IModel CreateChannel();
    }
}
