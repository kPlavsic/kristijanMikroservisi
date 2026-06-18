using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using mikroservisnaApp.Shared.Events;

namespace mikroservisnaApp.HostedServices
{
    public class EmailWorker : BackgroundService
    {
        private const string EmailQueue = "email.queue";
        private const int MaxEmailsPerMinute = 10;

        private readonly ILogger<EmailWorker> _logger;

        private readonly Queue<DateTime> _sentEmailTimestamps = new();

        private IConnection? _connection;
        private IChannel? _channel;

        public EmailWorker(ILogger<EmailWorker> logger)
        {
            _logger = logger;
        }

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

            await _channel.QueueDeclareAsync(
                queue: EmailQueue,
                durable: true,
                exclusive: false,
                autoDelete: false);

            
            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) => await HandleEmailAsync(ea, stoppingToken);

            await _channel.BasicConsumeAsync(queue: EmailQueue, autoAck: false, consumer: consumer);

            _logger.LogInformation("EmailWorker pokrenut, slusa na: {Queue}", EmailQueue);

            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) { }
        }

        private async Task HandleEmailAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            if (_channel is null) return;

            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var emailEvent = JsonSerializer.Deserialize<SendEmailEvent>(body);

                if (emailEvent is null)
                {
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    return;
                }

                await WaitIfRateLimitReachedAsync(cancellationToken);

                await SendEmailAsync(emailEvent);

                _sentEmailTimestamps.Enqueue(DateTime.UtcNow);

                _logger.LogInformation("Email poslat za: {To}, Subject: {Subject}", emailEvent.To, emailEvent.Subject);

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Greska pri slanju emaila");
                await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
            }
        }

        private async Task WaitIfRateLimitReachedAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                var now = DateTime.UtcNow;
                var oneMinuteAgo = now.AddMinutes(-1);

                while (_sentEmailTimestamps.Count > 0 && _sentEmailTimestamps.Peek() < oneMinuteAgo)
                {
                    _sentEmailTimestamps.Dequeue();
                }

                
                if (_sentEmailTimestamps.Count < MaxEmailsPerMinute)
                    return;

                
                var oldestTimestamp = _sentEmailTimestamps.Peek();
                var waitUntil = oldestTimestamp.AddMinutes(1);
                var waitTime = waitUntil - now;

                _logger.LogInformation(
                    "Rate limit dostignut ({Count}/{Max} mejlova u minuti). Cekam {Seconds:F1} sekundi.",
                    _sentEmailTimestamps.Count, MaxEmailsPerMinute, waitTime.TotalSeconds);

                await Task.Delay(waitTime, cancellationToken);
            }
        }

        private async Task SendEmailAsync(SendEmailEvent emailEvent)
        {

            var outboxPath = Path.Combine(Directory.GetCurrentDirectory(), "outbox");
            Directory.CreateDirectory(outboxPath);

            var fileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_{emailEvent.MessageId}.txt";
            var filePath = Path.Combine(outboxPath, fileName);

            var content = $"""
                MessageId: {emailEvent.MessageId}
                To: {emailEvent.To}
                Subject: {emailEvent.Subject}
                SentAt: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
                
                {emailEvent.Body}
                """;

            await File.WriteAllTextAsync(filePath, content);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_channel != null) await _channel.DisposeAsync();
            if (_connection != null) await _connection.DisposeAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}