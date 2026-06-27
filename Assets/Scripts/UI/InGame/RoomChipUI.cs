using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class RoomChipUI : MonoBehaviour
{
    const string GameSceneName = "GameScene";
    const string RoomChipObjectName = "RoomChip";

    [SerializeField] private TMP_Text roomLabel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitializeRuntimeBinding()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        InstallIfNeeded(SceneManager.GetActiveScene());
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InstallIfNeeded(scene);
    }

    static void InstallIfNeeded(Scene scene)
    {
        if (!scene.IsValid() || scene.name != GameSceneName)
            return;

        GameObject roomChip = GameObject.Find(RoomChipObjectName);
        if (roomChip == null || roomChip.GetComponent<RoomChipUI>() != null)
            return;

        roomChip.AddComponent<RoomChipUI>();
    }

    void Awake()
    {
        if (roomLabel == null)
            roomLabel = GetComponentInChildren<TMP_Text>(true);
    }

    void OnEnable()
    {
        Refresh();
        InvokeRepeating(nameof(Refresh), 0.25f, 0.5f);
    }

    void OnDisable()
    {
        CancelInvoke(nameof(Refresh));
    }

    void Refresh()
    {
        if (roomLabel == null)
            return;

        string roomId = !string.IsNullOrWhiteSpace(GameSession.RoomId)
            ? GameSession.RoomId
            : GameSession.RoomName;

        roomLabel.text = "Room: " + (string.IsNullOrWhiteSpace(roomId) ? "-" : roomId);
    }
}
