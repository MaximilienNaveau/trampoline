// using System.Collections;
// using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

public class Tile : MonoBehaviour, IDropHandler
{
    // Attributes
    private BasicToken attachedToken_;
    private Transform game_canvas_;

    public void Start()
    {
        game_canvas_ = FindAnyObjectByType<Canvas>().transform;
        Assert.AreNotEqual(game_canvas_, null);
    }

    public void Update()
    {
        if(HasToken())
        {
            attachedToken_.transform.position = transform.position;
            attachedToken_.Resize(((RectTransform)transform).sizeDelta);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            // Get the token being dragged.
            BasicToken token = eventData.pointerDrag.GetComponent<BasicToken>();
            // Maybe it was not a token being dragged.
            if (token == null)
            {
                return;
            }
            AttachToken(token);
        }
    }

    public void LetTheTokenGo()
    {
        attachedToken_ = null;
    }

    public bool IsBoardTile()
    {
        return transform.parent.gameObject.name == "Board";
    }

    public bool IsStoreTile()
    {
        return ! IsBoardTile();
    }

    public void AttachToken(BasicToken token)
    {
        attachedToken_ = token;
        attachedToken_.SetDraggedOnTile(true);
        attachedToken_.SetInBoard(IsBoardTile());
        attachedToken_.UpdateSize(((RectTransform)transform).sizeDelta);
        attachedToken_.SwapTileUnder(this);
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
