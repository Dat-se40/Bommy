using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hiển thị một lời mời kết bạn trong tab REQUESTS.
/// </summary>
public class FriendRequestRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text requestNamelbl;
    [SerializeField] private Image steamIcon;
    [SerializeField] private TMP_Text requestIdlbl;
    [SerializeField] private Button acceptbtn;
    [SerializeField] private Button declinebtn;

    private string friendId;
    private Action<string> onAcceptClicked;
    private Action<string> onDeclineClicked;

    public void Setup(
        string id,
        string displayName,
        bool isSteamFriend,
        Action<string> acceptCallback,
        Action<string> declineCallback
    )
    {
        friendId = id;
        onAcceptClicked = acceptCallback;
        onDeclineClicked = declineCallback;

        if (requestNamelbl != null)
            requestNamelbl.text = displayName;

        if (steamIcon != null)
            steamIcon.gameObject.SetActive(isSteamFriend);

        if (requestIdlbl != null)
            requestIdlbl.text = "ID: " + id;

        if (acceptbtn != null)
        {
            ButtonClickSound.EnsureOn(acceptbtn);
            acceptbtn.onClick.RemoveAllListeners();
            acceptbtn.onClick.AddListener(OnAcceptClicked);
        }

        if (declinebtn != null)
        {
            ButtonClickSound.EnsureOn(declinebtn);
            declinebtn.onClick.RemoveAllListeners();
            declinebtn.onClick.AddListener(OnDeclineClicked);
        }
    }

    private void OnAcceptClicked()
    {
        onAcceptClicked?.Invoke(friendId);
    }

    private void OnDeclineClicked()
    {
        onDeclineClicked?.Invoke(friendId);
    }

    [ContextMenu("Auto Bind UI From Children")]
    private void AutoBindUIFromChildren()
    {
        UIAutoBindUtility.RecordUndo(this, "Auto Bind FriendRequestRowUI");

        requestNamelbl = UIAutoBindUtility.FindChildComponent<TMP_Text>(
            this,
            "RequestNamelbl",
            "RequestNameLbl",
            "FriendNamelbl",
            "Namelbl"
        );

        steamIcon = UIAutoBindUtility.FindChildComponent<Image>(
            this,
            "SteamIcon",
            "FriendSteamIcon"
        );

        requestIdlbl = UIAutoBindUtility.FindChildComponent<TMP_Text>(
            this,
            "RequestIdlbl",
            "RequestIDlbl",
            "FriendIdlbl",
            "Idlbl"
        );

        acceptbtn = UIAutoBindUtility.FindChildComponent<Button>(
            this,
            "Acceptbtn"
        );

        declinebtn = UIAutoBindUtility.FindChildComponent<Button>(
            this,
            "Declinebtn"
        );

        UIAutoBindUtility.LogBindResult(
            this,
            "Auto Bind FriendRequestRowUI: " + gameObject.name,
            new BindLogItem("RequestNamelbl", requestNamelbl),
            new BindLogItem("SteamIcon", steamIcon),
            new BindLogItem("RequestIdlbl", requestIdlbl),
            new BindLogItem("Acceptbtn", acceptbtn),
            new BindLogItem("Declinebtn", declinebtn)
        );

        UIAutoBindUtility.SetDirty(this);
    }

}
