namespace mikroservisnaApp.LokacijaAPI.Data
{
    public class LokacijaReadModel
    {
        public int Id { get; set; }
        public string Naziv { get; set; } = string.Empty;
        public string Adresa { get; set; } = string.Empty;
        public int Kapacitet { get; set; }
        public bool Obrisana { get; set; }
    }
}