public enum CharacterSelectMode
{
    Play,
    Lobby
}

public static class GameSession
{
    public static CharacterSelectMode CharacterSelectMode = CharacterSelectMode.Play;

    public static string RoomName = "BM-0000";
    public static string MapName = "Classic Garden";
    public static int MaxPlayers = 4;

    public static int SelectedCharacterIndex;
    public static string SelectedCharacterName = "Player";
    public static int SelectedCharacterHp = 3;
    public static int SelectedCharacterBomb = 1;
    public static int SelectedCharacterSpeed = 60;

    public static void SetSelectedCharacter(
        int index,
        string characterName,
        int hp,
        int bomb,
        int speed
    )
    {
        SelectedCharacterIndex = index;
        SelectedCharacterName = characterName;
        SelectedCharacterHp = hp;
        SelectedCharacterBomb = bomb;
        SelectedCharacterSpeed = speed;

        UnityEngine.PlayerPrefs.SetInt("SelectedCharacterIndex", index);
        UnityEngine.PlayerPrefs.SetString("SelectedCharacterName", characterName);
        UnityEngine.PlayerPrefs.SetInt("SelectedCharacterHp", hp);
        UnityEngine.PlayerPrefs.SetInt("SelectedCharacterBomb", bomb);
        UnityEngine.PlayerPrefs.SetInt("SelectedCharacterSpeed", speed);
        UnityEngine.PlayerPrefs.Save();
    }

    public static void SetRoom(string roomName, string mapName, int maxPlayers)
    {
        RoomName = roomName;
        MapName = mapName;
        MaxPlayers = maxPlayers;
    }
}
