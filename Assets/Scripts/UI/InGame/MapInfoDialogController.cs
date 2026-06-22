using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hiển thị thông tin item/buff của map hiện tại trước khi trận bắt đầu.
/// Dữ liệu được lấy từ LevelRuntime.Current.destructibleDrops.
/// </summary>
public class MapInfoDialogController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject overlayRoot;

    [Header("Buttons")]
    [SerializeField] private Button closebtn;

    [Header("Labels")]
    [SerializeField] private TMP_Text mapInfoTitlelbl;
    [SerializeField] private TMP_Text mapNamelbl;

    [Header("Items")]
    [SerializeField] private Transform itemList;
    [SerializeField] private MapInfoItemCardUI itemCardTemplate;
    [SerializeField] private int maxCards = 8;

    [Header("Fallback")]
    [SerializeField] private Sprite fallbackIcon;
    [SerializeField] private string emptyText = "No item data";

    private readonly List<MapInfoItemCardUI> spawnedCards = new();

    private void Awake()
    {
        if (closebtn != null)
        {
            closebtn.onClick.RemoveAllListeners();
            closebtn.onClick.AddListener(Close);
        }

        if (itemCardTemplate != null)
            itemCardTemplate.gameObject.SetActive(false);

        if (overlayRoot != null)
            overlayRoot.SetActive(false);
    }

    public void OpenFromCurrentLevel()
    {
        LevelConfig level = LevelRuntime.Current;

        if (level == null)
        {
            OpenEmpty();
            return;
        }

        if (overlayRoot != null)
        {
            overlayRoot.SetActive(true);
            overlayRoot.transform.SetAsLastSibling();
        }

        if (mapInfoTitlelbl != null)
            mapInfoTitlelbl.text = "MAP INFO";

        if (mapNamelbl != null)
            mapNamelbl.text = level.displayName;

        RenderItems(level);
    }

    public void Close()
    {
        if (overlayRoot != null)
            overlayRoot.SetActive(false);
    }

    private void RenderItems(LevelConfig level)
    {
        ClearCards();

        if (itemList == null || itemCardTemplate == null)
            return;

        DropEntry[] entries = level.destructibleDrops.entries;

        if (entries == null || entries.Length == 0)
        {
            SpawnEmptyCard();
            return;
        }

        HashSet<int> usedEffectIds = new();
        int count = 0;

        for (int i = 0; i < entries.Length; i++)
        {
            DropEntry entry = entries[i];
            EffectTemplate effect = entry.effect;

            if (effect == null)
                continue;

            // Tránh hiện trùng cùng một effect nếu config có duplicate.
            if (!usedEffectIds.Add(effect.effectId))
                continue;

            SpawnEffectCard(effect, entry.maxSpawnCount);
            count++;

            if (count >= maxCards)
                break;
        }

        if (count == 0)
            SpawnEmptyCard();
    }

    private void SpawnEffectCard(EffectTemplate effect, int maxSpawnCount)
    {
        MapInfoItemCardUI card = Instantiate(itemCardTemplate, itemList);
        card.gameObject.SetActive(true);

        string description = !string.IsNullOrWhiteSpace(effect.mapInfoDescription)
            ? effect.mapInfoDescription
            : effect.description;

        if (maxSpawnCount > 0)
        {
            if (string.IsNullOrWhiteSpace(description))
                description = $"Max spawn on map: {maxSpawnCount}";
            else
                description += $"\nMax spawn on map: {maxSpawnCount}";
        }

        card.Setup(
            effect.uiIcon != null ? effect.uiIcon : ResolveFallbackIcon(effect),
            effect.displayName,
            description
        );

        spawnedCards.Add(card);
    }

    private Sprite ResolveFallbackIcon(EffectTemplate effect)
    {
        if (effect == null || effect.pickupPrefab == null)
            return fallbackIcon;

        SpriteRenderer spriteRenderer = effect.pickupPrefab.GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null && spriteRenderer.sprite != null)
            return spriteRenderer.sprite;

        return fallbackIcon;
    }

    private void SpawnEmptyCard()
    {
        MapInfoItemCardUI card = Instantiate(itemCardTemplate, itemList);
        card.gameObject.SetActive(true);
        card.Setup(fallbackIcon, "ITEMS", emptyText);
        spawnedCards.Add(card);
    }

    private void OpenEmpty()
    {
        if (overlayRoot != null)
        {
            overlayRoot.SetActive(true);
            overlayRoot.transform.SetAsLastSibling();
        }

        if (mapInfoTitlelbl != null)
            mapInfoTitlelbl.text = "MAP INFO";

        if (mapNamelbl != null)
            mapNamelbl.text = "Unknown Map";

        ClearCards();
        SpawnEmptyCard();
    }

    private void ClearCards()
    {
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            if (spawnedCards[i] != null)
                Destroy(spawnedCards[i].gameObject);
        }

        spawnedCards.Clear();
    }
}
