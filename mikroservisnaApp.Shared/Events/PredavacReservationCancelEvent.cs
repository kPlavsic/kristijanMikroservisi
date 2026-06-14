namespace mikroservisnaApp.Shared.Events
{
    public class PredavacReservationCancelEvent
    {
        public string CorrelationId { get; set; } = string.Empty;
        public int PredavacId { get; set; }
    }
}