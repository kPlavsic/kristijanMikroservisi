namespace mikroservisnaApp.LokacijaAPI.EventSourcing.Models
{
    public class LokacijaSnapshot : AggregateSnapshot
    {
        public string Naziv { get; set; } = string.Empty;
        public string Adresa { get; set; } = string.Empty;
        public int Kapacitet { get; set; }
        public bool Obrisana { get; set; }
    }
}