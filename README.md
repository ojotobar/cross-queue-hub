# CrossQueue.Hub

[![NuGet](https://img.shields.io/nuget/v/CrossQueue.Hub.svg)](https://www.nuget.org/packages/CrossQueue.Hub)
[![NuGet Downloads](https://img.shields.io/nuget/dt/CrossQueue.Hub.svg)](https://www.nuget.org/packages/CrossQueue.Hub)

# CrossQueue.Hub

CrossQueue.Hub is a lightweight .NET library that provides a unified abstraction for working with multiple message brokers such as **RabbitMQ**, **Apache Kafka**, and **AWS SQS**.  
It allows you to publish and subscribe to messages in a transport-agnostic way, keeping your application code clean and scalable.

---

## 🚀 Features
- Unified interface for RabbitMQ, Kafka, and AWS SQS.
- JSON message serialization/deserialization built-in.
- Publisher/Subscriber abstraction for simplicity.
- Easily extendable to other message brokers.
- Configurable and production-ready.

---

## 📦 Installation
```bash
dotnet add package CrossQueue.Hub
```

---

## ⚡ Usage

### 1. Configure the broker in your appsettings.json
```json
{
  "CrossQueueHub": {
    "RabbitMQ": {
        "ConnectionString": "amqp://guest:guest@localhost:5672/",
        "DefaultExchange": "default-exchange",
        "DefaultExchangeType": "fanout",
        "Durable": true,
        "PublishRetryCount": 5,
        "PublishRetryDelayMs": 200,
        "ConsumerRetryCount": 5,
        "ConsumerRetryDelayMs": 500,
        "DeadLetterExchange": "dlx"
    }
  }
}
```

### 2. Register CrossQueue.Hub in Program.cs
The are two overloads of the ```AddCrossQueueHubRabbitMqBus()``` method
```csharp
// Using Action<CrossQueueOptions> overload for code-based config
builder.Services.AddRabbitMqBus(opt =>
{
    opt.ConnectionString = "amqp://guest:guest@localhost:5672/";
    opt.DefaultExchange = "myapp.exchange";
    opt.DefaultExchangeType = ExchangeType.Topic;
});

// Using IConfiguration overload
builder.Services.AddRabbitMqBus(builder.Configuration);

// OR using custom section name.
// In this case, your section in the appsettings.json will be 'MyCustomRabbitMq' instead of 'CrossQueueHub'
builder.Services.AddRabbitMqBus(builder.Configuration, "MyCustomRabbitMq");
```

### 3. Inject the RabbitMQPubSub class into the constructor of the class you want to use it.
```csharp
public class RabbitMQPubSubTest
{
  private readonly IRabbitMQPubSub _pubSub;

  public RabbitMQPubSubUser(IRabbitMQPubSub pubSub)
  {
    _pubSub = pubSub;
  }
}
```

### 4. Publish a message
```csharp
await _pubSub.PublishAsync(new { Text = "Hello World" }, routingKey: "my.route");
```

### 5. Subscribe to a message
```csharp
_pubSub.Subscribe<dynamic>("my.queue", async msg =>
{
    Console.WriteLine($"Received: {msg.Text}");
    await Task.CompletedTask;
}, routingKey: "my.route");
```

---

## 🛠 Supported Brokers
- ✅ RabbitMQ  

---

## 📖 Roadmap
- Apache Kafka  
- AWS SQS  
- Add support for Azure Service Bus.
- Metrics & observability integrations.

---

## 🤝 Contributing
Pull requests are welcome! For major changes, please open an issue first to discuss what you’d like to change.

---

## 📜 License
MIT License