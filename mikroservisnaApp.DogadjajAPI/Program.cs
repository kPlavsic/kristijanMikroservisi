using Microsoft.EntityFrameworkCore;
using mikroservisnaApp.DogadjajAPI.Data;
using mikroservisnaApp.DogadjajAPI.HostedServices;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();


builder.Services.AddDbContext<DogadjajDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHostedService<RabbitMqConsumerHostedService>();


var app = builder.Build();

app.UseAuthorization();
app.MapControllers();
app.Run();