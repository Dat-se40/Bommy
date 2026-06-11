using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class CursorUI : MonoBehaviour
{
    private RectTransform _cursorTransform; 
    private Canvas _parentCavans; 
    private RectTransform _canvasRectTransform; 
    [SerializeField] private Camera _canvasCamera;
    [SerializeField] private InputActionReference _pointerPositionAction; 
    [SerializeField] private GameObject _normalCursor;
    [SerializeField] private GameObject _selectorCursor;
    [SerializeField] private GameObject player; 
    private Vector2 tileOffset = new Vector2(0.5f, 0.5f);
    bool isSelectorMode = false;    

    private void Awake()
    {
        _cursorTransform = GetComponent<RectTransform>();
        _parentCavans = GetComponentInParent<Canvas>();
        if(!_parentCavans)
        {
            Debug.LogError("CursorUI: No parent canvas found.");
            return;
        }
        _canvasRectTransform = _parentCavans.GetComponent<RectTransform>();
      //  _canvasCamera = _parentCavans.renderMode == RenderMode.ScreenSpaceCamera ? _parentCavans.worldCamera : null;    
        _selectorCursor.SetActive(isSelectorMode);
    }
    public void Update()
    {
        var keyword = Keyboard.current; 
        if (keyword.qKey.isPressed) 
        {
            QuickSwitchMode();
        }else if (keyword.altKey.isPressed) 
        {
            UseDefaultCursor();
        }
    }
    public void OnEnable()
    {
        Cursor.visible = false; 
        if (_pointerPositionAction != null)
        {
            _pointerPositionAction.action.Enable();
            _pointerPositionAction.action.performed += OnPointerPositionChanged;
        }
    }   
    public void OnDisable()
    {
        Cursor.visible = true; 
        if (_pointerPositionAction != null)
        {
            _pointerPositionAction.action.performed -= OnPointerPositionChanged;
            _pointerPositionAction.action.Disable();
        }
    }
    private void OnPointerPositionChanged(InputAction.CallbackContext ctx   )
    {
        if (_cursorTransform == null || _canvasRectTransform == null)
        {
            Debug.LogError("CursorUI: Missing required components.");
            return;
        }
        Vector2 mousePosition = ctx.ReadValue<Vector2>();
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRectTransform, mousePosition, _canvasCamera, out var localPoint))
        {
            if (isSelectorMode)
            {
                localPoint = SnapToGrid(localPoint);
            }
            _cursorTransform.anchoredPosition = localPoint;
        }
    }
    Vector2 SnapToGrid(Vector2 pos)
    {
        return new Vector2(
            Mathf.Floor(pos.x) + tileOffset.x,
            Mathf.Floor(pos.y) + tileOffset.y
        );
    }
    public void QuickSwitchMode() 
    {
        isSelectorMode = !isSelectorMode;   
        _selectorCursor.SetActive(isSelectorMode);  
        _normalCursor.SetActive(!isSelectorMode);
        Cursor.visible = false;
    }
    public void UseDefaultCursor() 
    {
        isSelectorMode = false;   
        _selectorCursor.SetActive(false);  
        _normalCursor.SetActive(false);
        Cursor.visible = true;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (isSelectorMode)
        {
            Gizmos.DrawWireSphere(_canvasCamera.transform.position, 2);
        }
    }
#endif
}
