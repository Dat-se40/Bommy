public enum CharacterSelectMode
{
    Play,
    Lobby
}

public static class GameSession
{
    public static CharacterSelectMode CharacterSelectMode = CharacterSelectMode.Play;

    #region Share data  from Host
    public static string RoomName = "BM-0000";
    public static string MapName = "Classic Garden";
    public static int MapId = LobbyBrowserUIController.DEFAULT_MAP_ID; 
    public static int MaxPlayers = 4;
    #endregion
    #region Private data
    public static int SelectedCharacterIndex;
    public static string SelectedCharacterName = "Player";
    public static int SelectedCharacterHp = 3;
    public static int SelectedCharacterBomb = 1;
    public static int SelectedCharacterSpeed = 60;
    #endregion
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
    }

    public static void SetRoom(string roomName, int roomId, int maxPlayers, string mapName)
    {
        RoomName = roomName;
        MapId = roomId;
        MaxPlayers = maxPlayers;
        MapName = mapName;  
    }
    // Trong quá trình connect thì người chơi nhận thông tin lại 1 lần nữa
    public static void GetShareDataFromServer(string roomName, int roomId, int maxPlayers, string mapName) 
    {
        RoomName = roomName;
        MapId = roomId;
        MaxPlayers = maxPlayers;
        MapName = mapName;
    }
    public static void Reset()
    {
        CharacterSelectMode = CharacterSelectMode.Play;
        RoomName = "BM-0000";
        MapName = "Classic Garden";
        MapId = 1; 
        MaxPlayers = 4;
        SelectedCharacterIndex = 0;
        SelectedCharacterName = "Player";
        SelectedCharacterHp = 3;
        SelectedCharacterBomb = 1;
        SelectedCharacterSpeed = 60;
    }
}
