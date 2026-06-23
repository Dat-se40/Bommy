using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Một dòng trong Room List panel.
/// </summary>
public class LobbyRoomRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text roomNamelbl;
    [SerializeField] private TMP_Text roomIdlbl;
    [SerializeField] private TMP_Text playerCountlbl;
    [SerializeField] private TMP_Text maplbl;
    [SerializeField] private Button joinbtn;

    LobbyRoomDto room;
    Action<LobbyRoomDto> onJoinClicked;

    public void Setup(LobbyRoomDto entry, Action<LobbyRoomDto> joinCallback)
    {
        room = entry;
        onJoinClicked = joinCallback;

        if (roomNamelbl != null)
            roomNamelbl.text = entry.roomName;

        if (roomIdlbl != null)
            roomIdlbl.text = entry.roomId;

        if (playerCountlbl != null)
            playerCountlbl.text = entry.currentPlayers + "/" + entry.maxPlayers;

        if (maplbl != null)
            maplbl.text = entry.mapName;

        if (joinbtn != null)
        {
            joinbtn.onClick.RemoveAllListeners();
            joinbtn.onClick.AddListener(OnJoinClicked);
            joinbtn.interactable = entry.currentPlayers < entry.maxPlayers;
        }
    }

    void OnJoinClicked()
    {
        if (room != null)
            onJoinClicked?.Invoke(room);
    }

    [ContextMenu("Auto Bind From Children")]
    void AutoBindFromChildren()
    {
        if (roomNamelbl == null)
            roomNamelbl = FindChildText("RoomNamelbl");

        if (roomIdlbl == null)
            roomIdlbl = FindChildText("RoomIdlbl");

        if (playerCountlbl == null)
            playerCountlbl = FindChildText("PlayerCountlbl", "Players");

        if (maplbl == null)
            maplbl = FindChildText("Maplbl");

        if (joinbtn == null)
            joinbtn = GetComponentInChildren<Button>(true);
    }

    TMP_Text FindChildText(params string[] names)
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        for (int n = 0; n < names.Length; n++)
        {
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].name.Trim().StartsWith(names[n], StringComparison.OrdinalIgnoreCase))
                    return texts[i];
            }
        }

        return null;
    }
}
