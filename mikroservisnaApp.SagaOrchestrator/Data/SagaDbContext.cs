using Microsoft.EntityFrameworkCore;
using mikroservisnaApp.SagaOrchestrator.Entities;

namespace mikroservisnaApp.SagaOrchestrator.Data
{
    public class SagaDbContext : DbContext
    {
        public SagaDbContext(DbContextOptions<SagaDbContext> options) : base(options)
        {
        }

        public DbSet<AngazovanjeSagaState> SagaStates { get; set; }
        public DbSet<OutboxMessage> OutboxMessages { get; set; }
    }
}