using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn vào Canvas hoặc Panel cha — tự động thêm sound cho tất cả Button con.
/// </summary>
public class AutoButtonSound : MonoBehaviour
{
    void Start()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);

        foreach (Button btn in buttons)
        {
            btn.onClick.AddListener(() =>
                SoundPlayback.PlayLocal(SoundKey.SfxClick));
        }
    }
}