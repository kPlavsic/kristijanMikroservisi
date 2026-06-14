namespace mikroservisnaApp.Shared.Events
{
    public class PredavacValidationResponse
    {
        public string CorrelationId { get; set; } = string.Empty;
        public int PredavacId { get; set; }
        public bool Exists { get; set; }
    }
}