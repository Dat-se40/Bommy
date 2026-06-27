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

        if (lockOverlay != null)
            lockOverlay.SetActive(false);
    }

    public void Setup(
        CharacterSelectShopController controller,
        int cardIndex,
        CharacterDefinition data,
        bool owned,
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

        if (statuslbl != null)
            statuslbl.text = GetStatusText(owned);

        if (lockOverlay != null)
            lockOverlay.SetActive(false);

        ApplyCardVisual(selected);
        SetupButton();
    }

    static string GetPriceText(CharacterDefinition data, bool owned)
    {
        if (owned)
            return "OWNED";

        if (data.Price <= 0)
            return "FREE";

        return "Coins: " + data.Price;
    }

    static string GetStatusText(bool owned)
    {
        if (owned)
            return "READY";

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

    [ContextMenu("Auto Bind UI From Children")]
    private void AutoBindUIFromChildren()
    {
        UIAutoBindUtility.RecordUndo(this, "Auto Bind CharacterCardUI");

        button = GetComponent<Button>();

        if (cardImage == null)
            cardImage = GetComponent<Image>();

        icon = UIAutoBindUtility.FindChildComponent<Image>(
            this,
            "Icon",
            "Avatar",
            "CharacterIcon"
        );

        characterNamelbl = UIAutoBindUtility.FindChildComponent<TMP_Text>(
            this,
            "CharacterNamelbl",
            "CharacterNameLbl",
            "Namelbl",
            "NameLbl",
            "Name"
        );

        pricelbl = UIAutoBindUtility.FindChildComponent<TMP_Text>(
            this,
            "Pricelbl",
            "PriceLbl",
            "Price"
        );


        statuslbl = UIAutoBindUtility.FindChildComponent<TMP_Text>(
            this,
            "Statuslbl",
            "StatusLbl",
            "Status"
        );

        lockOverlay = UIAutoBindUtility.FindChildGameObject(
            this,
            "LockOverlay",
            "LockedOverlay",
            "Lock",
            "Locked"
        );

        if (normalSprite == null && cardImage != null)
            normalSprite = cardImage.sprite;

        TryBindSelectedSpriteFromButton();

        UIAutoBindUtility.LogBindResult(
            this,
            "Auto Bind CharacterCardUI result for " + gameObject.name,
            new BindLogItem("Card Image", cardImage),
            new BindLogItem("Button", button),
            new BindLogItem("Icon", icon),
            new BindLogItem("CharacterNamelbl", characterNamelbl),
            new BindLogItem("Pricelbl", pricelbl),
            new BindLogItem("Statuslbl", statuslbl),
            new BindLogItem("LockOverlay", lockOverlay),
            new BindLogItem("Normal Sprite", normalSprite),
            new BindLogItem("Selected Sprite", selectedSprite)
        );

        UIAutoBindUtility.SetDirty(this);
    }

    /// <summary>
    /// Nếu Button đang dùng Sprite Swap thì lấy Selected Sprite làm selectedSprite.
    /// </summary>
    private void TryBindSelectedSpriteFromButton()
    {
        if (button == null)
            return;

        if (selectedSprite != null)
            return;

        Sprite selected = button.spriteState.selectedSprite;

        if (selected != null)
            selectedSprite = selected;
    }

}
