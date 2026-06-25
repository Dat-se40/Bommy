using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapInfoItemCardUI : MonoBehaviour
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text itemNamelbl;
    [SerializeField] private TMP_Text itemDeslbl;

    public void Setup(Sprite icon, string itemName, string description)
    {
        if (itemIcon != null)
        {
            itemIcon.sprite = icon;
            itemIcon.enabled = icon != null;
        }

        if (itemNamelbl != null)
            itemNamelbl.text = itemName;

        if (itemDeslbl != null)
            itemDeslbl.text = description;
    }
}
