using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;  
public class MovementController : MonoBehaviour
{
    public new Rigidbody2D rigidbody;
    
    [SerializeField] private float speed = 5f;  
    Vector2[] directions = new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
    Vector2 currentDirection = Vector2.zero;    
    void Start()
    {
        this.rigidbody = GetComponent<Rigidbody2D>(); 
    }

    // Update is called once per frame
    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return; 
        currentDirection = getDirection(keyboard);  
    }
    void FixedUpdate()
    {
        Vector2 position = rigidbody.position;
        Vector2 traslation = currentDirection * speed * Time.fixedDeltaTime;

        rigidbody.MovePosition(position + traslation); 
    }
    Vector2 getDirection(Keyboard keyboard) 
    {
        switch (true) 
        {
            case true when keyboard.wKey.isPressed:
                return directions[0];
            case true when keyboard.sKey.isPressed:
                return directions[1];
            case true when keyboard.aKey.isPressed:
                return directions[2];
            case true when keyboard.dKey.isPressed:
                return directions[3];
            default:
                return Vector2.zero;
        }
    }
}
