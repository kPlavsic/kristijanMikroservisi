namespace mikroservisnaApp.Models
{
    public class TipDogadjaja
    {
        public int Id { get; set; }
        public string Naziv { get; set; }

        public List<Dogadjaj> Dogadjaji { get; set; }
    }
}
