using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Một slot trong board HUD.
/// Slot này theo dõi một PlayerBoardState và tự cập nhật avatar, tên, HP, điểm.
/// </summary>
public class PlayerBoardSlotUI : MonoBehaviour
{
    [Header("Slot")]
    [SerializeField] private int slotIndex;

    [Header("Main UI")]
    [SerializeField] private Image avatar;
    [SerializeField] private TMP_Text playerNamelbl;
    [SerializeField] private TMP_Text hplbl;
    [SerializeField] private TMP_Text bomblbl;
    [SerializeField] private TMP_Text goldlbl;
    [SerializeField] private TMP_Text scorelbl;

    [Header("HP Icons")]
    [SerializeField] private GameObject hpGroup;
    [SerializeField] private Image[] hpIcons = new Image[3];

    [Header("States")]
    [SerializeField] private GameObject emptyState;
    [SerializeField] private GameObject localHighlight;

    [Header("HP Icon Colors")]
    [SerializeField] private Color activeHpColor = Color.white;
    [SerializeField] private Color inactiveHpColor = new Color(1f, 1f, 1f, 0.25f);

    private PlayerBoardState trackedState;
    private CharacterDatabase characterDatabase;

    public int SlotIndex => slotIndex;

    public void AssignPlayer(
        PlayerBoardState state,
        CharacterDatabase database,
        bool isLocal = false
    )
    {
        Unsubscribe();

        trackedState = state;
        characterDatabase = database;

        if (trackedState != null)
            trackedState.Changed += RefreshFromState;

        if (localHighlight != null)
            localHighlight.SetActive(isLocal);

        RefreshFromState();
    }

    public void SetEmpty()
    {
        Unsubscribe();
        trackedState = null;

        if (emptyState != null)
            emptyState.SetActive(true);

        if (localHighlight != null)
            localHighlight.SetActive(false);

        if (avatar != null)
        {
            avatar.sprite = null;
            avatar.enabled = false;
        }

        if (playerNamelbl != null)
            playerNamelbl.text = "—";

        if (hplbl != null)
            hplbl.text = string.Empty;

        if (bomblbl != null)
            bomblbl.text = string.Empty;

        if (goldlbl != null)
            goldlbl.text = string.Empty;

        if (scorelbl != null)
            scorelbl.text = string.Empty;

        SetHpIcons(0, hpIcons != null ? hpIcons.Length : 0);
    }

    private void RefreshFromState()
    {
        if (trackedState == null)
        {
            SetEmpty();
            return;
        }

        if (emptyState != null)
            emptyState.SetActive(false);

        CharacterDefinition definition = ResolveDefinition();

        if (avatar != null)
        {
            Sprite sprite = definition != null ? definition.Icon : null;
            avatar.sprite = sprite;
            avatar.enabled = sprite != null;
        }

        if (playerNamelbl != null)
        {
            playerNamelbl.text = trackedState.DisplayName;

            if (trackedState.IsEliminated)
                playerNamelbl.text += " (OUT)";
        }

        if (hplbl != null)
            hplbl.text = "HP " + trackedState.CurrentHp + "/" + trackedState.MaxHp;

        SetHpIcons(trackedState.CurrentHp, trackedState.MaxHp);

        // Hiện tại PlayerBoardState chưa có Bomb/Gold sync riêng.
        // Để trống để tránh đọc property chưa tồn tại.
        if (bomblbl != null)
            bomblbl.text = string.Empty;

        if (goldlbl != null)
            goldlbl.text = string.Empty;

        if (scorelbl != null)
            scorelbl.text = trackedState.Score.ToString();
    }

    private CharacterDefinition ResolveDefinition()
    {
        if (trackedState == null)
            return null;

        if (characterDatabase != null)
            return characterDatabase.GetById(trackedState.CharacterId);

        return MatchSessionBroker.ResolveDefinition(new PlayerMatchProfile
        {
            characterId = trackedState.CharacterId,
            catalogIndex = trackedState.CatalogIndex
        });
    }

    /// <summary>
    /// Cập nhật icon trái tim theo HP hiện tại.
    /// </summary>
    private void SetHpIcons(int currentHp, int maxHp)
    {
        if (hpGroup != null)
            hpGroup.SetActive(hpIcons != null && hpIcons.Length > 0);

        if (hpIcons == null)
            return;

        int visibleCount = Mathf.Min(maxHp, hpIcons.Length);

        for (int i = 0; i < hpIcons.Length; i++)
        {
            if (hpIcons[i] == null)
                continue;

            bool shouldShow = i < visibleCount;
            bool isActive = i < currentHp;

            hpIcons[i].gameObject.SetActive(shouldShow);
            hpIcons[i].color = isActive ? activeHpColor : inactiveHpColor;
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Unsubscribe()
    {
        if (trackedState != null)
            trackedState.Changed -= RefreshFromState;
    }

    [ContextMenu("Auto Bind UI From Children")]
    private void AutoBindUIFromChildren()
    {
#if UNITY_EDITOR
        Undo.RecordObject(this, "Auto Bind PlayerBoardSlotUI");
#endif

        avatar = FindChildComponent<Image>("Avatar");

        playerNamelbl = FindChildComponent<TMP_Text>(
            "PlayerNamelbl",
            "PlayerNameLbl",
            "Namelbl",
            "NameLbl"
        );

        hplbl = FindChildComponent<TMP_Text>(
            "Hplbl",
            "HpLbl",
            "HPLbl",
            "HpText"
        );

        bomblbl = FindChildComponent<TMP_Text>(
            "Bomblbl",
            "BombLbl",
            "BombText"
        );

        goldlbl = FindChildComponent<TMP_Text>(
            "Goldlbl",
            "GoldLbl",
            "GoldText"
        );

        scorelbl = FindChildComponent<TMP_Text>(
            "Scorelbl",
            "ScoreLbl",
            "ScoreText"
        );

        Transform hpGroupTransform = FindChildTransform(
            "HpGroup",
            "HPGroup",
            "HeartGroup"
        );

        hpGroup = hpGroupTransform != null ? hpGroupTransform.gameObject : null;

        hpIcons = new Image[3];
        hpIcons[0] = FindChildComponent<Image>("HpIcon_0", "HPIcon_0", "Heart_0");
        hpIcons[1] = FindChildComponent<Image>("HpIcon_1", "HPIcon_1", "Heart_1");
        hpIcons[2] = FindChildComponent<Image>("HpIcon_2", "HPIcon_2", "Heart_2");

        LogBindResult();

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        PrefabUtility.RecordPrefabInstancePropertyModifications(this);
#endif
    }

    private T FindChildComponent<T>(params string[] names) where T : Component
    {
        Transform child = FindChildTransform(names);

        if (child == null)
            return null;

        T component = child.GetComponent<T>();

        if (component == null)
        {
            Debug.LogWarning(
                "[FLOW:HUD] Found child '" + child.name + "' but missing component " + typeof(T).Name,
                child
            );
        }

        return component;
    }

    private Transform FindChildTransform(params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            Transform result = FindDeepChildByName(transform, names[i]);

            if (result != null)
                return result;
        }

        return null;
    }

    private Transform FindDeepChildByName(Transform root, string targetName)
    {
        if (root == null)
            return null;

        if (IsSameName(root.name, targetName))
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindDeepChildByName(root.GetChild(i), targetName);

            if (result != null)
                return result;
        }

        return null;
    }

    private bool IsSameName(string a, string b)
    {
        return NormalizeName(a) == NormalizeName(b);
    }

    private string NormalizeName(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value
            .Replace(" ", "")
            .Replace("_", "")
            .Replace("-", "")
            .ToLowerInvariant();
    }

    private void LogBindResult()
    {
        Debug.Log(
            "[FLOW:HUD] Auto Bind result for " + gameObject.name + "\n" +
            "Avatar: " + Bound(avatar) + "\n" +
            "PlayerNamelbl: " + Bound(playerNamelbl) + "\n" +
            "Hplbl: " + Bound(hplbl) + "\n" +
            "Bomblbl: " + Bound(bomblbl) + "\n" +
            "Goldlbl: " + Bound(goldlbl) + "\n" +
            "Scorelbl: " + Bound(scorelbl) + "\n" +
            "HpGroup: " + Bound(hpGroup) + "\n" +
            "HpIcon_0: " + Bound(GetHpIcon(0)) + "\n" +
            "HpIcon_1: " + Bound(GetHpIcon(1)) + "\n" +
            "HpIcon_2: " + Bound(GetHpIcon(2)),
            this
        );
    }

    private Image GetHpIcon(int index)
    {
        if (hpIcons == null)
            return null;

        if (index < 0 || index >= hpIcons.Length)
            return null;

        return hpIcons[index];
    }

    private string Bound(Object obj)
    {
        return obj != null ? "OK" : "MISSING";
    }
}
