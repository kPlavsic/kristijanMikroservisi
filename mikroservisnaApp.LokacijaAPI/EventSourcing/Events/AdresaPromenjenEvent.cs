namespace mikroservisnaApp.LokacijaAPI.EventSourcing.Events
{
    public class AdresaPromenjenEvent : Event
    {
        public int LokacijaId { get; set; }
        public string StaraAdresa { get; set; } = string.Empty;
        public string NovaAdresa { get; set; } = string.Empty;
    }
}