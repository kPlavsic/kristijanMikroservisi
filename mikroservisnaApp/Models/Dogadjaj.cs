namespace mikroservisnaApp.Models
{
    public class Dogadjaj
    {
        public int Id { get; set; }
        public string? Naziv { get; set; }
        public string? Agenda { get; set; }
        public DateTime DatumIVreme { get; set; }
        public int Trajanje { get; set; }
        public decimal CenaKotizacije { get; set; }

        public int LokacijaId { get; set; }
        public Lokacija? Lokacija { get; set; }

        public int TipDogadjajaId { get; set; }
        public TipDogadjaja? TipDogadjaja { get; set; }

        public List<Angazovanje>? Angazovanja { get; set; }

    }
}
