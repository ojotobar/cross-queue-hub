using CrossQueue.Hub.Services.Interfaces;
using CrossQueue.Hub.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace CrossQueue.Hub.Services.Implementations
{
    public class RabbitMQConnection : IRabbitMQConnection
    {
        private readonly ConnectionFactory _factory;
        private IConnection? _connection;
        private readonly object _lock = new();
        private readonly ILogger<RabbitMQConnection> _logger;

        public RabbitMQConnection(IOptions<CrossQueueOptions> options, ILogger<RabbitMQConnection> logger)
        {
            _factory = new ConnectionFactory
            {
                Uri = new Uri(options.Value.RabbitMQ.ConnectionString),
                DispatchConsumersAsync = true
            };
            _logger = logger;
        }

        public IConnection GetConnection()
        {
            const int maxRetries = 10;
            var delay = TimeSpan.FromSeconds(30);
            for (int i = 1; i <= maxRetries; i++)
            {
                try
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
                catch (BrokerUnreachableException ex)
                {
                    _logger.LogWarning($"RabbitMQ not ready yet: {ex.Message}");
                    if (i == maxRetries)
                        throw new Exception("RabbitMQ connection failed after max retries.", ex);
                    Thread.Sleep(delay);
                }
            }

            throw new Exception("Unexpected error: all retries exhausted.");
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
