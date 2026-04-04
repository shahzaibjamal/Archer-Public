using UnityEngine;
using UnityEngine.EventSystems;

public class FloatingJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("UI Rects")]
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;
    [SerializeField] private CanvasGroup canvasGroup;
    
    private Vector2 _inputVector = Vector2.zero;
    private Canvas _parentCanvas;

    public Vector2 Direction => _inputVector;

    private void Start()
    {
        _parentCanvas = GetComponentInParent<Canvas>();
        HideJoystick();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        ShowJoystick();

        // Convert the universal screen press exactly into the root canvas local space!
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _parentCanvas.transform as RectTransform, 
            eventData.position, 
            eventData.pressEventCamera, 
            out Vector2 localPoint
        );

        // Snap the background perfectly flush with the finger
        background.anchoredPosition = localPoint;
        
        // Immediately trigger drag physics so it updates identically on frame 1
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, 
            eventData.position, 
            eventData.pressEventCamera, 
            out Vector2 position))
        {
            Vector2 sizeDelta = background.sizeDelta;

            // Normalize coordinate data relative exactly mathematically to the visual center!
            position.x = (position.x / sizeDelta.x) * 2;
            position.y = (position.y / sizeDelta.y) * 2;

            _inputVector = new Vector2(position.x, position.y);
            if (_inputVector.magnitude > 1f)
            {
                _inputVector = _inputVector.normalized;
            }

            // Offset the inner knob (Handle) physically keeping it bound mathematically securely inside the ring limit
            handle.anchoredPosition = new Vector2(
                _inputVector.x * (sizeDelta.x / 2), 
                _inputVector.y * (sizeDelta.y / 2)
            );
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _inputVector = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
        HideJoystick();
    }

    private void ShowJoystick()
    {
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        background.gameObject.SetActive(true);
    }

    private void HideJoystick()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        background.gameObject.SetActive(false);
    }
}
