// using System.Collections;
// using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragAndDrop : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    private RectTransform rectTransform_;

    private void Awake()
    {
        rectTransform_ = GetComponent<RectTransform>();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("DragAndDrop::OnBeginDrag(): Test");
    }

    public void OnDrag(PointerEventData eventData)
    {
        Debug.Log("DragAndDrop::OnDrag(): Test");
        rectTransform_.anchoredPosition += eventData.delta;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("DragAndDrop::OnEndDrag(): Test");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("DragAndDrop::OnPointerDown(): Test");
    }

    
}
