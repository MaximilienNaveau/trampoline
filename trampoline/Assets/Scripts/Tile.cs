// using System.Collections;
// using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

public class Tile : MonoBehaviour, IDropHandler
{
    // Attributes
    private BasicToken attachedToken_;
    private Store store_;

    public void Start()
    {
        store_ = FindAnyObjectByType<Store>();
        Assert.AreNotEqual(store_, null);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {   
            // Relocate the Token.
            eventData.pointerDrag.transform.position = transform.position;
            // Store a reference.
            AttachToken(eventData.pointerDrag.GetComponent<BasicToken>());
            // Update the state of the storage as a token has been moved.
            store_.UpdateStorage();
        }
    }

    public void LetTheTokenGo()
    {
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
