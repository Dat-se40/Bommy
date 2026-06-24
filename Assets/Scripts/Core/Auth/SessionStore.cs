using Nakama;
using UnityEngine;

public static class SessionStore
{
    private const string KeyAuthToken = "NK_AUTH_TOKEN";
    private const string KeyRefreshToken = "NK_REFRESH_TOKEN";
    private const string KeyUserId = "NK_USER_ID";

    public static void Save(string authToken, string refreshToken, string userId)
    {
        PlayerPrefs.SetString(KeyAuthToken, authToken);
        PlayerPrefs.SetString(KeyRefreshToken, refreshToken);
        PlayerPrefs.SetString(KeyUserId, userId);
        PlayerPrefs.Save();
    }

    public static void Save(ISession session)
    {
        Save(session.AuthToken, session.RefreshToken, session.UserId);
    }

    public static (string authToken, string refreshToken, string userId) Load()
    {
        return (
            PlayerPrefs.GetString(KeyAuthToken, null),
            PlayerPrefs.GetString(KeyRefreshToken, null),
            PlayerPrefs.GetString(KeyUserId, null)
        );
    }

    public static bool HasSession()
    {
        var (auth, refresh, id) = Load();
        return !string.IsNullOrEmpty(auth)
            && !string.IsNullOrEmpty(refresh)
            && !string.IsNullOrEmpty(id);
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(KeyAuthToken);
        PlayerPrefs.DeleteKey(KeyRefreshToken);
        PlayerPrefs.DeleteKey(KeyUserId);
        PlayerPrefs.Save();
    }
}