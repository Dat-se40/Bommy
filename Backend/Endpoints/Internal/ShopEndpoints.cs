using Backend.Models;
using Backend.Services;

namespace Backend.Endpoints.Internal;

static class ShopEndpoints
{
    public static IEndpointRouteBuilder MapShopEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/shop/purchase", (
            PurchaseCharacterRequest request,
            HttpRequest httpRequest,
            BommyState state) =>
        {
            if (!RequestAuth.TryGetBearerToken(httpRequest, out string accessToken) ||
                !state.TryPurchaseCharacter(accessToken, request.CharacterId, out PlayerAccountSnapshot? account))
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new PurchaseCharacterResponse(true, account, null));
        })
        .WithName("PurchaseCharacter")
        .WithTags("Shop");

        return app;
    }
}
