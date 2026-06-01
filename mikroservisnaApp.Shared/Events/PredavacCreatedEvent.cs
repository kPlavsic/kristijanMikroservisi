namespace mikroservisnaApp.Shared.Events
{
    public class PredavacCreatedEvent
    {
        public string MessageId { get; set; } = string.Empty;
        public int PredavacId { get; set; }
        public string Ime { get; set; } = string.Empty;
        public string Prezime { get; set; } = string.Empty;
    }
}