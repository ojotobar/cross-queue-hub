using CrossQueue.Hub.Services.Interfaces;
using CrossQueue.Hub.Shared.Models;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using System.Text.Json;
using System.Text;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Logging;

namespace CrossQueue.Hub.Services.Implementations
{
    public class RabbitMQPubSub : IRabbitMQPubSub
    {
        private readonly CrossQueueOptions _options;
        private readonly IRabbitMQConnection _connection;
        private IModel _channel;
        private readonly AsyncRetryPolicy _publishRetryPolicy;
        private readonly AsyncRetryPolicy _consumerRetryPolicy;
        private readonly ILogger<RabbitMQPubSub> _logger;

        public RabbitMQPubSub(IRabbitMQConnection connection, 
            IOptions<CrossQueueOptions> options, ILogger<RabbitMQPubSub> logger)
        {
            _connection = connection;
            _logger = logger;
            _options = options.Value;

            _channel = _connection.CreateChannel();
            _channel.ExchangeDeclare(_options.RabbitMQ.DefaultExchange,
                                     _options.RabbitMQ.DefaultExchangeType,
                                     durable: _options.RabbitMQ.Durable);

            // Retry policies
            _publishRetryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    _options.RabbitMQ.PublishRetryCount,
                    attempt => TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * _options.RabbitMQ.PublishRetryDelayMs),
                    (ex, ts, attempt, ctx) =>
                    {
                        _logger.LogInformation($"[Publish Retry] Attempt {attempt}: {ex.Message}");
                    });

            _consumerRetryPolicy = Policy
                .Handle<Exception>()
               .WaitAndRetryAsync(
                   _options.RabbitMQ.ConsumerRetryCount,
                   attempt => TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * _options.RabbitMQ.ConsumerRetryDelayMs),
                   (ex, ts, attempt, ctx) =>
                   {
                       _logger.LogInformation($"[Consumer Retry] Attempt {attempt}: {ex.Message}");
                   });
        }

        public async Task PublishAsync<T>(T message, string? exchange = null, string routingKey = "")
        {
            var targetExchange = exchange ?? _options.RabbitMQ.DefaultExchange;

            await _publishRetryPolicy.ExecuteAsync(async () =>
            {
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
                var props = _channel.CreateBasicProperties();
                props.Persistent = true;

                _channel.BasicPublish(exchange: targetExchange,
                                      routingKey: routingKey,
                                      basicProperties: props,
                                      body: body);

                await Task.CompletedTask;
            });
        }

        public void Subscribe<T>(string queue, Func<T, Task> handler, string? exchange = null, string routingKey = "")
        {
            var targetExchange = exchange ?? _options.RabbitMQ.DefaultExchange;
            _channel = _connection.CreateChannel();
            _channel.ExchangeDeclare(targetExchange,
                                     _options.RabbitMQ.DefaultExchangeType,
                                     durable: _options.RabbitMQ.Durable);

            var args = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", _options.RabbitMQ.DeadLetterExchange }
            };

            _channel.QueueDeclare(queue, durable: _options.RabbitMQ.Durable, exclusive: false, autoDelete: false, arguments: args);
            _channel.QueueBind(queue, targetExchange, routingKey);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (_, ea) =>
            {
                try
                {
                    var message = JsonSerializer.Deserialize<T>(ea.Body.ToArray())!;
                    await _consumerRetryPolicy.ExecuteAsync(async () => await handler(message));
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (JsonException ex)
                {
                    _logger.LogError($"A JsonException occurred: {ex.Message}");
                    _channel.BasicNack(ea.DeliveryTag, false, requeue: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling message from {Queue}", queue);
                    _channel.BasicNack(ea.DeliveryTag, false, requeue: false);
                }
            };

            _channel.BasicConsume(queue, autoAck: false, consumer);
            _logger.LogInformation("Subscribed to {Queue} with routingKey {RoutingKey}", queue, routingKey);
        }
    }
}
