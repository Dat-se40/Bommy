using Nakama;
using System;

internal static class AuthErrorMapper
{
    public static string Login(ApiResponseException exception)
    {
        return exception.StatusCode switch
        {
            401 => "Email or password is incorrect.",
            404 => "No account found for that email.",
            _ => Map(exception)
        };
    }

    public static string Register(ApiResponseException exception)
    {
        return exception.StatusCode == 409
            ? "That email is already in use."
            : Map(exception);
    }

    public static string Map(Exception exception)
    {
        string message = exception?.Message ?? string.Empty;
        string lower = message.ToLowerInvariant();

        if (lower.Contains("refused") ||
            lower.Contains("connectfailure") ||
            lower.Contains("sending the request") ||
            lower.Contains("timeout"))
            return "Cannot reach the server. Check your connection and try again.";

        return message.Length <= 180 ? message : message[..180] + "...";
    }
}
