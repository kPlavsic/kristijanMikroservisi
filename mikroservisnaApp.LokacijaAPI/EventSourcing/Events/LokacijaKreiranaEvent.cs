namespace mikroservisnaApp.LokacijaAPI.EventSourcing.Events
{
    public class LokacijaKreiranaEvent : Event
    {
        public int LokacijaId { get; set; }
        public string Naziv { get; set; } = string.Empty;
        public string Adresa { get; set; } = string.Empty;
        public int Kapacitet { get; set; }
    }
}