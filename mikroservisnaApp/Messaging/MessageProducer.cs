using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace mikroservisnaApp.Messaging
{
    public class MessageProducer : IAsyncDisposable
    {
        private const string ExchangeName = "predavac.exchange";
        private const string QueueName = "predavac.queue";
        private const string RoutingKey = "predavac.events";

        private IConnection? _connection;
        private IChannel? _channel;

        public async Task InitializeAsync()
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest"
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            await _channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Direct, durable: true, autoDelete: false);
            await _channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false);
            await _channel.QueueBindAsync(QueueName, ExchangeName, RoutingKey);
        }

        public async Task PublishAsync<T>(string eventType, T data)
        {
            if (_channel == null)
                await InitializeAsync();

            var message = JsonSerializer.Serialize(new { EventType = eventType, Data = data });
            var body = Encoding.UTF8.GetBytes(message);

            var properties = new BasicProperties { Persistent = true };

            await _channel!.BasicPublishAsync(
                exchange: ExchangeName,
                routingKey: RoutingKey,
                mandatory: false,
                basicProperties: properties,
                body: body);
        }

        public async ValueTask DisposeAsync()
        {
            if (_channel != null) await _channel.DisposeAsync();
            if (_connection != null) await _connection.DisposeAsync();
        }
    }
}