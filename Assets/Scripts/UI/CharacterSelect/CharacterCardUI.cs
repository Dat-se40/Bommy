using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterCardUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image cardImage;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text characterNamelbl;
    [SerializeField] private TMP_Text pricelbl;
    [SerializeField] private TMP_Text requiredLevellbl;
    [SerializeField] private TMP_Text statuslbl;
    [SerializeField] private GameObject lockOverlay;

    [Header("Card Sprites")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite selectedSprite;

    Button button;
    CharacterSelectShopController owner;
    int index;

    void Awake()
    {
        button = GetComponent<Button>();

        if (cardImage == null)
            cardImage = GetComponent<Image>();
    }

    public void Setup(
        CharacterSelectShopController controller,
        int cardIndex,
        CharacterDefinition data,
        bool owned,
        bool levelUnlocked,
        bool selected
    )
    {
        owner = controller;
        index = cardIndex;

        if (data == null)
        {
            gameObject.SetActive(false);
            return;
        }

        if (icon != null)
            icon.sprite = data.Icon;

        if (characterNamelbl != null)
            characterNamelbl.text = data.CharacterName;

        if (pricelbl != null)
            pricelbl.text = GetPriceText(data, owned);

        if (requiredLevellbl != null)
            requiredLevellbl.text = "LV " + data.RequiredLevel + "+";

        if (statuslbl != null)
            statuslbl.text = GetStatusText(owned, levelUnlocked);

        if (lockOverlay != null)
            lockOverlay.SetActive(!owned && !levelUnlocked);

        ApplyCardVisual(selected);
        SetupButton();
    }

    static string GetPriceText(CharacterDefinition data, bool owned)
    {
        if (owned)
            return "OWNED";

        if (data.Price <= 0)
            return "FREE";

        return "Gold: " + data.Price;
    }

    static string GetStatusText(bool owned, bool levelUnlocked)
    {
        if (owned)
            return "READY";

        if (!levelUnlocked)
            return "LOCKED";

        return "BUY";
    }

    void ApplyCardVisual(bool selected)
    {
        if (cardImage == null)
            return;

        if (selected && selectedSprite != null)
        {
            cardImage.sprite = selectedSprite;
            return;
        }

        if (normalSprite != null)
            cardImage.sprite = normalSprite;
    }

    void SetupButton()
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        owner?.SelectCharacter(index);
    }
}
