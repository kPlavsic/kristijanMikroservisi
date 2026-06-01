namespace mikroservisnaApp.DogadjajAPI.Entities
{
    public class ProcessedMessage
    {
        public long Id { get; set; }
        public string EventId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public DateTime ProcessedAtUtc { get; set; }
    }
}