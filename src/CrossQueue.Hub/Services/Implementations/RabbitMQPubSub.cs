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
        private IModel _publisherChannel;
        private readonly AsyncRetryPolicy _publishRetryPolicy;
        private readonly AsyncRetryPolicy _consumerRetryPolicy;
        private readonly ILogger<RabbitMQPubSub> _logger;

        public RabbitMQPubSub(IRabbitMQConnection connection, 
            IOptions<CrossQueueOptions> options, ILogger<RabbitMQPubSub> logger)
        {
            _connection = connection;
            _logger = logger;
            _options = options.Value;

            _publisherChannel = _connection.CreateChannel();
            _publisherChannel.ExchangeDeclare(_options.RabbitMQ.DefaultExchange,
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
                var props = _publisherChannel.CreateBasicProperties();
                props.Persistent = true;

                _publisherChannel.BasicPublish(
                    exchange: targetExchange,
                    routingKey: routingKey,
                    basicProperties: props,
                    body: body
                );

                await Task.CompletedTask;
            });
        }

        public void Subscribe<T>(string queue, Func<T, Task> handler, string? exchange = null, string routingKey = "")
        {
            var targetExchange = exchange ?? _options.RabbitMQ.DefaultExchange;

            // ✅ Create a dedicated channel for this subscription
            var channel = _connection.CreateChannel();

            channel.ExchangeDeclare(
                targetExchange,
                _options.RabbitMQ.DefaultExchangeType,
                durable: _options.RabbitMQ.Durable
            );

            var args = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", _options.RabbitMQ.DeadLetterExchange }
            };

            channel.QueueDeclare(
                queue,
                durable: _options.RabbitMQ.Durable,
                exclusive: false,
                autoDelete: false,
                arguments: args
            );

            channel.QueueBind(queue, targetExchange, routingKey);

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.Received += async (_, ea) =>
            {
                try
                {
                    var message = JsonSerializer.Deserialize<T>(ea.Body.ToArray(), new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (message is null)
                    {
                        _logger.LogWarning("Null/invalid message received on {Queue}", queue);
                        channel.BasicReject(ea.DeliveryTag, requeue: false); // send to DLX
                        return;
                    }

                    await _consumerRetryPolicy.ExecuteAsync(() => handler(message));

                    channel.BasicAck(ea.DeliveryTag, multiple: false);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Invalid JSON for message from {Queue}", queue);
                    channel.BasicReject(ea.DeliveryTag, requeue: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling message from {Queue}", queue);
                    channel.BasicReject(ea.DeliveryTag, requeue: false);
                }
            };

            channel.BasicConsume(queue, autoAck: false, consumer);

            channel.ModelShutdown += (_, args) =>
            {
                _logger.LogWarning("Channel for {Queue} closed: {ReplyCode} - {ReplyText}",
                    queue, args.ReplyCode, args.ReplyText);
            };

            _logger.LogInformation("Subscribed to {Queue} with routingKey {RoutingKey}", queue, routingKey);
        }
    }
}
