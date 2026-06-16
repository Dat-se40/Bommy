/// <summary>
/// Phạm vi phát âm thanh trong multiplayer.
/// Không ảnh hưởng runtime — dùng làm quy ước + tài liệu.
/// </summary>
public enum SoundScope
{
    /// <summary>Chỉ client hiện tại (isOwner, UI, feedback cá nhân).</summary>
    Local,

    /// <summary>Mọi client cùng nghe — trigger từ SyncVar / SyncList / event replicate.</summary>
    Synced,
}
