// using System.Collections;
// using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
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
    private BasicToken attachedToken_;

    private void Awake()
    {
        rowRectTransform_ = transform.parent.gameObject.GetComponent<RectTransform>();
        boardRectTransform_ = transform.parent.parent.gameObject.GetComponent<RectTransform>();
        rectTransform_ = GetComponent<RectTransform>();
    }

    public void UpdateAbsolutePosition()
    {
        boardTopLeftCorner_.x = boardRectTransform_.anchoredPosition.x -
                                0.5f * boardRectTransform_.sizeDelta.x;
        boardTopLeftCorner_.y = boardRectTransform_.anchoredPosition.y +
                                0.5f * boardRectTransform_.sizeDelta.y;
        rowTopLeftCorner_.x = boardTopLeftCorner_.x +
                                rowRectTransform_.anchoredPosition.x -
                                0.5f * rowRectTransform_.sizeDelta.x;
        rowTopLeftCorner_.y = boardTopLeftCorner_.y +
                                rowRectTransform_.anchoredPosition.y +
                                0.5f * rowRectTransform_.sizeDelta.y;
        absolutePosition_ = rowTopLeftCorner_ +
                            rectTransform_.anchoredPosition;
    }

    public Vector2 GetAbsolutePosition()
    {
        return absolutePosition_;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {   
            // Relocate the Token.
            UpdateAbsolutePosition();
            eventData.pointerDrag.GetComponent<RectTransform>().
                anchoredPosition = absolutePosition_;

            // Store a reference.
            attachedToken_ = eventData.pointerDrag.GetComponent<BasicToken>();
            attachedToken_.SetDraggedOnTile(true);
            attachedToken_.SwapTileUnder(this);
        }
    }

    public void LetTheTokenGo()
    {
        attachedToken_ = null;
    }

    public bool HasToken()
    {
        return attachedToken_ != null;
    }

    public BasicToken GetToken()
    {
        return attachedToken_;
    }
}
