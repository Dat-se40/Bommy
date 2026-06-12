using PurrNet;
using System;
using System.Linq;
using UnityEngine;

public class NetworkPlayerSpawner : NetworkBehaviour
{
    [SerializeField] GameObject playerPrefabs;
    [SerializeField] ExplosionCreator explosionCreator;
    [SerializeField] Grid grid;
    [SerializeField] Transform[] spawnPoints;
    [SerializeField] GameObject players; 
    SyncList<PlayerController> playerControllers = new SyncList<PlayerController>();
    protected override void OnSpawned()
    {
        base.OnSpawned();
        Debug.Log("Player Spawned is running");    
        networkManager.onPlayerJoined += SpawnPlayer;
        spawnPoints = grid.transform.Find("Map").GetComponent<MapRefs>().SpawnPoints;
    }

    private void SpawnPlayer(PlayerID player, bool isReconnect, bool asServer)
    {
        if (!asServer) return;
        // Sẽ có 1 hàm để tính toán các vị trí có thể spawn dựa trên số lượng player hiện tại, ở đây tạm thời sẽ spawn ở vị trí đầu tiên
        int playerCount = playerControllers.Count;
        var newPlayer = Instantiate(playerPrefabs,spawnPoints[playerCount].position,Quaternion.identity);
        newPlayer.transform.SetParent(players.transform);

        if (newPlayer.TryGetComponent<PlayerController>(out var playerController))
        {
            Debug.Log($"PlayerController component {player.id} found and added to the list.");
            playerController.GiveOwnership(player);
            networkManager.Spawn(newPlayer);
            playerControllers.Add(playerController);
        }
    }
    
}
