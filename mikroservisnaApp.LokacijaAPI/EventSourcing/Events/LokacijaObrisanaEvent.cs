namespace mikroservisnaApp.LokacijaAPI.EventSourcing.Events
{
    public class LokacijaObrisanaEvent : Event
    {
        public int LokacijaId { get; set; }
    }
}