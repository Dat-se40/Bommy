public readonly struct AuthResult
{
    public bool Success { get; }
    public string Error { get; }

    private AuthResult(bool success, string error)
    {
        Success = success;
        Error = error;
    }

    public static AuthResult Ok() => new(true, null);
    public static AuthResult Fail(string msg) => new(false, msg);
}