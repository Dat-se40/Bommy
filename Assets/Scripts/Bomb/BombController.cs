using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Tilemaps;

// BombController.cs
public class BombController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] GameObject bombPrefab;
    [SerializeField] ExplosionCreator explosionCreator;

    [SerializeField] float bombFuseTime = 3f;
    [SerializeField] int bombAmount = 3;
    [SerializeField] float bombChargeTime = 3f;

    private int bombRemaining;
    private float bombCurrentChargeTime;
    private MovementController movementController;

    private void OnEnable()
    {
        bombRemaining = bombAmount;
        movementController = GetComponent<MovementController>();
        explosionCreator.eventComplete += OnExplosionComplete;
    }

    private void OnDisable()
    {
        explosionCreator.eventComplete -= OnExplosionComplete;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        bombCurrentChargeTime += Time.deltaTime;
        if (bombCurrentChargeTime >= bombChargeTime)
        {
            bombCurrentChargeTime = 0f;
            bombRemaining = Mathf.Min(bombRemaining + 1, bombAmount);
        }

        if (keyboard.spaceKey.wasPressedThisFrame && bombRemaining > 0 && !movementController.isMoving)
            StartCoroutine(PlaceBomb(transform.position));
    }

    private IEnumerator PlaceBomb(Vector2 position)
    {
        bombRemaining--;
        GameObject bomb = Instantiate(bombPrefab, position, Quaternion.identity);

        yield return new WaitForSeconds(bombFuseTime);

        Destroy(bomb);
        StartExplosion(position);
    }

    private void StartExplosion(Vector2 position)
    {
        // BombController không biết gì về Explosion — chỉ ra lệnh tạo
        explosionCreator.CreateOnDirection(position, Vector2.up);
        explosionCreator.CreateOnDirection(position, Vector2.down);
        explosionCreator.CreateOnDirection(position, Vector2.left);
        explosionCreator.CreateOnDirection(position, Vector2.right);
    }

    private void OnExplosionComplete()
    {
        Debug.Log("Explosion fully complete");
        // Có thể raise event, unlock bomb slot, v.v.
    }
    
}