using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn vào Canvas hoặc Panel cha — tự thêm ButtonClickSound cho mọi Button con.
/// Chạy muộn để không bị các script UI SetupButtons ghi đè onClick.
/// </summary>
[DefaultExecutionOrder(1000)]
public class AutoButtonSound : MonoBehaviour
{
    [SerializeField]
    private bool includeInactive = true;

    void Start()
    {
        RegisterChildButtons();
    }

    public void RegisterChildButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(includeInactive);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button btn = buttons[i];

            if (btn == null)
                continue;

            ButtonClickSound.EnsureOn(btn);
        }
    }
}
