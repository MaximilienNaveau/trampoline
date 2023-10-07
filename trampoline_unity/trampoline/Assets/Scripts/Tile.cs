// using System.Collections;
// using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Tile : MonoBehaviour, IDropHandler
{
    // Attributes
    private RectTransform rectTransform_;
    private RectTransform rowRectTransform_;
    private RectTransform boardRectTransform_;
    private Vector2 rowTopLeftCorner_;
    private Vector2 boardTopLeftCorner_;
    private Vector2 absolutePosition_;

    private void Awake()
    {
        rowRectTransform_ = transform.parent.gameObject.GetComponent<RectTransform>();
        boardRectTransform_ = transform.parent.parent.gameObject.GetComponent<RectTransform>();
        rectTransform_ = GetComponent<RectTransform>();
    }

    // Start is called before the first frame update
    private void Start()
    {
        
    }

    // Update is called once per frame
    private void Update()
    {
    }

    public void UpdateAbsolutePosition()
    {
        boardTopLeftCorner_.x = boardRectTransform_.anchoredPosition.x - 0.5f * boardRectTransform_.sizeDelta.x;
        boardTopLeftCorner_.y = boardRectTransform_.anchoredPosition.y + 0.5f * boardRectTransform_.sizeDelta.y;
        rowTopLeftCorner_.x = boardTopLeftCorner_.x + rowRectTransform_.anchoredPosition.x - 0.5f * rowRectTransform_.sizeDelta.x;
        rowTopLeftCorner_.y = boardTopLeftCorner_.y + rowRectTransform_.anchoredPosition.y + 0.5f * rowRectTransform_.sizeDelta.y;
        absolutePosition_ = rowTopLeftCorner_ + rectTransform_.anchoredPosition;
    }

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("Dropped on a Tile");
        if (eventData.pointerDrag != null)
        {   
            UpdateAbsolutePosition();
            eventData.pointerDrag.GetComponent<RectTransform>().
                anchoredPosition = absolutePosition_;
            eventData.pointerDrag.GetComponent<DragAndDrop>().setDraggedOnTile(true);
        }
    }
}
