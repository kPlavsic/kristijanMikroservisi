using Microsoft.EntityFrameworkCore;
using mikroservisnaApp.PredavacAPI.Models;

namespace mikroservisnaApp.PredavacAPI.Data
{
    public class PredavacDbContext : DbContext
    {
        public PredavacDbContext(DbContextOptions<PredavacDbContext> options)
            : base(options)
        {
        }

        public DbSet<Predavac> Predavaci { get; set; }
    }
}