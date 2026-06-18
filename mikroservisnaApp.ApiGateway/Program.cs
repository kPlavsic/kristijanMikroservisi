using mikroservisnaApp.ApiGateway.Middleware;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddOcelot();

var app = builder.Build();

//app.UseMiddleware<ApiKeyMiddleware>();

app.UseMiddleware<RequestLoggingMiddleware>();

app.UseMiddleware<RequestTransformationMiddleware>();

await app.UseOcelot();

app.Run();