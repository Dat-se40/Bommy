using UnityEngine;

[CreateAssetMenu(menuName = "Level/Level Config")]
public class LevelConfig : ScriptableObject
{
    public string levelId;

    public string displayName;

    [Header("Destructible Drops")]
    public LevelDropSettings destructibleDrops;
}
