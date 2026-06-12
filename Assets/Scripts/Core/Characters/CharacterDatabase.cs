using UnityEngine;

[CreateAssetMenu(
    fileName = "CharacterDatabase",
    menuName = "Bommy/Character/Character Database"
)]
public class CharacterDatabase : ScriptableObject
{
    [SerializeField] private CharacterDefinition[] characters;

    public CharacterDefinition[] Characters => characters;
    public int Count => characters != null ? characters.Length : 0;

    public CharacterDefinition GetByIndex(int index)
    {
        if (characters == null || index < 0 || index >= characters.Length)
            return null;

        return characters[index];
    }

    public CharacterDefinition GetById(int characterId)
    {
        if (characters == null)
            return null;

        for (int i = 0; i < characters.Length; i++)
        {
            CharacterDefinition character = characters[i];

            if (character != null && character.CharacterId == characterId)
                return character;
        }

        return null;
    }

    public int GetIndexById(int characterId)
    {
        if (characters == null)
            return -1;

        for (int i = 0; i < characters.Length; i++)
        {
            CharacterDefinition character = characters[i];

            if (character != null && character.CharacterId == characterId)
                return i;
        }

        return -1;
    }

    public bool ContainsId(int characterId)
    {
        return GetById(characterId) != null;
    }
}
