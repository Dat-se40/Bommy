using PurrNet;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] public PlayerInfor playerInfor; 

    protected override void OnSpawned()
    {
        base.OnSpawned();
        RoomManager.Instance.ConnectPlayerRpc(playerInfor);
    }

    protected override void OnDespawned()
    {
        base.OnDespawned();
        RoomManager.Instance.DisconnectPlayer(playerInfor);
    }
}