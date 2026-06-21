using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardItemUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image rewardIcon;
    [SerializeField] private TMP_Text rewardAmountlbl;

    public void Setup(Sprite icon, int amount)
    {
        if (rewardIcon != null && icon != null)
            rewardIcon.sprite = icon;

        if (rewardAmountlbl != null)
            rewardAmountlbl.text = amount.ToString();
    }
}
