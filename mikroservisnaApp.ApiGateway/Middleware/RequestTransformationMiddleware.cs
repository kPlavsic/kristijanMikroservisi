namespace mikroservisnaApp.ApiGateway.Middleware;

public class RequestTransformationMiddleware
{
    private readonly RequestDelegate _next;

    public RequestTransformationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        context.Request.Headers["X-Gateway"] =
            "mikroservisnaAppGateway";
        Console.WriteLine(
            $"Dodat header X-Gateway");

        await _next(context);
    }
}