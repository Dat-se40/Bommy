using Backend.Models;
using Backend.Services;

namespace Backend.Endpoints.Internal;

static class PlayerEndpoints
{
    public static IEndpointRouteBuilder MapPlayerEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/players/me", (HttpRequest request, BommyState state) =>
        {
            if (!RequestAuth.TryGetBearerToken(request, out string accessToken))
                return Results.Unauthorized();

            return state.TryGetAccount(accessToken, out PlayerAccountSnapshot? account)
                ? Results.Ok(account)
                : Results.Unauthorized();
        })
        .WithName("GetCurrentPlayer")
        .WithTags("Players");

        return app;
    }
}
