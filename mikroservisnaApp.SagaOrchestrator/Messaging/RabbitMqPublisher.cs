using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace mikroservisnaApp.SagaOrchestrator.Messaging
{
    public class RabbitMqPublisher : IAsyncDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;

        private RabbitMqPublisher(IConnection connection, IChannel channel)
        {
            _connection = connection;
            _channel = channel;
        }

        public static async Task<RabbitMqPublisher> CreateAsync(string hostName = "localhost")
        {
            var factory = new ConnectionFactory()
            {
                HostName = hostName,
                UserName = "guest",
                Password = "guest"
            };

            var connection = await factory.CreateConnectionAsync();
            var channel = await connection.CreateChannelAsync();
            return new RabbitMqPublisher(connection, channel);
        }

        public async Task PublishAsync(string queueName, object message)
        {
            await _channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            await _channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queueName,
                body: body);

            Console.WriteLine($"[SAGA] Poslata poruka na queue: {queueName}");
        }
        public async Task PublishRawAsync(string queueName, byte[] body)
        {
            await _channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            await _channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queueName,
                body: body);

            Console.WriteLine($"[SAGA] Poslata raw poruka na queue: {queueName}");
        }
        public async ValueTask DisposeAsync()
        {
            if (_channel != null) await _channel.DisposeAsync();
            if (_connection != null) await _connection.DisposeAsync();
        }
    }
}