namespace mikroservisnaApp.LokacijaAPI.EventSourcing.Models
{
    public abstract class AggregateSnapshot
    {
        public int ID { get; set; }
        public int Version { get; set; }
    }
}