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
        if (attachedToken_ != null)
        {
            attachedToken_.transform.SetParent(game_canvas_);
        }
        attachedToken_ = null;
    }

    public void AttachToken(BasicToken token)
    {
        attachedToken_ = token;
        attachedToken_.SetDraggedOnTile(true);
        attachedToken_.SetInBoard(transform.parent.gameObject.name == "Board");
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
