using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Slider))]
public class ForceSliderDrag : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private Slider slider;
    private RectTransform sliderRect;
    private Canvas canvas;
    private bool isDragging = false;

    void Awake()
    {
        slider = GetComponent<Slider>();
        sliderRect = slider.GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;
        UpdateValue(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging)
            UpdateValue(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
    }

    private void UpdateValue(PointerEventData eventData)
    {
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            sliderRect, eventData.position, eventData.enterEventCamera, out localPoint))
        {
            float pct = Mathf.Clamp01((localPoint.x - sliderRect.rect.xMin) / sliderRect.rect.width);
            slider.value = Mathf.Lerp(slider.minValue, slider.maxValue, pct);
        }
    }
}