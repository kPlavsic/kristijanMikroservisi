namespace mikroservisnaApp.Shared.Events
{
    public class PredavacReservationRequestEvent
    {
        public string CorrelationId { get; set; } = string.Empty;
        public int PredavacId { get; set; }
        public DateTime Vreme { get; set; }
    }
}