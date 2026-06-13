using Backend.Models;
using Backend.Services;

namespace Backend.Endpoints.Internal;

static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/auth/dev", (AuthDevRequest request, BommyState state) =>
        {
            if (string.IsNullOrWhiteSpace(request.DevId))
                return Results.BadRequest(new ErrorResponse("devId is required"));

            AuthResponse response = state.AuthenticateDev(
                request.DevId.Trim(),
                string.IsNullOrWhiteSpace(request.DisplayName)
                    ? request.DevId.Trim()
                    : request.DisplayName.Trim()
            );

            return Results.Ok(response);
        })
        .WithName("AuthenticateDev")
        .WithTags("Auth");

        app.MapPost("/v1/auth/steam", (AuthSteamRequest request, BommyState state) =>
        {
            if (string.IsNullOrWhiteSpace(request.SteamTicket))
                return Results.BadRequest(new ErrorResponse("steamTicket is required"));

            AuthResponse response = state.AuthenticateSteamTicket(request.SteamTicket.Trim());
            return Results.Ok(response);
        })
        .WithName("AuthenticateSteam")
        .WithTags("Auth");

        return app;
    }
}
