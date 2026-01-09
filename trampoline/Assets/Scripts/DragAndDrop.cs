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
    private TurnManager turnManager_;
    private StoreMultiplayer store_;

    private void Awake()
    {
        rectTransform_ = GetComponent<RectTransform>();
        canvasGroup_ = GetComponent<CanvasGroup>();
        canvas_ = GameObject.FindGameObjectWithTag(
            "GameCanvas").GetComponent<Canvas>();
        draggedOnTile_ = false;
        
        // Try to get TurnManager (might not exist in solo mode)
        turnManager_ = FindAnyObjectByType<TurnManager>();
        
        // Find the store (for multiplayer - there's only one dynamic store)
        if (turnManager_ != null)
        {
            store_ = FindAnyObjectByType<StoreMultiplayer>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        BasicToken token = GetComponent<BasicToken>();
        if (token == null)
        {
            return;
        }
        
        // Frozen tokens can now be dragged (as a group), so no need to block them
        
        // Check if this is multiplayer mode and if it's the correct player's turn
        if (turnManager_ != null && store_ != null)
        {
            // Check if this token belongs to the current player in the store
            if (!store_.OwnsToken(token))
            {
                Debug.Log($"DragAndDrop: Cannot drag - not current player's token.");
                return;
            }
        }
        
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
