namespace mikroservisnaApp.LokacijaAPI.CQRS.Commands
{
    public class IzmeniLokacijuCommand
    {
        public int Id { get; set; }
        public string? NoviNaziv { get; set; }
        public string? NovaAdresa { get; set; }
        public int? NoviKapacitet { get; set; }
    }
}