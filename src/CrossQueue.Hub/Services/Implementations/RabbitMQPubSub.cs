using CrossQueue.Hub.Shared.Models;
using RabbitMQ.Client;
using System.Text.Json;
using System.Text;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Logging;

namespace CrossQueue.Hub.Services.Implementations
{
    public class RabbitMQPubSub
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly CrossQueueSettings _settings;
        private readonly ILogger _logger;
        private const string dlq = "dead_letter_queue";
        private const string dlx = "dead_letter_exchange";
        private const string retryX = "retry_exchange";
        private const string retryQ = "retry_queue";
        private const int maxRetry = 3;

        public RabbitMQPubSub(IConnection connection, CrossQueueSettings settings, 
            ILogger<RabbitMQPubSub> logger)
        {
            _connection = connection;
            _channel = connection.CreateModel();
            _settings = settings;
            _logger = logger;
            _channel.ExchangeDeclare(exchange: _settings.RabbitMQ.Exchange, 
                type: ExchangeType.Direct, durable: true);
            SetupDeadLetterQueue();
        }

        public void Publish<TMessage>(TMessage message, string routingKey)
        {
            var json = JsonSerializer.Serialize(message);
            var encodedMessage = Encoding.UTF8.GetBytes(json);
            var properties = _channel.CreateBasicProperties();
            properties.DeliveryMode = 2;

            _channel.BasicPublish(exchange: _settings.RabbitMQ.Exchange,
                             routingKey: routingKey,
                             basicProperties: properties,
                             body: encodedMessage);
        }

        public void Subscribe<TMessage>(string queue, string routingKey, Func<TMessage, Task> handler, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                _channel.BasicQos(0, 1, false);

                var channelConsumer = new AsyncEventingBasicConsumer(_channel);
                _channel.QueueDeclare(queue: queue, durable: true, exclusive: false, autoDelete: false);

                // Queue (DLX set to retry exchange)
                var mainArgs = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", retryX }
            };

                _channel.QueueBind(queue: queue,
                    exchange: _settings.RabbitMQ.Exchange,
                    routingKey: routingKey,
                    arguments: mainArgs);

                SetupRetryExchangeAndQueue(queue);
                _logger.LogInformation(" [*] Waiting for messages...");

                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += (model, ea) =>
                {
                    try
                    {
                        var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                        var message = JsonSerializer.Deserialize<TMessage>(json);
                        if (message == null)
                        {
                            _logger.LogWarning($"The message of type {typeof(TMessage)} is null");
                            return;
                        }

                        handler(message);
                        _channel.BasicAck(ea.DeliveryTag, multiple: false);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError($"JSON parsing failed: {ex.Message}");
                        _channel.BasicNack(ea.DeliveryTag, false, requeue: false); // goes to DLQ
                    }
                    catch (Exception)
                    {
                        var props = ea.BasicProperties;
                        var headers = props.Headers ?? new Dictionary<string, object>();
                        int retryCount = headers.ContainsKey("x-retry-count")
                            ? Convert.ToInt32(headers["x-retry-count"])
                            : 0;

                        if (retryCount >= maxRetry)
                        {
                            _logger.LogInformation($"Sending to DLQ after {maxRetry} retries");
                            var dlqProps = _channel.CreateBasicProperties();
                            _channel.BasicPublish(dlx, "", dlqProps, ea.Body);
                        }
                        else
                        {
                            Console.WriteLine($"Retry {retryCount + 1} after backoff");

                            var retryProps = _channel.CreateBasicProperties();
                            retryProps.Headers = new Dictionary<string, object>
                        {
                            { "x-retry-count", retryCount + 1 }
                        };

                            _channel.BasicPublish(dlx, "retry", retryProps, ea.Body);
                        }

                        _channel.BasicAck(ea.DeliveryTag, false); // Acknowledge regardless
                    }
                };

                _channel.BasicConsume(queue: queue, autoAck: true, consumer: consumer);
            }
        }

        private void SetupRetryExchangeAndQueue(string mainQueue)
        {
            // Retry Exchange
            _channel.ExchangeDeclare(retryX, ExchangeType.Direct);

            // Retry Queue (TTL + DLX back to main)
            var retryArgs = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", _settings.RabbitMQ.Exchange }, // send back to default exchange
                { "x-dead-letter-routing-key", mainQueue },
                { "x-message-ttl", 30000 } // 10 sec delay for retry
            };
            
            _channel.QueueDeclare(retryQ, true, false, false, retryArgs);
            _channel.QueueBind(retryQ, retryX, "retry");

            _logger.LogInformation("Main + Retry + DLQ declared");
        }

        private void SetupDeadLetterQueue()
        {
            // Dead Letter Exchange + Queue
            _channel.ExchangeDeclare(dlx, ExchangeType.Fanout);
            _channel.QueueDeclare(dlq, true, false, false, null);
            _channel.QueueBind(dlq, dlx, "");
        }
    }
}
