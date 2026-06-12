using UnityEngine;

/// <summary>
/// Gán mỗi PlayerBoardSlotUI với PlayerBoardState tương ứng (theo slotIndex).
/// </summary>
public class PlayerBoardHub : MonoBehaviour
{
    [SerializeField] private CharacterDatabase characterDatabase;
    [SerializeField] private PlayerBoardSlotUI[] slots;

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

    void RebindAllFromScene()
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
