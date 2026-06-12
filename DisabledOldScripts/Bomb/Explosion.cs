using PurrNet;
using UnityEngine;
using UnityEngine.Tilemaps;

// Visual only — MonoBehaviour, không cần NetworkBehaviour
// Server Instantiate → PurrNet tự sync sang client
public class Explosion : MonoBehaviour, IHitableSkill
{
    [SerializeField] AnimationClip clip;
    public float clipLength => clip != null ? clip.length : 1f;

    // Gọi từ Animation Event — keyframe cuối
    public void OnAnimationEnd()
    {
        Destroy(gameObject);
    }

    // Gọi từ server nếu cần xử lý hit (optional, vì ExplosionCreator đã xử lý)
    public void HandleHitObject()
    {
        Collider2D col = Physics2D.OverlapCircle(transform.position, 0.1f);
        if (col == null) return;

        int layer = col.gameObject.layer;

        if (layer == LayerMask.NameToLayer("Destructibles"))
        {
            Tilemap tilemap = col.GetComponent<Tilemap>();
            if (tilemap == null) return;
            Vector3Int cell = tilemap.WorldToCell(transform.position);
            if (tilemap.HasTile(cell)) tilemap.SetTile(cell, null);
        }else  if (layer == LayerMask.NameToLayer("Player"))
        {
            Debug.Log("Đánh trúng player");
        }
    }
    //[ServerRpc(requireOwnership:false)]
    //public void HandleHitPlayer( GameObject gameObject, RPCInfo rPCInfo = default) 
    //{
    //    string name = gameObject.GetComponent<PlayerController>().playerInfor.playerName;

    //  //  Debug.Log($"Player {rPCInfo.}")
    //}
}