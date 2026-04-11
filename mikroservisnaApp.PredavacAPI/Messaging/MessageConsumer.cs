using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace mikroservisnaApp.PredavacAPI.Messaging
{
    public class MessageConsumer : BackgroundService
    {
        private const string ExchangeName = "predavac.exchange";
        private const string QueueName = "predavac.queue";
        private const string RoutingKey = "predavac.events";

        private IConnection? _connection;
        private IChannel? _channel;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
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

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (sender, eventArgs) =>
            {
                var body = eventArgs.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                Console.WriteLine($"[RECEIVED] {DateTime.Now:HH:mm:ss} | {message}");

                await _channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
            };

            await _channel.BasicConsumeAsync(queue: QueueName, autoAck: false, consumer: consumer);

            // Drži servis živ dok aplikacija radi
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_channel != null) await _channel.DisposeAsync();
            if (_connection != null) await _connection.DisposeAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}