namespace mikroservisnaApp.SagaOrchestrator.Entities
{
    public enum OutboxStatus
    {
        ForProcessing,
        Processed
    }

    public class OutboxMessage
    {
        public long Id { get; set; }
        public Guid CorrelationId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public OutboxStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}