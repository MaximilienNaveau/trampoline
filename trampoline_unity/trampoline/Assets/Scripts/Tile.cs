// using System.Collections;
// using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

public class Tile : MonoBehaviour, IDropHandler
{
    // Attributes
    private RectTransform tileRectTransform_;
    private RectTransform rowRectTransform_;
    private RectTransform boardRectTransform_;
    private RectTransform staticCanvasRectTransform_;
    private RectTransform gameCanvasRectTransform_;
    private Vector2 rowTopLeftCorner_;
    private Vector2 boardTopLeftCorner_;
    private Vector2 absolutePosition_;
    private BasicToken attachedToken_;

    private void Awake()
    {
        gameCanvasRectTransform_ = transform.parent.parent.parent.parent.gameObject.GetComponent<RectTransform>();
        staticCanvasRectTransform_ = transform.parent.parent.parent.gameObject.GetComponent<RectTransform>();
        boardRectTransform_ = transform.parent.parent.gameObject.GetComponent<RectTransform>();
        rowRectTransform_ = transform.parent.gameObject.GetComponent<RectTransform>();
        tileRectTransform_ = GetComponent<RectTransform>();
    }

    public void UpdateAbsolutePosition()
    {
        absolutePosition_ = staticCanvasRectTransform_.anchoredPosition +
                            boardRectTransform_.anchoredPosition +
                            rowRectTransform_.anchoredPosition +
                            tileRectTransform_.anchoredPosition;
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
