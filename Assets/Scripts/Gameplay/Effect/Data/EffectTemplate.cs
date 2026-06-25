using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Effect")]
public class EffectTemplate : ScriptableObject
{
    public int effectId;

    public string displayName;

    public EffectType effectType;

    public float duration;

    public float specialValue;

    public string description; 

    [Range(0f, 1f)]
    public float rating;

    public GameObject pickupPrefab;

    public GameObject vfxPrefab;

    [Header("UI")]
    public Sprite uiIcon;

    [TextArea]
    public string mapInfoDescription;

    [Header("Special Bomb")]
    public GameObject placedBombPrefab; //Prefab bomb đặc biệt được đặt xuống khi effect đang active.
    public float fuseSeconds = 2f; //Thời gian chờ trước khi bomb nổ.
    public int damage = 1; //Số mạng trừ khi người chơi dính vụ nổ.
    public bool ignoreOwner = true; //Có bỏ qua người đặt trap không.

}