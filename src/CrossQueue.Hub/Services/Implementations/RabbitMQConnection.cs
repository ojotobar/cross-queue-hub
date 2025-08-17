using CrossQueue.Hub.Models;
using CrossQueue.Hub.Services.Interfaces;
using CrossQueue.Hub.Shared.Models;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace CrossQueue.Hub.Services.Implementations
{
    public class RabbitMQConnection : IRabbitMQConnection
    {
        private readonly ConnectionFactory _factory;
        private IConnection? _connection;
        private readonly object _lock = new();

        public RabbitMQConnection(IOptions<CrossQueueOptions> options)
        {
            _factory = new ConnectionFactory
            {
                Uri = new Uri(options.Value.RabbitMQ.ConnectionString),
                DispatchConsumersAsync = true
            };
        }

        public IConnection GetConnection()
        {
            if (_connection is { IsOpen: true })
                return _connection;

            lock (_lock)
            {
                if (_connection is { IsOpen: true })
                    return _connection;

                _connection = _factory.CreateConnection();
            }

            return _connection!;
        }

        public IModel CreateChannel()
        {
            return GetConnection().CreateModel();
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}
