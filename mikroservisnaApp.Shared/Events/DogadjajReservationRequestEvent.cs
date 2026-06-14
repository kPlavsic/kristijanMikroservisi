namespace mikroservisnaApp.Shared.Events
{
    public class DogadjajReservationRequestEvent
    {
        public string CorrelationId { get; set; } = string.Empty;
        public int DogadjajId { get; set; }
        public DateTime Vreme { get; set; }
    }
}