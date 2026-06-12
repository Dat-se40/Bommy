using UnityEngine;

/// <summary>
/// Gán mỗi PlayerBoardSlotUI với PlayerBoardState tương ứng (theo slotIndex).
/// </summary>
public class PlayerBoardHub : MonoBehaviour
{
    [SerializeField] private CharacterDatabase characterDatabase;
    [SerializeField] private PlayerBoardSlotUI[] slots;

    public static PlayerBoardHub Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
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
        RebindAllFromScene();
    }

    /// <summary>
    /// Gọi từ PlayerBoardState khi SyncVar identity đã replicate (mọi client).
    /// Không phụ thuộc ObserversRpc từ spawner.
    /// </summary>
    public void RegisterBoardState(PlayerBoardState state)
    {
        if (state == null || slots == null)
            return;

        int index = state.SlotIndex;

        if (!FlowGuard.IsValidSlotIndex(index, slots.Length, out string reason))
        {
            FlowGuard.Error(FlowGuard.TagHud, reason, this);
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
        if (slots == null)
            return;

        PlayerBoardState[] states = FindObjectsByType<PlayerBoardState>(FindObjectsSortMode.None);

        for (int i = 0; i < slots.Length; i++)
        {
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
        if (slots == null || !FlowGuard.IsValidSlotIndex(slotIndex, slots.Length, out _))
            return;

        slots[slotIndex].SetEmpty();
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
}
