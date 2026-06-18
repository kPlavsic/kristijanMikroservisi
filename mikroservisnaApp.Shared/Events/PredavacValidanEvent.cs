namespace mikroservisnaApp.Shared.Events
{
    public class PredavacValidanEvent
    {
        public string SagaId { get; set; } = string.Empty;
        public string Naziv { get; set; } = string.Empty;
        public string Agenda { get; set; } = string.Empty;
        public DateTime DatumIVreme { get; set; }
        public int Trajanje { get; set; }
        public decimal CenaKotizacije { get; set; }
        public int LokacijaId { get; set; }
        public int TipDogadjajaId { get; set; }
        public bool Success { get; set; }
        public string? FailedReason { get; set; }
    }
}