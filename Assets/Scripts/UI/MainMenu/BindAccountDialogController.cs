using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Điều khiển BindAccountOverlay.
/// </summary>
public class BindAccountDialogController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject dialogRoot;

    [Header("Buttons")]
    [SerializeField] private Button closeBindAccountbtn;
    [SerializeField] private Button bindGooglebtn;
    [SerializeField] private Button bindDiscordbtn;
    [SerializeField] private Button bindSteambtn;

    [Header("Optional")]
    [SerializeField] private TMP_Text bindStatuslbl;

    private void Awake()
    {
        BindButtons();

        if (dialogRoot != null)
            dialogRoot.SetActive(false);
    }

    private void BindButtons()
    {
        if (closeBindAccountbtn != null)
        {
            closeBindAccountbtn.onClick.RemoveAllListeners();
            closeBindAccountbtn.onClick.AddListener(CloseDialog);
        }

        if (bindGooglebtn != null)
        {
            bindGooglebtn.onClick.RemoveAllListeners();
            bindGooglebtn.onClick.AddListener(() => BindProvider("Google", bindGooglebtn));
        }

        if (bindDiscordbtn != null)
        {
            bindDiscordbtn.onClick.RemoveAllListeners();
            bindDiscordbtn.onClick.AddListener(() => BindProvider("Discord", bindDiscordbtn));
        }

        if (bindSteambtn != null)
        {
            bindSteambtn.onClick.RemoveAllListeners();
            bindSteambtn.onClick.AddListener(() => BindProvider("Steam", bindSteambtn));
        }
    }

    public void OpenDialog()
    {
        SoundManager.Instance?.PlayOpenDialog();

        if (dialogRoot != null)
        {
            dialogRoot.SetActive(true);
            dialogRoot.transform.SetAsLastSibling();
        }

        SetStatus("Link account to protect your data.");
    }

    public void CloseDialog()
    {
        if (dialogRoot != null)
            dialogRoot.SetActive(false);
    }

    private void BindProvider(string provider, Button button)
    {
        // TODO[ACCOUNT]: Gọi provider/backend thật.
        SetStatus(provider + " linked.");

        if (button == null)
            return;

        button.interactable = false;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>();

        if (label != null)
            label.text = "BOUND";
    }

    private void SetStatus(string message)
    {
        if (bindStatuslbl != null)
            bindStatuslbl.text = message;
    }
}
