/// <summary>
/// API gọi âm thanh có phân loại Local / Synced.
/// Cả hai đều phát qua SoundManager trên máy hiện tại — khác nhau ở <b>chỗ gọi</b> trong code.
/// </summary>
public static class SoundPlayback
{
    /// <summary>
    /// Âm thanh cá nhân — chỉ gọi khi <c>isOwner</c> hoặc UI local.
    /// Ví dụ: nhặt buff, bị trúng đạn (chỉ mình nghe).
    /// </summary>
    public static void PlayLocal(string key)
    {
        if (SoundManager.Instance == null)
            return;

        SoundManager.Instance.PlaySfx(key);
    }

    /// <summary>
    /// Âm thanh đồng bộ — gọi trong handler replicate chạy trên <b>mọi client</b>
    /// (SyncVar.onChanged, SyncList.onChanged, MatchFinishedChanged, …).
    /// Ví dụ: nổ bom, đặt bom (khi mọi client thấy object), game over.
    /// </summary>
    public static void PlaySynced(string key)
    {
        if (SoundManager.Instance == null)
            return;

        SoundManager.Instance.PlaySfx(key);
    }

    public static void PlayBgm(string key, bool restartIfSame = false)
    {
        if (SoundManager.Instance == null)
            return;

        SoundManager.Instance.PlayBgm(key, restartIfSame);
    }

    public static void StopBgm()
    {
        if (SoundManager.Instance == null)
            return;

        SoundManager.Instance.StopBgm();
    }
}
