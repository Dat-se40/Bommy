using UnityEngine;

/// <summary>
/// Data sheet mẫu 1 người chơi khi chưa có BE.
/// </summary>
[CreateAssetMenu(
    fileName = "SamplePlayerDataSheet",
    menuName = "Bommy/Session/Sample Player Data Sheet"
)]
public class SamplePlayerDataSheet : ScriptableObject
{
    public PlayerAccountSnapshot account = new();
    public PlayerMatchProfile defaultLocalProfile;
}
