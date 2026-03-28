namespace mikroservisnaApp.Models
{
    public class Lokacija
    {
        public int Id { get; set; }

        public string? Naziv { get; set; }

        public string? Adresa { get; set; }

        public int Kapacitet { get; set; }

        public List<Dogadjaj>? Dogadjaji { get; set; }
    }
}
