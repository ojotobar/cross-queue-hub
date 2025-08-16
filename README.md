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
      "Connection": "amqp://user:password@hostname/vhost",
      "Exchange": "default"
    }
  }
}
```

### 2. Register CrossQueue.Hub in Program.cs
```csharp
builder.Services.AddCrossQueueHub(builder.Configuration);
```

### 3. Inject the RabbitMQPubSub class into the constructor of the class you want to use it.
```csharp
public class RabbitMQPubSubUser
{
  private readonly RabbitMQPubSub _pubSub;

  public RabbitMQPubSubUser(RabbitMQPubSub pubSub)
  {
    _pubSub = pubSub;
  }
}
```

### 4. Publish a message
```csharp
_pubSub.Publish(new { OrderId = 123, Amount = 250.00 }, "order.created");
```

### 5. Subscribe to a message
```csharp
_pubSub.Subscribe<object>("order-queue", "order.created", async message =>
{
    Console.WriteLine(message);
    await Task.CompletedTask;
}, CancellationToken.None);
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