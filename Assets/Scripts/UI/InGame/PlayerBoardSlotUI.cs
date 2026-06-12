using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Một slot board — theo dõi riêng PlayerBoardState được chỉ định.
/// </summary>
public class PlayerBoardSlotUI : MonoBehaviour
{
    [SerializeField] private int slotIndex;
    [SerializeField] private Image avatar;
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text hpLabel;
    [SerializeField] private TMP_Text livesLabel;
    [SerializeField] private TMP_Text scoreLabel;
    [SerializeField] private GameObject emptyState;
    [SerializeField] private GameObject localHighlight;

    PlayerBoardState trackedState;
    CharacterDatabase characterDatabase;

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

        if (localHighlight != null)
            localHighlight.SetActive(false);

        if (emptyState != null)
            emptyState.SetActive(true);

        if (nameLabel != null)
            nameLabel.text = "—";

        if (hpLabel != null)
            hpLabel.text = string.Empty;

        if (livesLabel != null)
            livesLabel.text = string.Empty;

        if (scoreLabel != null)
            scoreLabel.text = string.Empty;

        if (avatar != null)
            avatar.enabled = false;
        this.gameObject.SetActive(false);
    }

    void RefreshFromState()
    {
        if (trackedState == null)
        {
            SetEmpty();
            return;
        }
        this.gameObject.SetActive(true);   
        if (emptyState != null)
            emptyState.SetActive(false);

        CharacterDefinition definition = characterDatabase != null
            ? characterDatabase.GetById(trackedState.CharacterId)
            : MatchSessionBroker.ResolveDefinition(new PlayerMatchProfile
            {
                characterId = trackedState.CharacterId,
                catalogIndex = trackedState.CatalogIndex
            });

        if (nameLabel != null)
            nameLabel.text = trackedState.DisplayName;

        if (hpLabel != null)
            hpLabel.text = $"HP {trackedState.CurrentHp}/{trackedState.MaxHp}";

        if (livesLabel != null)
            livesLabel.text = $"♥ {trackedState.CurrentLives}";

        if (scoreLabel != null)
            scoreLabel.text = trackedState.Score.ToString();

        if (avatar != null)
        {
            Sprite sprite = definition != null ? definition.Icon : null;
            avatar.sprite = sprite;
            avatar.enabled = sprite != null;
        }

        if (trackedState.IsEliminated && nameLabel != null)
            nameLabel.text += " (OUT)";

    }

    void OnDisable()
    {
        Unsubscribe();
    }

    void Unsubscribe()
    {
        if (trackedState != null)
            trackedState.Changed -= RefreshFromState;
    }
}
