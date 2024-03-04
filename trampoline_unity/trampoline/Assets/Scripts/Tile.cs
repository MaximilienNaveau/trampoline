// using System.Collections;
// using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Tile : MonoBehaviour, IDropHandler
{
    // Attributes
    private RectTransform tileRectTransform_;
    private RectTransform rowRectTransform_;
    private RectTransform boardRectTransform_;
    private RectTransform staticCanvasRectTransform_;
    private Vector2 absolutePosition_;
    private BasicToken attachedToken_;
    private GameController gameController_;

    private void Awake()
    {
        staticCanvasRectTransform_ = transform.parent.parent.parent.gameObject.GetComponent<RectTransform>();
        boardRectTransform_ = transform.parent.parent.gameObject.GetComponent<RectTransform>();
        rowRectTransform_ = transform.parent.gameObject.GetComponent<RectTransform>();
        tileRectTransform_ = GetComponent<RectTransform>();
    }

    private void Start()
    {
        gameController_ = FindObjectOfType<GameController>();
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

            // Update the game status.
            gameController_.AskUpdate();
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
