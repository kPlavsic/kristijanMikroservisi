namespace mikroservisnaApp.Shared.Events
{
    public class AngazovanjeRequestedEvent
    {
        public string CorrelationId { get; set; } = string.Empty;
        public int PredavacId { get; set; }
        public int DogadjajId { get; set; }
        public string NazivPredavanja { get; set; } = string.Empty;
        public DateTime Vreme { get; set; }
    }
}