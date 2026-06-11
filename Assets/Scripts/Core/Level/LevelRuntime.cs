using UnityEngine;

public static class LevelRuntime
{
    public static LevelConfig Current { get; private set; }

    public static void SetLevel(LevelConfig config)
    {
        Current = config;

        if (config != null)
            Debug.Log($"Level loaded: {config.displayName} ({config.levelId})");
    }

    public static void Clear()
    {
        Current = null;
    }
}
