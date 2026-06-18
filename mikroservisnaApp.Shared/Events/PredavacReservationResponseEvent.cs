namespace mikroservisnaApp.Shared.Events
{
    public class PredavacReservationResponseEvent
    {
        public string CorrelationId { get; set; } = string.Empty;
        public int PredavacId { get; set; }
        public bool Success { get; set; }
        public string? FailedReason { get; set; }
    }
}