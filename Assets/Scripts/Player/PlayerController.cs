using PurrNet;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] public PlayerInfor playerInfor; 
    [SerializeField] public float distance = 4;
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
    public void Update()
    {
        if (!isOwner) return;
        // Các logic điều khiển chung của player sẽ được đặt ở đây
    
    }
}