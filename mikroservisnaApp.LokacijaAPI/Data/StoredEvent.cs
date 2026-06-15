namespace mikroservisnaApp.LokacijaAPI.Data
{
    public class StoredEvent
    {
        public int Id { get; set; }
        public int AggregateId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string EventData { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int Version { get; set; }
    }
}