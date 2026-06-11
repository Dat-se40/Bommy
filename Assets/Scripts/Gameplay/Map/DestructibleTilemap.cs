using UnityEngine;
using UnityEngine.Tilemaps;

public class DestructibleTilemap : MonoBehaviour
{
    #region Fields

    [SerializeField] private Tilemap tilemap;

    #endregion

    #region Unity

    private void Awake()
    {
        if (tilemap == null)
            tilemap = GetComponent<Tilemap>();
    }

    #endregion

    #region Public

    public bool HasTile(Vector3Int cell)
    {
        return tilemap != null && tilemap.HasTile(cell);
    }

    public void DestroyTile(Vector3Int cell)
    {
        if (tilemap == null)
            return;

        if (!tilemap.HasTile(cell))
            return;

        tilemap.SetTile(cell, null);
    }

    #endregion
}
