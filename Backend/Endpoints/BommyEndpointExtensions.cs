using Backend.Endpoints.Internal;

namespace Backend.Endpoints;

public static class BommyEndpointExtensions
{
    public static IEndpointRouteBuilder MapBommyEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapDiagnosticsEndpoints();
        app.MapAuthEndpoints();
        app.MapPlayerEndpoints();
        app.MapShopEndpoints();
        app.MapMatchEndpoints();

        return app;
    }
}
