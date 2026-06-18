namespace mikroservisnaApp.Shared.Events
{
    public class DogadjajReservationCancelEvent
    {
        public string CorrelationId { get; set; } = string.Empty;
        public int DogadjajId { get; set; }
    }
}