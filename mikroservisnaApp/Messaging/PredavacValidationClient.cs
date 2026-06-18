using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using mikroservisnaApp.Shared.Events;

namespace mikroservisnaApp.Messaging
{
    public class PredavacValidationClient : IAsyncDisposable
    {
        private const string RequestQueue = "predavac.validation.request";
        private const string ReplyQueue = "predavac.validation.reply";

        private IConnection? _connection;
        private IChannel? _publishChannel;
        private IChannel? _consumerChannel;

        
        private readonly ConcurrentDictionary<string, TaskCompletionSource<PredavacValidationResponse>> _pendingRequests = new();

        public async Task InitializeAsync()
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest"
            };

            _connection = await factory.CreateConnectionAsync();
            _publishChannel = await _connection.CreateChannelAsync();
            _consumerChannel = await _connection.CreateChannelAsync();

            
            await _publishChannel.QueueDeclareAsync(RequestQueue, durable: false, exclusive: false, autoDelete: false);
            await _consumerChannel.QueueDeclareAsync(ReplyQueue, durable: false, exclusive: false, autoDelete: false);

            
            var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
            consumer.ReceivedAsync += HandleReplyAsync;
            await _consumerChannel.BasicConsumeAsync(queue: ReplyQueue, autoAck: true, consumer: consumer);
        }

        public async Task<PredavacValidationResponse> ValidateAsync(int predavacId, TimeSpan? timeout = null)
        {
            if (_publishChannel is null)
                await InitializeAsync();

            var correlationId = Guid.NewGuid().ToString("N");


            var tcs = new TaskCompletionSource<PredavacValidationResponse>();
            _pendingRequests[correlationId] = tcs;

            var request = new PredavacValidationRequest
            {
                CorrelationId = correlationId,
                PredavacId = predavacId,
                ReplyTo = ReplyQueue
            };

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));

            var properties = new BasicProperties
            {
                CorrelationId = correlationId,
                ReplyTo = ReplyQueue
            };

            await _publishChannel!.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: RequestQueue,
                mandatory: false,
                basicProperties: properties,
                body: body);

            
            var timeoutTask = Task.Delay(timeout ?? TimeSpan.FromSeconds(10));
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                _pendingRequests.TryRemove(correlationId, out _);
                return new PredavacValidationResponse
                {
                    CorrelationId = correlationId,
                    PredavacId = predavacId,
                    Exists = false
                };
            }

            return await tcs.Task;
        }

        private async Task HandleReplyAsync(object sender, BasicDeliverEventArgs ea)
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            var response = JsonSerializer.Deserialize<PredavacValidationResponse>(body);

            if (response is null) return;

            if (_pendingRequests.TryRemove(response.CorrelationId, out var tcs))
            {
                tcs.SetResult(response);
            }

            await Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            if (_consumerChannel != null) await _consumerChannel.DisposeAsync();
            if (_publishChannel != null) await _publishChannel.DisposeAsync();
            if (_connection != null) await _connection.DisposeAsync();
        }
    }
}