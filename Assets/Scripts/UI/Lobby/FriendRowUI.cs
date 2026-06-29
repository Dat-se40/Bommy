using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// Hiển thị một dòng friend trong tab FRIENDS.
/// </summary>
public class FriendRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text friendNamelbl;
    [SerializeField] private Image steamIcon;
    [SerializeField] private TMP_Text friendIdlbl;
    [SerializeField] private TMP_Text friendStatuslbl;
    [SerializeField] private Button invitebtn;
    [SerializeField] private Button joinbtn;

    private string friendId;
    private Action<string> onInviteClicked;
    private Action<string> onJoinRoomClicked;

    public void Setup(
        string id,
        string displayName,
        string username,
        bool online,
        bool isSteamFriend,
        string currentRoomId,
        Action<string> inviteCallback,
        Action<string> joinRoomCallback
    )
    {
        friendId = id;
        onInviteClicked = inviteCallback;
        onJoinRoomClicked = joinRoomCallback;

        if (friendNamelbl != null)
            friendNamelbl.text = displayName;

        if (steamIcon != null)
            steamIcon.gameObject.SetActive(isSteamFriend);

        if (friendIdlbl != null)
        {
            if (id.StartsWith("steam:", StringComparison.OrdinalIgnoreCase))
            {
                friendIdlbl.text = "Steam Friend";
            }
            else if (!string.IsNullOrEmpty(username))
            {
                friendIdlbl.text = "ID: " + username;
            }
            else
            {
                friendIdlbl.text = "ID: " + (id.Length > 8 ? id.Substring(0, 8) + "..." : id);
            }
        }

        if (friendStatuslbl != null)
        {
            if (!online)
                friendStatuslbl.text = "OFFLINE";
            else if (!string.IsNullOrEmpty(currentRoomId))
                friendStatuslbl.text = "IN ROOM";
            else
                friendStatuslbl.text = "ONLINE";
        }

        if (invitebtn != null)
        {
            ButtonClickSound.EnsureOn(invitebtn);
            invitebtn.interactable = online;
            invitebtn.onClick.RemoveAllListeners();
            invitebtn.onClick.AddListener(OnInviteClicked);
        }

        if (joinbtn != null)
        {
            ButtonClickSound.EnsureOn(joinbtn);
            joinbtn.interactable = online && !string.IsNullOrEmpty(currentRoomId);
            joinbtn.onClick.RemoveAllListeners();
            joinbtn.onClick.AddListener(OnJoinRoomClicked);
        }
    }

    private void OnInviteClicked()
    {
        onInviteClicked?.Invoke(friendId);
    }

    private void OnJoinRoomClicked()
    {
        onJoinRoomClicked?.Invoke(friendId);
    }

    [ContextMenu("Auto Bind UI From Children")]
    private void AutoBindUIFromChildren()
    {
        UIAutoBindUtility.RecordUndo(this, "Auto Bind FriendRowUI");

        friendNamelbl = UIAutoBindUtility.FindChildComponent<TMP_Text>(
            this,
            "FriendNamelbl",
            "FriendNameLbl",
            "Namelbl"
        );

        steamIcon = UIAutoBindUtility.FindChildComponent<Image>(
            this,
            "SteamIcon",
            "FriendSteamIcon"
        );

        friendIdlbl = UIAutoBindUtility.FindChildComponent<TMP_Text>(
            this,
            "FriendIdlbl",
            "FriendIDlbl",
            "Idlbl"
        );

        friendStatuslbl = UIAutoBindUtility.FindChildComponent<TMP_Text>(
            this,
            "FriendStatuslbl",
            "Statuslbl"
        );

        invitebtn = UIAutoBindUtility.FindChildComponent<Button>(
            this,
            "Invitebtn"
        );

        joinbtn = UIAutoBindUtility.FindChildComponent<Button>(
            this,
            "Joinbtn",
            "JoinFriendRoombtn",
            "JoinRoombtn"
        );

        UIAutoBindUtility.LogBindResult(
            this,
            "Auto Bind FriendRowUI: " + gameObject.name,
            new BindLogItem("FriendNamelbl", friendNamelbl),
            new BindLogItem("SteamIcon", steamIcon),
            new BindLogItem("FriendIdlbl", friendIdlbl),
            new BindLogItem("FriendStatuslbl", friendStatuslbl),
            new BindLogItem("Invitebtn", invitebtn),
            new BindLogItem("Joinbtn", joinbtn)
        );

        UIAutoBindUtility.SetDirty(this);
    }

}
