// using System.Collections;
// using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Tile : MonoBehaviour, IDropHandler
{
    // Attributes
    private BasicToken attachedToken_;
    private Transform game_canvas_;

    public void Start()
    {
        game_canvas_ = FindAnyObjectByType<Canvas>().transform;
        if (game_canvas_ == null)
        {
            Debug.LogError("Tile: Canvas component is missing.");
            throw new System.Exception("Tile: Canvas component is missing.");
        }
        
        // Ensure the tile has an Image component that can receive raycasts
        // This is required for IDropHandler to work properly
        Image image = GetComponent<Image>();
        if (image == null)
        {
            image = gameObject.AddComponent<Image>();
            // image.color = new Color(1, 1, 1, 0.01f); // Nearly transparent
        }
        image.raycastTarget = true;
    }

    public void Update()
    {
        if(HasToken())
        {
            attachedToken_.transform.position = transform.position;
            attachedToken_.UpdateSize(((RectTransform)transform).sizeDelta);
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
            // Only allow drop if the tile is free (checkIfFree=true)
            AttachToken(token, checkIfFree: true);
        }
    }

    public void LetTheTokenGo()
    {
        attachedToken_ = null;
    }

    public bool IsBoardTile()
    {
        // Check if this tile belongs to a Board or BoardMultiplayer component (not Store)
        return GetComponentInParent<Board>() != null || GetComponentInParent<BoardMultiplayer>() != null;
    }

    public bool IsStoreTile()
    {
        return ! IsBoardTile();
    }

    public void AttachToken(BasicToken token, bool checkIfFree = false)
    {
        // If we need to check if tile is free (during drag/drop), return if occupied
        if (checkIfFree && HasToken())
        {
            return;
        }
        
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
