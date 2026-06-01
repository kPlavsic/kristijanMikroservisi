using Microsoft.EntityFrameworkCore;
using mikroservisnaApp.DogadjajAPI.Entities;

namespace mikroservisnaApp.DogadjajAPI.Data
{
    public class DogadjajDbContext : DbContext
    {
        public DogadjajDbContext(DbContextOptions<DogadjajDbContext> options)
            : base(options)
        {
        }

        public DbSet<PredavacReference> PredavacReference { get; set; }
        public DbSet<ProcessedMessage> ProcessedMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PredavacReference>()
                .Property(p => p.Id)
                .ValueGeneratedNever(); // Id dolazi izvana, ne generišemo ga
        }
    }
}