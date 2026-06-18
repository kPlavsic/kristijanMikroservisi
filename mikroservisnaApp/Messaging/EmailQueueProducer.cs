using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using mikroservisnaApp.Shared.Events;

namespace mikroservisnaApp.Messaging
{
    public class EmailQueueProducer : IAsyncDisposable
    {
        private const string EmailQueue = "email.queue";

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

            await _channel.QueueDeclareAsync(
                queue: EmailQueue,
                durable: true,
                exclusive: false,
                autoDelete: false);
        }

        public async Task EnqueueAsync(SendEmailEvent emailEvent)
        {
            if (_channel is null)
                await InitializeAsync();

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(emailEvent));

            var properties = new BasicProperties
            {
                Persistent = true,
                MessageId = emailEvent.MessageId
            };

            await _channel!.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: EmailQueue,
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