namespace mikroservisnaApp.Shared.Events
{
    public class DogadjajReservationResponseEvent
    {
        public string CorrelationId { get; set; } = string.Empty;
        public int DogadjajId { get; set; }
        public bool Success { get; set; }
        public string? FailedReason { get; set; }
    }
}