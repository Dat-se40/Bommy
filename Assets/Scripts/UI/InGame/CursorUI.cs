using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class CursorUI : MonoBehaviour
{
    private RectTransform _cursorTransform; 
    private Canvas _parentCavans; 
    private RectTransform _canvasRectTransform;     
    [Header("Cursor GameObjects")]  
    [SerializeField] private GameObject _normalCursor;
    [SerializeField] private GameObject _selectorCursor;
    [SerializeField] private Camera _canvasCamera;
    [Header("Keyboard Actions")]
    [SerializeField] private InputActionReference _quickSwitchAction;
    [SerializeField] private InputActionReference _defaultCursorAction;
    [SerializeField] private InputActionReference _pointerPositionAction;
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
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null) 
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 9999; 
        }
        
    }
    private void OnDefaultCursor(InputAction.CallbackContext context)
    {
        UseDefaultCursor();
    }

    private void OnQuickSwitch(InputAction.CallbackContext context)
    {
        QuickSwitchMode();
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
            
            _cursorTransform.anchoredPosition = localPoint;
        }
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
    public Vector3 CurrentMouseWorldPosition
    {
        get
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();

            Vector3 worldPos =
                Camera.main.ScreenToWorldPoint(
                    new Vector3(mousePos.x, mousePos.y,
                    Mathf.Abs(Camera.main.transform.position.z)));

            worldPos.z = 0;

            return worldPos;
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
        if (_quickSwitchAction != null)
        {
            _quickSwitchAction.action.Enable();
            _quickSwitchAction.action.performed += OnQuickSwitch; // Khi nhấn phím, gọi hàm OnQuickSwitch
        }

        // Đăng ký sự kiện cho phím Alt
        if (_defaultCursorAction != null)
        {
            _defaultCursorAction.action.Enable();
            _defaultCursorAction.action.performed += OnDefaultCursor; // Khi nhấn phím, gọi hàm OnDefaultCursor
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
        if (_quickSwitchAction != null)
        {
            _quickSwitchAction.action.performed -= OnQuickSwitch;
            _quickSwitchAction.action.Disable();
        }

        if (_defaultCursorAction != null)
        {
            _defaultCursorAction.action.performed -= OnDefaultCursor;
            _defaultCursorAction.action.Disable();
        }
    }
}
