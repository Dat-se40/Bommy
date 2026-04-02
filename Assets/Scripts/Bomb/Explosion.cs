using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEditor.PlayerSettings;
using static UnityEngine.Rendering.DebugUI.Table;

public class Explosion : MonoBehaviour
{
    public event Action explodedEvent;
    public bool hasExploded { get; private set; }

    [SerializeField] AnimationClip clip;

    public void OnAnimationEnd()
    {
        hasExploded = true;
        explodedEvent?.Invoke();
        gameObject.SetActive(false);
    }

    public void HandleHitObject()
    {
        Collider2D col = Physics2D.OverlapCircle(transform.position, 0.1f);
        if (col == null) return;

        int layer = col.gameObject.layer;

        if (layer == LayerMask.NameToLayer("Player"))
        {
            Debug.Log("Player hit!");
            // GameSystem.Raise<PlayerDieEvent>()
        }
        else if (layer == LayerMask.NameToLayer("Destructibles"))
        {
            Tilemap tilemap = col.GetComponent<Tilemap>();
            if (tilemap == null) return;
            Vector3Int cell = tilemap.WorldToCell(transform.position);
            if (tilemap.HasTile(cell)) tilemap.SetTile(cell, null);
        }
    }

    public float clipLength => clip != null ? clip.length : 1f;
}