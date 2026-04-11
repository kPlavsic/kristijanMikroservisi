using Microsoft.EntityFrameworkCore;
using mikroservisnaApp.Messaging;
using mikroservisnaApp.Patterns;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("PredavacAPI", (client) =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("PredavacAPIEndpoint")!);
});

// Circuit Breaker kao Singleton
builder.Services.AddSingleton<CircuitBreaker>(sp =>
    new CircuitBreaker(3, TimeSpan.FromSeconds(10))
);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<mikroservisnaApp.Data.AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<MessageProducer>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
