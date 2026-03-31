using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;  
public class MovementController : MonoBehaviour
{
    public new Rigidbody2D rigidbody;
    public Animator animator;
    [SerializeField] private float speed = 5f;  
    Vector2[] directions = new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
    Vector2 currentDirection = Vector2.zero;    
    void Start()
    {
        this.rigidbody = GetComponent<Rigidbody2D>(); 
        this.animator = GetComponent<Animator>();   
    }

    // Update is called once per frame
    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return; 
        currentDirection = getDirectionByKey(keyboard);  
        UpdateAnimation(currentDirection);
    }
    void FixedUpdate()
    {
        Vector2 position = rigidbody.position;
        Vector2 traslation = currentDirection * speed * Time.fixedDeltaTime;

        rigidbody.MovePosition(position + traslation); 
    }
    Vector2 getDirectionByKey(Keyboard keyboard) 
    {
        switch (true) 
        {
            case true when keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed:
                return directions[0];
            case true when keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed:
                return directions[1];
            case true when keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed:
                return directions[2];
            case true when keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed:
                return directions[3];
            default:
                return Vector2.zero;
        }
    }
    void UpdateAnimation(Vector2 direction) 
    {
        switch (true) 
        {
            case true when direction == directions[0]:
                animator.SetTrigger("Go Up");
                break;
            case true when direction == directions[1]:
                animator.SetTrigger("Go Down");
                break;
            case true when direction == directions[2]:
                animator.SetTrigger("Turn Left");
                break;
            case true when direction == directions[3]:
                animator.SetTrigger("Turn Right");
                break;
            default:
                animator.SetTrigger("Idle");    
                break;
        }   
    }
}
