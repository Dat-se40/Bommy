namespace Backend.Endpoints.Internal;

static class DiagnosticsEndpoints
{
    public static IEndpointRouteBuilder MapDiagnosticsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new
        {
            status = "ok",
            service = "bommy-backend",
            utc = DateTimeOffset.UtcNow
        }))
        .WithName("Health")
        .WithTags("Diagnostics");

        return app;
    }
}
