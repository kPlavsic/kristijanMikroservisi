using Microsoft.EntityFrameworkCore;

namespace mikroservisnaApp.LokacijaAPI.Data
{
    public class LokacijaDbContext : DbContext
    {
        public LokacijaDbContext(DbContextOptions<LokacijaDbContext> options)
            : base(options)
        {
        }

        public DbSet<StoredEvent> Events { get; set; }
        public DbSet<StoredSnapshot> Snapshots { get; set; }
        public DbSet<LokacijaReadModel> LokacijeReadModel { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LokacijaReadModel>()
                .Property(l => l.Id)
                .ValueGeneratedNever();
        }
    }
}