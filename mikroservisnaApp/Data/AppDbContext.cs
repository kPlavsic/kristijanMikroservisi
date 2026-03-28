using Microsoft.EntityFrameworkCore;
using mikroservisnaApp.Models;

namespace mikroservisnaApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Dogadjaj> Dogadjaji { get; set; }
        public DbSet<Predavac> Predavaci { get; set; }
        public DbSet<Lokacija> Lokacije { get; set; }
        public DbSet<TipDogadjaja> TipoviDogadjaja { get; set; }
        public DbSet<Angazovanje> Angazovanja { get; set; }
    }
}