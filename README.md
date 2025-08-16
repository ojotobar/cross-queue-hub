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
    "Provider": "RabbitMQ",
    "RabbitMQ": {
      "Host": "amqp://user:password@hostname/vhost"
    },
    "Kafka": {
      "BootstrapServers": "localhost:9092"
    },
    "SQS": {
      "Region": "us-east-1",
      "AccessKey": "your-access-key",
      "SecretKey": "your-secret-key"
    }
  }
}
```

### 2. Register CrossQueue.Hub in Program.cs
```csharp
builder.Services.AddCrossQueueHub(builder.Configuration);
```

### 3. Publish a message
```csharp
await _publisher.PublishAsync("orders.created", new { OrderId = 123, Amount = 250.00 });
```

### 4. Subscribe to a message
```csharp
_subscriber.Subscribe<OrderCreated>("orders.created", message =>
{
    Console.WriteLine($"Order received: {message.OrderId} - {message.Amount}");
});
```

---

## 🛠 Supported Brokers
- ✅ RabbitMQ  
- ✅ Apache Kafka  
- ✅ AWS SQS  

---

## 📖 Roadmap
- Add support for Azure Service Bus.
- Add retry policies and dead-letter queue handling.
- Metrics & observability integrations.

---

## 🤝 Contributing
Pull requests are welcome! For major changes, please open an issue first to discuss what you’d like to change.

---

## 📜 License
MIT License