using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Một slot board — theo dõi riêng PlayerBoardState được chỉ định.
/// </summary>
public class PlayerBoardSlotUI : MonoBehaviour
{
    [Header("Slot")]
    [SerializeField] private int slotIndex;

    [Header("Main UI")]
    [SerializeField] private Image avatar;
    [FormerlySerializedAs("nameLabel")]
    [SerializeField] private TMP_Text playerNamelbl;
    [FormerlySerializedAs("hpLabel")]
    [SerializeField] private TMP_Text hplbl;
    [SerializeField] private TMP_Text bomblbl;
    [SerializeField] private TMP_Text trapSkilllbl;
    [SerializeField] private TMP_Text goldlbl;
    [FormerlySerializedAs("scoreLabel")]
    [SerializeField] private TMP_Text scorelbl;

    [Header("Placeable Display")]
    [SerializeField] private string bombCountFormat = "{0}/{1}";
    [SerializeField] private string trapSkillFormat = "Trap:{0}";
    [SerializeField] private bool hideTrapSkillWhenZero = true;

    [Header("HP Icons")]
    [SerializeField] private GameObject hpGroup;
    [SerializeField] private Image[] hpIcons = new Image[3];

    [Header("States")]
    [SerializeField] private GameObject emptyState;
    [SerializeField] private GameObject localHighlight;

    [Header("HP Icon Colors")]
    [SerializeField] private Color activeHpColor = Color.white;
    [SerializeField] private Color inactiveHpColor = new Color(1f, 1f, 1f, 0.25f);

    [Header("Damage Feedback")]
    [SerializeField] private RectTransform shakeTarget;
    [SerializeField] private float heartPunchScale = 0.4f;
    [SerializeField] private float heartPunchDuration = 0.2f;
    [SerializeField] private float slotShakeStrength = 20f;
    [SerializeField] private float slotShakeDuration = 0.4f;
    [SerializeField] private int slotShakeVibrato = 8;

    PlayerBoardState trackedState;
    CharacterDatabase characterDatabase;
    int lastDisplayedHp = -1;

    void Awake()
    {
        if (shakeTarget == null)
            shakeTarget = transform as RectTransform;

        EnsureUiReferences();
    }

    public int SlotIndex => slotIndex;

    public void SetSlotIndex(int index) => slotIndex = index;

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

        lastDisplayedHp = -1;
        RefreshFromState();
    }

    public void SetEmpty()
    {
        StopDamageFeedback();
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

        if (trapSkilllbl != null)
            trapSkilllbl.text = string.Empty;

        if (goldlbl != null)
            goldlbl.text = string.Empty;

        if (scorelbl != null)
            scorelbl.text = string.Empty;

        SetHpIcons(0, hpIcons != null ? hpIcons.Length : 0);
        lastDisplayedHp = -1;
        gameObject.SetActive(false);
    }

    void RefreshFromState()
    {
        if (trackedState == null)
        {
            SetEmpty();
            return;
        }

        gameObject.SetActive(true);

        if (emptyState != null)
            emptyState.SetActive(false);

        int newHp = trackedState.CurrentHp;
        int newMaxHp = trackedState.MaxHp;
        bool tookDamage = lastDisplayedHp >= 0 && newHp < lastDisplayedHp;
        int previousHp = lastDisplayedHp;
        int newGold = trackedState.Gold;
        CharacterDefinition definition = characterDatabase != null
            ? characterDatabase.GetById(trackedState.CharacterId)
            : MatchSessionBroker.ResolveDefinition(new PlayerMatchProfile
            {
                characterId = trackedState.CharacterId,
                catalogIndex = trackedState.CatalogIndex
            });

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
            hplbl.text = "HP " + newHp + "/" + newMaxHp;

        SetHpIcons(newHp, newMaxHp);

        if (tookDamage)
            PlayDamageFeedback(previousHp, newHp);

        lastDisplayedHp = newHp;

        RefreshPlaceableLabels();

        if (goldlbl != null)
            goldlbl.text = "GOLD: " + newGold;

        if (scorelbl != null)
            scorelbl.text = trackedState.Score.ToString();
    }

    void RefreshPlaceableLabels()
    {
        if (trackedState == null)
            return;

        if (bomblbl != null)
        {
            bomblbl.text = string.Format(
                bombCountFormat,
                trackedState.AvailableBombs,
                trackedState.MaxBombs
            );
        }

        if (trapSkilllbl == null)
            return;

        int trapCharges = trackedState.TrapSkillCharges;

        if (hideTrapSkillWhenZero && trapCharges <= 0)
        {
            trapSkilllbl.text = string.Empty;
            return;
        }

        trapSkilllbl.text = string.Format(trapSkillFormat, trapCharges);
    }

    /// <summary>
    /// Cập nhật icon trái tim theo HP hiện tại.
    /// </summary>
    void SetHpIcons(int currentHp, int maxHp)
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

    void PlayDamageFeedback(int previousHp, int currentHp)
    {
        int lostHeartIndex = previousHp - 1;

        if (hpIcons != null
            && lostHeartIndex >= 0
            && lostHeartIndex < hpIcons.Length
            && hpIcons[lostHeartIndex] != null)
        {
            Transform heart = hpIcons[lostHeartIndex].transform;
            heart.DOKill();
            heart.localScale = Vector3.one;
            heart.DOPunchScale(Vector3.one * heartPunchScale, heartPunchDuration, 1, 0.5f);
        }

        if (shakeTarget != null)
        {
            shakeTarget.DOKill();
            shakeTarget.localRotation = Quaternion.identity;
            shakeTarget.DOPunchRotation(
                new Vector3(0f, 0f, slotShakeStrength),
                slotShakeDuration,
                slotShakeVibrato,
                0.5f
            );
        }
    }

    void StopDamageFeedback()
    {
        if (hpIcons != null)
        {
            for (int i = 0; i < hpIcons.Length; i++)
            {
                if (hpIcons[i] == null)
                    continue;

                hpIcons[i].transform.DOKill();
                hpIcons[i].transform.localScale = Vector3.one;
            }
        }

        if (shakeTarget != null)
        {
            shakeTarget.DOKill();
            shakeTarget.localRotation = Quaternion.identity;
        }
    }

    void OnDisable()
    {
        StopDamageFeedback();
        Unsubscribe();
    }

    void Unsubscribe()
    {
        if (trackedState != null)
            trackedState.Changed -= RefreshFromState;
    }

    void EnsureUiReferences()
    {
        if (avatar != null
            && playerNamelbl != null
            && hplbl != null
            && bomblbl != null
            && goldlbl != null)
        {
            return;
        }

        AutoBindUIFromChildren();
    }

    [ContextMenu("Auto Bind UI From Children")]
    void AutoBindUIFromChildren()
    {
        UIAutoBindUtility.RecordUndo(this, "Auto Bind PlayerBoardSlotUI");

        avatar = UIAutoBindUtility.FindChildComponent<Image>(
            this,
            "Avatar",
            "Icon",
            "CharacterIcon"
        );

        playerNamelbl = UIAutoBindUtility.FindChildComponent<TMP_Text>(
            this,
            "PlayerNamelbl",
            "PlayerNameLbl",
            "Namelbl",
            "NameLbl",
            "Name"
        );

        hplbl = UIAutoBindUtility.FindChildComponent<TMP_Text>(
            this,
            "Hplbl",
            "HpLbl",
            "HPLbl",
            "HpText"
        );

        bomblbl = UIAutoBindUtility.FindChildComponent<TMP_Text>(
            this,
            "Bomblbl",
            "BombLbl",
            "BombText"
        );

        trapSkilllbl = UIAutoBindUtility.FindChildComponent<TMP_Text>(
            this,
            "TrapSkilllbl",
            "TrapLbl",
            "TrapSkillLbl",
            "TrapText"
        );

        goldlbl = UIAutoBindUtility.FindChildComponent<TMP_Text>(
            this,
            "Goldlbl",
            "GoldLbl",
            "GoldText"
        );

        scorelbl = UIAutoBindUtility.FindChildComponent<TMP_Text>(
            this,
            "Scorelbl",
            "ScoreLbl",
            "ScoreText"
        );

        hpGroup = UIAutoBindUtility.FindChildGameObject(
            this,
            "HpGroup",
            "HPGroup",
            "HeartGroup"
        );

        hpIcons = new Image[3];

        hpIcons[0] = UIAutoBindUtility.FindChildComponent<Image>(
            this,
            "HpIcon_0",
            "HPIcon_0",
            "Heart_0"
        );

        hpIcons[1] = UIAutoBindUtility.FindChildComponent<Image>(
            this,
            "HpIcon_1",
            "HPIcon_1",
            "Heart_1"
        );

        hpIcons[2] = UIAutoBindUtility.FindChildComponent<Image>(
            this,
            "HpIcon_2",
            "HPIcon_2",
            "Heart_2"
        );

        emptyState = UIAutoBindUtility.FindChildGameObject(
            this,
            "EmptyState",
            "Empty",
            "WaitingState"
        );

        localHighlight = UIAutoBindUtility.FindChildGameObject(
            this,
            "LocalHighlight",
            "Highlight",
            "SelectedHighlight"
        );

        UIAutoBindUtility.LogBindResult(
            this,
            "Auto Bind PlayerBoardSlotUI result for " + gameObject.name,
            new BindLogItem("Avatar", avatar),
            new BindLogItem("PlayerNamelbl", playerNamelbl),
            new BindLogItem("Hplbl", hplbl),
            new BindLogItem("Bomblbl", bomblbl),
            new BindLogItem("TrapSkilllbl", trapSkilllbl),
            new BindLogItem("Goldlbl", goldlbl),
            new BindLogItem("Scorelbl", scorelbl),
            new BindLogItem("HpGroup", hpGroup),
            new BindLogItem("HpIcon_0", GetHpIconForLog(0)),
            new BindLogItem("HpIcon_1", GetHpIconForLog(1)),
            new BindLogItem("HpIcon_2", GetHpIconForLog(2)),
            new BindLogItem("EmptyState", emptyState),
            new BindLogItem("LocalHighlight", localHighlight)
        );

        UIAutoBindUtility.SetDirty(this);
    }

    Image GetHpIconForLog(int iconIndex)
    {
        if (hpIcons == null)
            return null;

        if (iconIndex < 0 || iconIndex >= hpIcons.Length)
            return null;

        return hpIcons[iconIndex];
    }
}
