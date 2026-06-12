using PurrNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;


// Quản lí các thông tin người chơi trong phòng, hiển thị UI các thứ
public class RoomManager : NetworkBehaviour
{
    public static RoomManager Instance;

    [Header("UI References")]
    [SerializeField] private List<PlayerInforUI> playerInforUIs;
    [SerializeField] private TextMeshProUGUI timeSpan;

    private SyncList<PlayerInfor> playerConnected = new SyncList<PlayerInfor>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
    }

    protected override void OnSpawned()
    {
        base.OnSpawned();
        playerConnected.onChanged += UpdateRoomUI;

        
        //UpdateRoomUI(playerConnected);
    }

    private void UpdateRoomUI(SyncListChange<PlayerInfor> change)
    {
        foreach (var item in playerInforUIs)
        {
            item.SetUI(null);
        }


        for (int i = 0; i < playerConnected.Count; i++)
        {
            if (i < playerInforUIs.Count)
            {
                playerInforUIs[i].SetUI(playerConnected[i]);
            }
        }
    }

    protected override void OnDespawned()
    {
        base.OnDespawned();
        playerConnected.onChanged -= UpdateRoomUI;
    }
    [ServerRpc(requireOwnership: false)]
    public void ConnectPlayerRpc(PlayerInfor playerInfor)
    {
        Debug.Log(playerInfor.playerName + " has been connected");
        playerConnected.Add(playerInfor);
    }

    public void DisconnectPlayer(PlayerInfor playerInfor)
    {
        DisconnectPlayerRpc(playerInfor);
    }

    [ServerRpc(requireOwnership: false)]
    private void DisconnectPlayerRpc(PlayerInfor playerInfor)
    {
        playerConnected.Remove(playerInfor);
    }
}