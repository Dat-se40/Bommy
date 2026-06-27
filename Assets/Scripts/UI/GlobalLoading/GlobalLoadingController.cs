using System;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class GlobalLoadingController : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string authGateSceneName = "AuthGate";
    [SerializeField] private string dedicatedServerSceneName = "GameScene";

    [Header("UI References")]
    public TMP_Text connectionStatusText;
    public TMP_Text retryAttemptText;
    public Button retryButton;

    private CancellationTokenSource connectionCts;
    private BackendConnectionService connectionService;
    private bool isConnecting;

    private void Awake()
    {
        if (DedicatedServerBootstrap.IsDedicatedServerRuntime)
        {
            Debug.Log("[GlobalLoadingController] Dedicated server runtime detected. Loading dedicated server scene...");
            enabled = false;
            SceneManager.LoadScene(dedicatedServerSceneName);
            return;
        }

        // Ensure services exist
        connectionService = FindAnyObjectByType<BackendConnectionService>();
        if (connectionService == null)
        {
            GameObject serviceGo = new GameObject("BackendConnectionService");
            connectionService = serviceGo.AddComponent<BackendConnectionService>();
        }

        // Dynamically build UI if not assigned
        EnsureUI();
    }

    private void Start()
    {
        if (DedicatedServerBootstrap.IsDedicatedServerRuntime)
            return;

        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(RetryNow);
            retryButton.gameObject.SetActive(false); // Hide initially
        }

        StartConnectionFlow();
    }

    private void OnDestroy()
    {
        CancelConnection();
    }

    private void StartConnectionFlow()
    {
        CancelConnection();
        connectionCts = new CancellationTokenSource();
        _ = RunConnectionFlowAsync(connectionCts.Token);
    }

    private void CancelConnection()
    {
        if (connectionCts != null)
        {
            connectionCts.Cancel();
            connectionCts.Dispose();
            connectionCts = null;
        }
    }

    private async Task RunConnectionFlowAsync(CancellationToken token)
    {
        if (isConnecting) return;
        isConnecting = true;

        if (connectionStatusText != null)
            connectionStatusText.text = "Connecting to server...";

        if (retryAttemptText != null)
            retryAttemptText.text = "";

        if (retryButton != null)
            retryButton.gameObject.SetActive(false);

        // Listen for attempt changes
        connectionService.OnAttemptCountChanged += UpdateAttemptCount;

        BackendConnectionResult connectionResult = await connectionService.WaitForBackendAsync(token);

        connectionService.OnAttemptCountChanged -= UpdateAttemptCount;

        if (token.IsCancellationRequested)
        {
            isConnecting = false;
            return;
        }

        if (connectionResult.Success)
        {
            if (connectionStatusText != null)
                connectionStatusText.text = "Restoring session...";

            AuthService auth = AuthService.GetOrCreate();
            AuthResult restoreResult = await auth.TryRestoreSessionAsync();

            if (token.IsCancellationRequested)
            {
                isConnecting = false;
                return;
            }

            if (restoreResult.Success)
            {
                Debug.Log("[GlobalLoadingController] Session restored. Loading MainMenu...");
                SceneManager.LoadScene(mainMenuSceneName);
            }
            else
            {
                Debug.Log("[GlobalLoadingController] No valid session. Loading AuthGate...");
                SceneManager.LoadScene(authGateSceneName);
            }
        }
        else
        {
            // If failed (should not happen since WaitForBackend retries forever unless cancelled)
            if (connectionStatusText != null)
                connectionStatusText.text = "Failed to connect to backend.";
            
            if (retryButton != null)
                retryButton.gameObject.SetActive(true);
        }

        isConnecting = false;
    }

    private void UpdateAttemptCount(int attempt)
    {
        if (attempt > 1)
        {
            if (connectionStatusText != null)
                connectionStatusText.text = "Retrying connection...";

            if (retryAttemptText != null)
                retryAttemptText.text = $"Attempt #{attempt}";

            if (retryButton != null && !retryButton.gameObject.activeSelf)
                retryButton.gameObject.SetActive(true);
        }
    }

    public void RetryNow()
    {
        Debug.Log("[GlobalLoadingController] Manual retry requested.");
        StartConnectionFlow();
    }

    private void EnsureUI()
    {
        // If UI elements are already assigned, we are good
        if (connectionStatusText != null && retryAttemptText != null && retryButton != null)
            return;

        // Find existing UI canvas or create one
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("Canvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
        }

        // Create Panel Background
        Transform bgTransform = canvas.transform.Find("Background");
        GameObject bgGo;
        if (bgTransform == null)
        {
            bgGo = new GameObject("Background");
            bgGo.transform.SetParent(canvas.transform, false);
            Image bgImage = bgGo.AddComponent<Image>();
            bgImage.color = new Color(0.06f, 0.09f, 0.16f, 1f); // Sleek Dark Slate #0F172A
            RectTransform bgRect = bgImage.rectTransform;
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
        }
        else
        {
            bgGo = bgTransform.gameObject;
        }

        // Create Status Text
        if (connectionStatusText == null)
        {
            Transform textTrans = bgGo.transform.Find("StatusText");
            if (textTrans == null)
            {
                GameObject statusGo = new GameObject("StatusText");
                statusGo.transform.SetParent(bgGo.transform, false);
                connectionStatusText = statusGo.AddComponent<TextMeshProUGUI>();
                connectionStatusText.text = "Connecting to server...";
                connectionStatusText.fontSize = 32;
                connectionStatusText.alignment = TextAlignmentOptions.Center;
                connectionStatusText.color = Color.white;
                RectTransform statusRect = connectionStatusText.rectTransform;
                statusRect.anchorMin = new Vector2(0, 0.5f);
                statusRect.anchorMax = new Vector2(1, 0.5f);
                statusRect.anchoredPosition = new Vector2(0, 40);
                statusRect.sizeDelta = new Vector2(0, 80);
            }
            else
            {
                connectionStatusText = textTrans.GetComponent<TextMeshProUGUI>();
            }
        }

        // Create Retry Count Text
        if (retryAttemptText == null)
        {
            Transform countTrans = bgGo.transform.Find("RetryCountText");
            if (countTrans == null)
            {
                GameObject countGo = new GameObject("RetryCountText");
                countGo.transform.SetParent(bgGo.transform, false);
                retryAttemptText = countGo.AddComponent<TextMeshProUGUI>();
                retryAttemptText.text = "";
                retryAttemptText.fontSize = 20;
                retryAttemptText.alignment = TextAlignmentOptions.Center;
                retryAttemptText.color = new Color(0.66f, 0.7f, 0.68f, 1f);
                RectTransform countRect = retryAttemptText.rectTransform;
                countRect.anchorMin = new Vector2(0, 0.5f);
                countRect.anchorMax = new Vector2(1, 0.5f);
                countRect.anchoredPosition = new Vector2(0, -20);
                countRect.sizeDelta = new Vector2(0, 40);
            }
            else
            {
                retryAttemptText = countTrans.GetComponent<TextMeshProUGUI>();
            }
        }

        // Create Retry Button
        if (retryButton == null)
        {
            Transform btnTrans = bgGo.transform.Find("RetryButton");
            if (btnTrans == null)
            {
                GameObject buttonGo = new GameObject("RetryButton");
                buttonGo.transform.SetParent(bgGo.transform, false);
                Image btnImage = buttonGo.AddComponent<Image>();
                btnImage.color = new Color(0.18f, 0.2f, 0.2f, 1f);
                retryButton = buttonGo.AddComponent<Button>();
                RectTransform btnRect = btnImage.rectTransform;
                btnRect.anchorMin = new Vector2(0.5f, 0.5f);
                btnRect.anchorMax = new Vector2(0.5f, 0.5f);
                btnRect.anchoredPosition = new Vector2(0, -90);
                btnRect.sizeDelta = new Vector2(160, 45);

                GameObject btnTextGo = new GameObject("Text");
                btnTextGo.transform.SetParent(buttonGo.transform, false);
                TextMeshProUGUI btnText = btnTextGo.AddComponent<TextMeshProUGUI>();
                btnText.text = "Retry Now";
                btnText.fontSize = 18;
                btnText.alignment = TextAlignmentOptions.Center;
                btnText.color = Color.white;
                RectTransform btnTextRect = btnText.rectTransform;
                btnTextRect.anchorMin = Vector2.zero;
                btnTextRect.anchorMax = Vector2.one;
                btnTextRect.sizeDelta = Vector2.zero;
            }
            else
            {
                retryButton = btnTrans.GetComponent<Button>();
            }
        }

        // Ensure EventSystem exists
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }
}
