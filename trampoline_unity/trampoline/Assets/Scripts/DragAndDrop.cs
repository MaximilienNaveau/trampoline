// using System.Collections;
// using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragAndDrop : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    private Vector2 startDragPosition_;
    private bool draggedOnTile_;
    private RectTransform rectTransform_;
    private CanvasGroup canvasGroup_;

    private Canvas canvas_;

    private void Awake()
    {
        rectTransform_ = GetComponent<RectTransform>();
        canvasGroup_ = GetComponent<CanvasGroup>();
        canvas_ = GameObject.FindGameObjectWithTag(
            "StaticCanvas").GetComponent<Canvas>();
        draggedOnTile_ = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        canvasGroup_.alpha = 0.6f;
        canvasGroup_.blocksRaycasts = false;
        startDragPosition_ = rectTransform_.anchoredPosition;
        draggedOnTile_ = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform_.anchoredPosition += eventData.delta / canvas_.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup_.alpha = 1.0f;
        canvasGroup_.blocksRaycasts = true;
        if(!draggedOnTile_)
        {
            rectTransform_.anchoredPosition = startDragPosition_;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
    }

    public void setDraggedOnTile(bool draggedOnTile)
    {
        draggedOnTile_ = draggedOnTile;
    }
}
