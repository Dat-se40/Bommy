using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class BombController : MonoBehaviour
{
    [SerializeField] GameObject bombPrefab;
    // Bomb settings
    [SerializeField] readonly float bombFuseTime = 3f;
    [SerializeField] readonly int bombAmount = 3;
    [SerializeField] int bombRemaining;
    [SerializeField] readonly float bombChargeTime = 3f; 
    [SerializeField] float bombCurrentChargeTime = 0f;
    [SerializeField] MovementController movementController;
    private void OnEnable()
    {
        bombRemaining = bombAmount;
        movementController = GetComponent<MovementController>();
    }
    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return; 
        bombCurrentChargeTime += Time.deltaTime;    
        if(bombCurrentChargeTime >= bombChargeTime) 
        {
            bombCurrentChargeTime = 0f;
            bombRemaining = bombRemaining >= bombAmount ? bombAmount : bombRemaining + 1;  
            Debug.Log($"Bomb recharged! Remaining bombs: {bombRemaining}"); 
        }
        // Khi di chuyển ko được đặt bom
        if (keyboard.spaceKey.wasPressedThisFrame && bombRemaining > 0 && !movementController.isMoving) 
        {
            StartCoroutine(PlaceBomb());
        }
    }

    public IEnumerator PlaceBomb() 
    {
        GameObject bomb = Instantiate(bombPrefab, transform.position, Quaternion.identity);
        bombRemaining--;
       
        yield return new WaitForSeconds(bombFuseTime);
        // fuse time is over, bomb explodes
        Destroy(bomb);  
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Bomb"))
        {
            Debug.Log("Player exited bomb area, making it solid");
            collision.isTrigger = false;
        }
    }
}
