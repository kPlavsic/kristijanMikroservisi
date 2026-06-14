using System.ComponentModel.DataAnnotations.Schema;

namespace mikroservisnaApp.Models
{
    public class Predavac
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }
        public string? Ime { get; set; }
        public string? Prezime { get; set; }
        public string? Titula { get; set; }
        public string? OblastStrucnosti { get; set; }

        public List<Angazovanje>? Angazovanja { get; set; }
    }
}
