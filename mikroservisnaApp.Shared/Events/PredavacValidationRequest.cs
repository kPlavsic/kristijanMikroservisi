namespace mikroservisnaApp.Shared.Events
{
    public class PredavacValidationRequest
    {
        public string CorrelationId { get; set; } = string.Empty;
        public int PredavacId { get; set; }
        public string ReplyTo { get; set; } = string.Empty;
    }
}