// using System.Collections;
// using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Tile : MonoBehaviour, IDropHandler
{
    // Attributes
    private BasicToken attachedToken_;

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {   
            // Relocate the Token.
            eventData.pointerDrag.transform.position = transform.position;

            // Store a reference.
            attachedToken_ = eventData.pointerDrag.GetComponent<BasicToken>();
            attachedToken_.SetDraggedOnTile(true);
            attachedToken_.SetInBoard(transform.parent.gameObject.name == "Board");
            attachedToken_.UpdateSize(((RectTransform)transform).sizeDelta);
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
