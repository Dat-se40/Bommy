namespace Backend.Endpoints.Internal;

static class RequestAuth
{
    public static bool TryGetBearerToken(HttpRequest request, out string accessToken)
    {
        const string prefix = "Bearer ";
        string? header = request.Headers.Authorization;

        if (!string.IsNullOrWhiteSpace(header) &&
            header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            accessToken = header[prefix.Length..].Trim();
            return accessToken.Length > 0;
        }

        accessToken = string.Empty;
        return false;
    }
}
