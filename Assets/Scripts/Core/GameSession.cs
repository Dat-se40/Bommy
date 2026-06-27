public enum CharacterSelectMode
{
    Play,
    Lobby
}

public static class GameSession
{
    public static CharacterSelectMode CharacterSelectMode = CharacterSelectMode.Play;

    #region Share data  from Host
    public static string RoomId = "BM-0000";
    public static string RoomName = "BM-0000";
    public static string MapName = "Classic Garden";
    public static int MapId = LobbyApiContracts.DefaultMapId; 
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
        RoomId = string.IsNullOrWhiteSpace(roomName) ? "BM-0000" : roomName;
        RoomName = roomName;
        MapId = roomId;
        MaxPlayers = maxPlayers;
        MapName = mapName;  
    }

    public static void SetRoom(string roomId, string roomName, int mapId, int maxPlayers, string mapName)
    {
        RoomId = string.IsNullOrWhiteSpace(roomId) ? "BM-0000" : roomId;
        RoomName = string.IsNullOrWhiteSpace(roomName) ? RoomId : roomName;
        MapId = mapId;
        MaxPlayers = maxPlayers;
        MapName = string.IsNullOrWhiteSpace(mapName) ? "Unknown Map" : mapName;
    }

    // Trong quá trình connect thì người chơi nhận thông tin lại 1 lần nữa
    public static void GetShareDataFromServer(string roomName, int roomId, int maxPlayers, string mapName) 
    {
        RoomId = string.IsNullOrWhiteSpace(roomName) ? "BM-0000" : roomName;
        RoomName = roomName;
        MapId = roomId;
        MaxPlayers = maxPlayers;
        MapName = mapName;
    }
    public static void Reset()
    {
        CharacterSelectMode = CharacterSelectMode.Play;
        RoomId = "BM-0000";
        RoomName = "BM-0000";
        MapName = "Classic Garden";
        MapId = LobbyApiContracts.DefaultMapId; 
        MaxPlayers = 4;
        SelectedCharacterIndex = 0;
        SelectedCharacterName = "Player";
        SelectedCharacterHp = 3;
        SelectedCharacterBomb = 1;
        SelectedCharacterSpeed = 60;
    }
}
