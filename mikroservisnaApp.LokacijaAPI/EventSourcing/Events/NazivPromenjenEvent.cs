namespace mikroservisnaApp.LokacijaAPI.EventSourcing.Events
{
    public class NazivPromenjenEvent : Event
    {
        public int LokacijaId { get; set; }
        public string StariNaziv { get; set; } = string.Empty;
        public string NoviNaziv { get; set; } = string.Empty;
    }
}