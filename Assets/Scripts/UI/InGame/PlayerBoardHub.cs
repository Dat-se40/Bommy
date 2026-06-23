using TMPro;
using UnityEngine;

/// <summary>
/// Gán mỗi PlayerBoardSlotUI với PlayerBoardState tương ứng (theo slotIndex).
/// </summary>
public class PlayerBoardHub : MonoBehaviour
{
    [SerializeField] private CharacterDatabase characterDatabase;
    [SerializeField] private PlayerBoardSlotUI[] slots;
    [SerializeField] private GameObject prefapPlayerSlotUI; 
    [SerializeField] private bool autoFindSlotsIfEmpty = true;

    [Header("Announcement")]
    [SerializeField] private BoardAnnouncementUI announcementUI;
    [SerializeField] private TMP_Text announcement;

    public static PlayerBoardHub Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        EnsureSlotsResolved();

        // TODO[ANNOUNCE] Bật lại khi hoàn thiện BoardAnnouncementUI + UITopLayerSupport.
        // if (announcementUI == null)
        //     announcementUI = GetComponentInChildren<BoardAnnouncementUI>(true);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void OnEnable()
    {
        MatchSessionBroker.RosterChanged += RefreshStaticRoster;
    }

    void OnDisable()
    {
        MatchSessionBroker.RosterChanged -= RefreshStaticRoster;
    }

    void Start()
    {
        if (characterDatabase != null)
            MatchSessionBroker.SetCharacterCatalog(characterDatabase);

        RebindAllFromScene();
    }

    public void OnNetworkPlayerRegistered(PlayerMatchProfile profile)
    {
        MatchSessionBroker.RegisterRemotePlayer(profile);
        BindSlot(profile.slotIndex, profile.isLocal);
        RebindAllFromScene();
    }

    /// <summary>
    /// Gọi từ PlayerBoardState khi SyncVar identity đã replicate (mọi client).
    /// Không phụ thuộc ObserversRpc từ spawner.
    /// </summary>
    public void RegisterBoardState(PlayerBoardState state)
    {
        if (state == null)
            return;

        EnsureSlotsResolved();

        if (slots == null)
            return;

        int index = state.SlotIndex;

        if (!FlowGuard.IsValidSlotIndex(index, slots.Length, out string reason))
        {
            FlowGuard.Error(FlowGuard.TagHud, reason, this);
            return;
        }

        if (slots[index] == null)
        {
            FlowGuard.Error(FlowGuard.TagHud, "Slot UI is null at index " + index, this);
            return;
        }

        bool isLocal = state.isOwner;
        slots[index].AssignPlayer(state, characterDatabase, isLocal);
    }

    public void BindSlot(int index, bool isLocal = false)
    {
        if (slots == null)
            return;

        if (!FlowGuard.IsValidSlotIndex(index, slots.Length, out string reason))
        {
            FlowGuard.Error(FlowGuard.TagHud, reason, this);
            return;
        }

        if (slots[index] == null)
        {
            FlowGuard.Error(FlowGuard.TagHud, "Slot UI is null at index " + index, this);
            return;
        }

        PlayerBoardState state = FindBoardState(index);

        if (state != null)
            slots[index].AssignPlayer(state, characterDatabase, isLocal);
        else if (MatchSessionBroker.TryGetRosterSlot(index, out PlayerMatchProfile profile))
        {
            // Chưa có network object — chờ spawn.
            // TODO[NETWORK] Retry bind khi PlayerBoardState spawn xong.
        }
        else
        {
            slots[index].SetEmpty();
        }
    }

    public void RebindAllFromScene()
    {
        EnsureSlotsResolved();

        if (slots == null)
            return;

        PlayerBoardState[] states = FindObjectsByType<PlayerBoardState>(FindObjectsSortMode.None);

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                continue;

            PlayerBoardState match = null;

            for (int s = 0; s < states.Length; s++)
            {
                if (states[s].SlotIndex == i)
                {
                    match = states[s];
                    break;
                }
            }

            if (match != null)
            {
                bool isLocal = match.isOwner;
                slots[i].AssignPlayer(match, characterDatabase, isLocal);
            }
            else
            {
                slots[i].SetEmpty();
            }
        }
    }

    void RefreshStaticRoster()
    {
        RebindAllFromScene();
    }

    public void OnBoardStateDespawned(int slotIndex)
    {
        if (slots == null)
            return;

        if (!FlowGuard.IsValidSlotIndex(slotIndex, slots.Length, out _))
            return;

        if (slots[slotIndex] == null)
            return;

        slots[slotIndex].SetEmpty();
    }
    // TODO[ANNOUNCE] Hiện announcement slide + top canvas khi nhặt buff / sự kiện in-game.
    public void LocalAnnounce(string message)
    {
    }
    static PlayerBoardState FindBoardState(int slotIndex)
    {
        PlayerBoardState[] states = FindObjectsByType<PlayerBoardState>(FindObjectsSortMode.None);

        for (int i = 0; i < states.Length; i++)
        {
            if (states[i].SlotIndex == slotIndex)
                return states[i];
        }

        return null;
    }

    void EnsureSlotsResolved()
    {
        if (!autoFindSlotsIfEmpty && !IsSlotsEmpty() && !HasNullSlot())
            return;

        if (IsSlotsEmpty() || HasNullSlot())
            AutoFillSlotsFromChildren();

        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
                slots[i].SetSlotIndex(i);
        }
    }

    bool HasNullSlot()
    {
        if (slots == null)
            return true;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                return true;
        }

        return false;
    }

    bool IsSlotsEmpty()
    {
        if (slots == null || slots.Length == 0)
            return true;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
                return false;
        }

        return true;
    }

    [ContextMenu("Auto Fill Slots From Children")]
    void AutoFillSlotsFromChildren()
    {
        UIAutoBindUtility.RecordUndo(this, "Auto Fill PlayerBoardHub Slots");

        slots = UIAutoBindUtility.GetComponentsInChildrenSorted<PlayerBoardSlotUI>(
            transform,
            includeInactive: true,
            comparison: (a, b) => a.SlotIndex.CompareTo(b.SlotIndex)
        );

        Debug.Log(
            "[FLOW:HUD] Auto filled PlayerBoardSlotUI count=" + (slots != null ? slots.Length : 0),
            this
        );

        UIAutoBindUtility.SetDirty(this);
    }


}
