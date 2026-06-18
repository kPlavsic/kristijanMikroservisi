namespace mikroservisnaApp.LokacijaAPI.EventSourcing.Events
{
    public class KapacitetPromenjenEvent : Event
    {
        public int LokacijaId { get; set; }
        public int StariKapacitet { get; set; }
        public int NoviKapacitet { get; set; }
    }
}