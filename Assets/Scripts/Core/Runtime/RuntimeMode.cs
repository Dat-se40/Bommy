using System;
using UnityEngine;

public static class RuntimeMode
{
    const int DefaultPort = 5000;
    static bool parsed;

    public static bool IsDedicatedServer { get; private set; }
    public static int Port { get; private set; } = DefaultPort;
    public static string MatchId { get; private set; }
    public static string BackendUrl { get; private set; }
    public static string EdgegapDeploymentId { get; private set; }

    public static void Parse()
    {
        if (parsed)
            return;

        parsed = true;

        string[] args = Environment.GetCommandLineArgs();

        IsDedicatedServer =
            HasFlag(args, "-server") ||
            HasFlag(args, "--server") ||
            HasFlag(args, "-dedicatedServer");

#if UNITY_SERVER
        IsDedicatedServer = true;
#endif

        Port = GetInt(args, "-port", GetEnvInt("SERVER_PORT", GetEnvInt("PORT", DefaultPort)));
        MatchId = GetString(args, "-matchId", GetEnv("MATCH_ID"));
        BackendUrl = GetString(args, "-backendUrl", GetEnv("BACKEND_URL"));
        EdgegapDeploymentId = GetString(
            args,
            "-edgegapDeploymentId",
            GetEnv("EDGEGAP_DEPLOYMENT_ID")
        );
    }

    public static string Describe()
    {
        Parse();

        return IsDedicatedServer
            ? $"server port={Port} matchId={MatchId ?? "-"} edgegap={EdgegapDeploymentId ?? "-"}"
            : "client";
    }

    static bool HasFlag(string[] args, string flag)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    static string GetString(string[] args, string flag, string fallback = null)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }

        return fallback;
    }

    static int GetInt(string[] args, string flag, int fallback)
    {
        string raw = GetString(args, flag);
        return int.TryParse(raw, out int value) ? value : fallback;
    }

    static string GetEnv(string name, string fallback = null)
    {
        string value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    static int GetEnvInt(string name, int fallback)
    {
        string value = Environment.GetEnvironmentVariable(name);
        return int.TryParse(value, out int parsedValue) ? parsedValue : fallback;
    }
}
