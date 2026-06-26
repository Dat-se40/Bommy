using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Gắn trực tiếp lên Button — phát click qua pointer event, không phụ thuộc onClick
/// (tránh bị UI khác RemoveAllListeners xóa mất).
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonClickSound : MonoBehaviour, IPointerClickHandler
{
    [SerializeField]
    private string soundKey = SoundKey.SfxClick;

    Button button;

    void Awake()
    {
        button = GetComponent<Button>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isActiveAndEnabled || button == null || !button.interactable)
            return;

        SoundPlayback.PlayLocal(soundKey);
    }

    public static void EnsureOn(Button target)
    {
        if (target == null || target.GetComponent<ButtonClickSound>() != null)
            return;

        target.gameObject.AddComponent<ButtonClickSound>();
    }
}
