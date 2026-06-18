namespace mikroservisnaApp.Shared.Events
{
    public class AngazovanjeKreiranoEvent
    {
        public string CorrelationId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? FailedReason { get; set; }
    }
}