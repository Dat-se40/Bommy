using System.Collections;
using System.Collections.Generic;
using PurrNet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Chuyển về MonoBehaviour vì đây thuần túy là code xử lý giao diện
public class PlayerInforUI : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI textName;

    [SerializeField]
    Image avt;

    [SerializeField]
    Image hp;

    public int index;
    public bool isSet { get; private set; } = false;
    public string ownerName { get; private set; } = string.Empty;

    public void SetUI(PlayerInfor? playerInfor)
    {
        if (playerInfor == null)
        {
            isSet = false;
            ownerName = string.Empty;
        }
        else
        {
            isSet = true;
            //textName.text = playerInfor.;
            //ownerName = playerInfor.name;
            textName.text = playerInfor?.playerName;
            ownerName = playerInfor?.playerName;
        }
        this.gameObject.SetActive(isSet);
    }
}
