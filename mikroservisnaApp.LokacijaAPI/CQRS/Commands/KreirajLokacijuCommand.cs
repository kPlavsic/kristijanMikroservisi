namespace mikroservisnaApp.LokacijaAPI.CQRS.Commands
{
    public class KreirajLokacijuCommand
    {
        public int Id { get; set; }
        public string Naziv { get; set; } = string.Empty;
        public string Adresa { get; set; } = string.Empty;
        public int Kapacitet { get; set; }
    }
}