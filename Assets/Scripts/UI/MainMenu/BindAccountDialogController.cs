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

    private async void BindProvider(string provider, Button button)
    {
        if (provider == "Steam")
        {
            if (SteamService.Instance == null || !SteamService.Instance.IsInitialized)
            {
                SetStatus("Steam client is not running or failed to initialize.");
                return;
            }

            if (button != null) button.interactable = false;

            string token = SteamService.Instance.GetAuthSessionTicket();
            if (string.IsNullOrEmpty(token))
            {
                SetStatus("Failed to retrieve Steam authentication ticket.");
                if (button != null) button.interactable = true;
                return;
            }

            SetStatus("Linking Steam account...");
            AuthResult result = await AuthService.Instance.LinkSteamAsync(token);

            if (!result.Success)
            {
                Debug.LogError($"[BindAccountDialogController] Failed to link Steam: {result.Error}");
                SetStatus($"Failed to link Steam: {result.Error}");
                if (button != null) button.interactable = true;
                return;
            }
        }
        else
        {
            // TODO[ACCOUNT]: Gọi provider/backend thật.
            SetStatus(provider + " linked.");
            if (button != null) button.interactable = false;
        }

        if (button == null)
            return;

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
