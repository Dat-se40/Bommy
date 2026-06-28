using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Điều khiển RedeemCodeOverlay.
/// </summary>
public class RedeemCodeDialogController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject dialogRoot;

    [Header("Buttons")]
    [SerializeField] private Button closeRedeemCodebtn;
    [SerializeField] private Button redeembtn;

    [Header("Input")]
    [SerializeField] private TMP_InputField redeemCodeInput;

    [Header("Status")]
    [SerializeField] private TMP_Text redeemStatuslbl;

    private void Awake()
    {
        BindButtons();

        if (dialogRoot != null)
            dialogRoot.SetActive(false);
    }

    private void BindButtons()
    {
        if (closeRedeemCodebtn != null)
        {
            closeRedeemCodebtn.onClick.RemoveAllListeners();
            closeRedeemCodebtn.onClick.AddListener(CloseDialog);
        }

        if (redeembtn != null)
        {
            redeembtn.onClick.RemoveAllListeners();
            redeembtn.onClick.AddListener(Redeem);
        }

        if (redeemCodeInput != null)
        {
            redeemCodeInput.onSubmit.RemoveAllListeners();
            redeemCodeInput.onSubmit.AddListener(_ => Redeem());
        }
    }

    public void OpenDialog()
    {
        SoundManager.Instance?.PlayOpenDialog();

        if (redeemCodeInput != null)
            redeemCodeInput.text = "";

        SetStatus("");

        if (dialogRoot != null)
        {
            dialogRoot.SetActive(true);
            dialogRoot.transform.SetAsLastSibling();
        }

        if (redeemCodeInput != null)
        {
            redeemCodeInput.Select();
            redeemCodeInput.ActivateInputField();
        }
    }

    public void CloseDialog()
    {
        if (dialogRoot != null)
            dialogRoot.SetActive(false);
    }

    private void Redeem()
    {
        string code = redeemCodeInput != null
            ? redeemCodeInput.text.Trim().ToUpperInvariant()
            : "";

        if (string.IsNullOrEmpty(code))
        {
            SetStatus("Code is empty.");
            return;
        }

        // TODO[ACCOUNT]: Gửi code lên backend để redeem thật.
        SetStatus("Redeemed: " + code);
    }

    private void SetStatus(string message)
    {
        if (redeemStatuslbl != null)
            redeemStatuslbl.text = message;
    }
}
