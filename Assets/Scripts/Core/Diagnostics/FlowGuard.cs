using UnityEngine;

/// <summary>
/// Guard + log chuẩn cho các flow scene/network.
/// Các hàm validate pure có thể dùng trong EditMode test.
/// </summary>
public static class FlowGuard
{
    public const string TagSetup = "FLOW:SETUP";
    public const string TagNetwork = "FLOW:NETWORK";
    public const string TagHud = "FLOW:HUD";
    public const string TagGameplay = "FLOW:GAMEPLAY";

    public static bool Require(
        bool condition,
        string tag,
        string message,
        Object context = null
    )
    {
        if (condition)
            return true;

        Debug.LogWarning(Format(tag, message), context);
        return false;
    }

    public static bool RequireNotNull<T>(
        T value,
        string tag,
        string name,
        Object context = null
    ) where T : class
    {
        return Require(value != null, tag, name + " is null.", context);
    }

    public static void Info(string tag, string message, Object context = null)
    {
        Debug.Log(Format(tag, message), context);
    }

    public static void Error(string tag, string message, Object context = null)
    {
        Debug.LogError(Format(tag, message), context);
    }

    public static string Format(string tag, string message)
    {
        return "[" + tag + "] " + message;
    }

    /// <summary>
    /// Validate profile trước khi commit hoặc spawn player.
    /// </summary>
    public static bool IsValidSpawnProfile(PlayerMatchProfile profile, out string reason)
    {
        if (profile.characterId <= 0)
        {
            reason = "characterId must be > 0";
            return false;
        }

        if (profile.catalogIndex < 0)
        {
            reason = "catalogIndex must be >= 0";
            return false;
        }

        if (string.IsNullOrWhiteSpace(profile.displayName))
        {
            reason = "displayName is empty";
            return false;
        }

        reason = null;
        return true;
    }

    /// <summary>
    /// Validate slot index cho board HUD.
    /// </summary>
    public static bool IsValidSlotIndex(int slotIndex, int slotCount, out string reason)
    {
        if (slotCount <= 0)
        {
            reason = "slotCount must be > 0";
            return false;
        }

        if (slotIndex < 0 || slotIndex >= slotCount)
        {
            reason = "slotIndex " + slotIndex + " out of range [0," + (slotCount - 1) + "]";
            return false;
        }

        reason = null;
        return true;
    }
}
