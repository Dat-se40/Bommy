namespace Backend.Options;

public sealed class BommyBackendOptions
{
    public string LocalHost { get; init; } = "127.0.0.1";
    public int DefaultGameServerPort { get; init; } = 5000;
}
