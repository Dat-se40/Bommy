using Backend.Models;
using Backend.Services;

namespace Backend.Endpoints.Internal;

static class MatchEndpoints
{
    public static IEndpointRouteBuilder MapMatchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/matches/create", (
            CreateMatchRequest request,
            HttpRequest httpRequest,
            BommyState state) =>
        {
            if (!RequestAuth.TryGetBearerToken(httpRequest, out string accessToken) ||
                !state.TryGetAccount(accessToken, out _))
            {
                return Results.Unauthorized();
            }

            CreateMatchResponse response = state.CreateMatch(request);
            return Results.Ok(response);
        })
        .WithName("CreateMatch")
        .WithTags("Matches");

        app.MapPost("/v1/matches/{matchId}/server-ready", (
            string matchId,
            ServerReadyRequest request,
            BommyState state) =>
        {
            if (string.IsNullOrWhiteSpace(matchId))
                return Results.BadRequest(new ErrorResponse("matchId is required"));

            MatchServerAllocation allocation = state.MarkServerReady(matchId.Trim(), request);
            return Results.Ok(new ServerReadyResponse(true, allocation));
        })
        .WithName("RegisterServerReady")
        .WithTags("Matches");

        app.MapPost("/v1/matches/{matchId}/validate-player", (
            string matchId,
            MatchJoinPayload payload,
            BommyState state) =>
        {
            MatchJoinValidationResult result = state.ValidatePlayerJoin(matchId, payload);
            return Results.Ok(result);
        })
        .WithName("ValidatePlayerJoin")
        .WithTags("Matches");

        return app;
    }
}
