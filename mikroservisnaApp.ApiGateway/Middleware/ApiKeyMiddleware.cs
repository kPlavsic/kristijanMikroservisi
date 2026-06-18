namespace mikroservisnaApp.ApiGateway.Middleware;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;

    private const string ApiKeyHeaderName = "X-Api-Key";
    private const string ValidApiKey = "moja-tajna-sifra";

    public ApiKeyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;

            await context.Response.WriteAsync(
                "API key nije prosledjen.");

            return;
        }

        if (apiKey != ValidApiKey)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;

            await context.Response.WriteAsync(
                "Neispravan API key.");

            return;
        }

        await _next(context);
    }
}