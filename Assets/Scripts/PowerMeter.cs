using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PowerMeter : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("References")]
    public RectTransform cueOnMeter; 
    public RectTransform meterPanel; 
    public Drag dragController;

    [Header("Settings")]
    public float maxPower;
    public float maxPullDistance; 
    public float minPullDistance; 

    private bool _isDragging = false;
    private float _currentPower = 0f;
    private Vector2 _initialTouchPosition; 
    private Vector2 _initialCuePosition; 
    private Vector2 _initialCueSize;

    void Start()
    {
        _initialCuePosition = cueOnMeter.anchoredPosition;
        _initialCueSize = new Vector2(
            cueOnMeter.rect.width,
            cueOnMeter.rect.height
        );

        maxPullDistance = meterPanel.rect.height * 0.9f;
    }

    private void UpdateCuePosition(PointerEventData eventData, bool isFirstTouch = false)
    {
        Vector2 localCursor;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            meterPanel, eventData.position, eventData.pressEventCamera, out localCursor);

        if (isFirstTouch)
        {
            cueOnMeter.anchoredPosition = _initialCuePosition;
            _initialTouchPosition = localCursor;
            return;
        }

        float deltaY = _initialTouchPosition.y - localCursor.y;

        float progress = Mathf.Clamp01(deltaY / maxPullDistance);

        cueOnMeter.anchoredPosition = new Vector2(
            _initialCuePosition.x,
            Mathf.Clamp(_initialCuePosition.y - (progress * maxPullDistance),
                       _initialCuePosition.y - maxPullDistance,
                       _initialCuePosition.y)
        );

        _currentPower = progress * maxPower;
        dragController.SetCuePullDistance(progress * dragController.maxCuePullDistance);
    }

    public void OnPointerDown(PointerEventData eventData)
    {   
        if (!dragController.HasSelectedDirection()) return;

        _isDragging = true;
        dragController.ResetCuePosition();
        cueOnMeter.anchoredPosition = _initialCuePosition;
        UpdateCuePosition(eventData, true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_isDragging) UpdateCuePosition(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_isDragging)
        {
            _isDragging = false;
            if (dragController.HasSelectedDirection())
            {
                dragController.ApplyStrike(_currentPower);
                _currentPower = 0f;
                cueOnMeter.anchoredPosition = _initialCuePosition;
            }
        }
    }

    public void SetPower(float power)
    {
        _currentPower = Mathf.Clamp(power, 0, maxPower);
        float progress = _currentPower / maxPower;

        cueOnMeter.anchoredPosition = new Vector2(
            _initialCuePosition.x,
            Mathf.Clamp(_initialCuePosition.y - (progress * maxPullDistance),
                       _initialCuePosition.y - maxPullDistance,
                       _initialCuePosition.y)
        );
    }
}