namespace mikroservisnaApp.SagaOrchestrator.Entities
{
    public enum SagaStatus
    {
        Started,
        PredavacRezervisаn,
        DogadjajPotvrden,
        Completed,
        Failed,

        // Kompenzacija
        CancelPredavacReservation,
        CancelDogadjajReservation
    }

    public class AngazovanjeSagaState
    {
        public int Id { get; set; }
        public Guid CorrelationId { get; set; }

        public int PredavacId { get; set; }
        public int DogadjajId { get; set; }
        public string NazivPredavanja { get; set; } = string.Empty;
        public DateTime Vreme { get; set; }

        public SagaStatus Status { get; set; }
        public string? FailedReason { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}