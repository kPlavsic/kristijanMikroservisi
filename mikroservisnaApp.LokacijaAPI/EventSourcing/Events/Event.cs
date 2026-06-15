namespace mikroservisnaApp.LokacijaAPI.EventSourcing.Events
{
    public abstract class Event
    {
        protected Event()
        {
            ID = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
        }

        public Guid ID { get; }
        public DateTime CreatedAt { get; }
        public string EventType => GetType().Name;
    }
}