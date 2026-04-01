using UnityEngine;
using UnityEngine.InputSystem;

public class MovementController : MonoBehaviour
{
    public Rigidbody2D rd;
    public Animator animator;
    [SerializeField] private float speed = 5f;

    private Vector2 tileOffset = new Vector2(0.5f, 0.5f);

    private Vector2 targetPosition;
    public bool isMoving { get; private set; }

    void Start()
    {
        rd = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        // Snap vị trí ban đầu vào tâm tile gần nhất
        targetPosition = SnapToGrid(rd.position);
        rd.position = targetPosition;
    }

    void Update()
    {
        if (isMoving) return; // Đang di chuyển thì không nhận input

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        Vector2 dir = GetDirectionByKey(keyboard);
        if (dir == Vector2.zero)
        {
            UpdateAnimation(Vector2.zero);
            return;
        }

        Vector2 next = targetPosition + dir;

        if (IsBlocked(next)) return;

        targetPosition = next;
        isMoving = true;
        UpdateAnimation(dir);
    }

    void FixedUpdate()
    {
        if (!isMoving) return;

        Vector2 newPos = Vector2.MoveTowards(rd.position, targetPosition, speed * Time.fixedDeltaTime);
        rd.MovePosition(newPos);

        // Đến nơi thì dừng
        if (Vector2.Distance(newPos, targetPosition) < 0.001f)
        {
            rd.MovePosition(targetPosition); // Snap chính xác
            isMoving = false;
        }
    }

    // Snap về tâm tile gần nhất
    Vector2 SnapToGrid(Vector2 pos)
    {
        return new Vector2(
            Mathf.Floor(pos.x) + tileOffset.x,
            Mathf.Floor(pos.y) + tileOffset.y
        );
    }

    Vector2 GetDirectionByKey(Keyboard keyboard)
    {
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) return Vector2.up;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) return Vector2.down;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) return Vector2.left;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) return Vector2.right;
        return Vector2.zero;
    }

    void UpdateAnimation(Vector2 direction)
    {
        animator.SetBool("Go Up", direction == Vector2.up);
        animator.SetBool("Go Down", direction == Vector2.down);
        animator.SetBool("Go Left", direction == Vector2.left);
        animator.SetBool("Go Right", direction == Vector2.right);
        animator.SetBool("Idle", direction == Vector2.zero);
    }
    public bool IsBlocked(Vector2 position)
    {
        Collider2D hit = Physics2D.OverlapCircle(position, 0.1f, LayerMask.GetMask("Block"));
        return hit != null;
    }
}