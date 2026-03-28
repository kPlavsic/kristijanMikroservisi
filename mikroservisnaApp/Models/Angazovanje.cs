namespace mikroservisnaApp.Models
{
    public class Angazovanje
    {
        public int Id { get; set; }
        public string NazivPredavanja { get; set; }
        public DateTime Vreme { get; set; }

        public int PredavacId { get; set; }
        public Predavac Predavac { get; set; }

        public int DogadjajId { get; set; }
        public Dogadjaj Dogadjaj { get; set; }
    }
}
