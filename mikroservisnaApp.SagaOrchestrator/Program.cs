using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using mikroservisnaApp.SagaOrchestrator.Data;
using mikroservisnaApp.SagaOrchestrator.Services;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddDbContext<SagaDbContext>(options =>
            options.UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=SagaOrchestratorDb;Trusted_Connection=True;MultipleActiveResultSets=true"));

        services.AddScoped<SagaOrchestrator>();

        services.AddHostedService<OutboxDispatcher>();
        services.AddHostedService<SagaConsumer>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SagaDbContext>();
    db.Database.Migrate();
}

await host.RunAsync();