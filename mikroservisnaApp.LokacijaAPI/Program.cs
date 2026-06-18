using Microsoft.EntityFrameworkCore;
using mikroservisnaApp.LokacijaAPI.CQRS.Commands;
using mikroservisnaApp.LokacijaAPI.CQRS.Interfaces;
using mikroservisnaApp.LokacijaAPI.CQRS.Queries;
using mikroservisnaApp.LokacijaAPI.CQRS.ReadModels;
using mikroservisnaApp.LokacijaAPI.Data;
using mikroservisnaApp.LokacijaAPI.EventSourcing.Store;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Baza podataka
builder.Services.AddDbContext<LokacijaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Event Store
builder.Services.AddScoped<EventStore>();

// Command Handlers
builder.Services.AddScoped<ICommandHandler<KreirajLokacijuCommand, int>, KreirajLokacijuCommandHandler>();
builder.Services.AddScoped<ICommandHandler<IzmeniLokacijuCommand, bool>, IzmeniLokacijuCommandHandler>();
builder.Services.AddScoped<ICommandHandler<ObrisiLokacijuCommand, bool>, ObrisiLokacijuCommandHandler>();

// Query Handlers
builder.Services.AddScoped<IQueryHandler<GetSveLokacijeQuery, List<LokacijaListItem>>, GetSveLokacijeQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetLokacijaDetaljiQuery, LokacijaDetalji?>, GetLokacijaDetaljiQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetLokacijePoKapacitetuQuery, List<LokacijaListItem>>, GetLokacijePoKapacitetuQueryHandler>();

builder.Services.AddHostedService<mikroservisnaApp.LokacijaAPI.Messaging.SagaKoreografijaConsumer>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Automatski kreira tabele pri pokretanju
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LokacijaDbContext>();
    db.Database.EnsureCreated();
}

app.Run();