using UnityEngine;
using UnityEngine.U2D.Animation;

[CreateAssetMenu(
    fileName = "CharacterDefinition",
    menuName = "Bommy/Character/Character Definition"
)]
public class CharacterDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private int characterId;
    [SerializeField] private string characterName;

    [TextArea]
    [SerializeField] private string description;

    [Header("Visual")]
    [SerializeField] private Sprite icon;
    [SerializeField] private Sprite preview;
    [SerializeField] private SpriteLibraryAsset spriteLibrary;

    [Header("Gameplay Prefab")]
    [SerializeField] private GameObject playerPrefab;

    [Header("Stats")]
    [Range(1, 5)]
    [SerializeField] private int hp = 3;

    [Range(1, 5)]
    [SerializeField] private int bomb = 1;

    [Range(1, 100)]
    [SerializeField] private int speed = 60;

    [Header("Shop")]
    [SerializeField] private bool defaultOwned;
    [SerializeField] private int requiredLevel = 1;
    [SerializeField] private int price;

    public int CharacterId => characterId;
    public string CharacterName => characterName;
    public string Description => description;

    public Sprite Icon => icon;
    public Sprite Preview => preview != null ? preview : icon;
    public SpriteLibraryAsset SpriteLibrary => spriteLibrary;
    public GameObject PlayerPrefab => playerPrefab;

    public int Hp => hp;
    public int Bomb => bomb;
    public int Speed => speed;

    public bool DefaultOwned => defaultOwned;
    public int RequiredLevel => requiredLevel;
    public int Price => price;
}
