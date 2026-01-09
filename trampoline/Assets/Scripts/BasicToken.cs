using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Assertions;

public class BasicToken : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerClickHandler
{
    // Drag and drop variables
    private Transform parentOnDragStart_ = null;
    private Tile tile_under_ = null;
    private Tile startTileUnder_ = null;
    private bool draggedOnTile_ = false;
    private Vector2 dragOffset_;
    private RectTransform rectTransform_;
    private CanvasGroup canvasGroup_;
    private Canvas canvas_;
    private bool inBoard_ = false;
    
    // Multiplayer ownership and scoring
    private int ownerId_ = -1;              // Which player placed this token (-1 = unplaced)
    private int positionScore_ = 0;         // Score value (1, 2, 3... based on placement order)
    private bool isFrozen_ = false;         // Is this part of a validated, frozen word?
    private int frozenWordId_ = -1;         // Which frozen word group this belongs to (-1 = not frozen)
    
    // Group dragging for frozen words
    private List<BasicToken> groupDragTokens_ = null;  // Other tokens being dragged with this one
    private Vector2[] groupDragOffsets_ = null;        // Original offsets from lead token

    // Double click management.
    private float lastTapTime_ = -1000000.0f;
    private const float doubleTapThreshold_ = 0.3f; // Adjust as needed

    // Display colors and letters
    private int sideShown_ = 0;
    private const int FRONT_ = 0;
    private const int BACK_ = 1;

    // Gui stuff.
    private Image[] guiImages_;

    private TextMeshProUGUI[] guiLetters_;

    // Colors and letters to display.
    [SerializeField] private string [] letters_ = new string [] {"A", "B"};
    [SerializeField] private Color [] colors_ =
        new Color [] {MyGameColors.GetYellow(), MyGameColors.GetGreen()};

    // Double click management
    [SerializeField] private float flipDeltaDuration_ = 0.01f;
    [SerializeField] private float flipDeltaScale_ = 0.1f;
    

    public bool GetInBoard()
    {
        return inBoard_;
    }

    public float GetFlipDeltaDuration()
    {
        return flipDeltaDuration_;
    }

    public void SetInBoard(bool inBoard)
    {
        inBoard_ = inBoard;
    }

    public Tile GetTileUnder()
    {
        return tile_under_;
    }

    private void Awake()
    {
        flipDeltaDuration_ = 0.01f;
        flipDeltaScale_ = 0.1f;
        rectTransform_ = GetComponent<RectTransform>();
        canvasGroup_ = GetComponent<CanvasGroup>();
        canvas_ = FindAnyObjectByType<Canvas>();
        if (canvas_ == null)
        {
            Debug.LogError("BasicToken: Canvas component is missing.");
            throw new System.Exception("BasicToken: Canvas component is missing.");
        }
        draggedOnTile_ = false;

        guiImages_ = GetComponentsInChildren<Image>();
        guiLetters_ = GetComponentsInChildren<TextMeshProUGUI>();

        LayoutElement layoutElement = GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            layoutElement.ignoreLayout = true;
        }

        if (guiLetters_.Length != 2)
        {
            Debug.LogError($"BasicToken: Expected 2 guiLetters but found {guiLetters_.Length}.");
            throw new System.Exception($"BasicToken: Expected 2 guiLetters but found {guiLetters_.Length}.");
        }
        if (guiImages_.Length != 2)
        {
            Debug.LogError($"BasicToken: Expected 2 guiImages but found {guiImages_.Length}.");
            throw new System.Exception($"BasicToken: Expected 2 guiImages but found {guiImages_.Length}.");
        }

        UpdateContent();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // If this is a frozen token, prepare for group drag
        if (isFrozen_ && frozenWordId_ >= 0)
        {
            Debug.Log($"BasicToken: Starting group drag for frozen word ID {frozenWordId_}");
            BoardMultiplayer board = FindAnyObjectByType<BoardMultiplayer>();
            if (board != null)
            {
                groupDragTokens_ = board.GetTokensInFrozenWord(frozenWordId_);
                // Remove self from the group list
                groupDragTokens_.Remove(this);
                
                // Calculate offsets from this token to others
                groupDragOffsets_ = new Vector2[groupDragTokens_.Count];
                for (int i = 0; i < groupDragTokens_.Count; i++)
                {
                    RectTransform otherRect = (RectTransform)groupDragTokens_[i].transform;
                    groupDragOffsets_[i] = otherRect.anchoredPosition - rectTransform_.anchoredPosition;
                }
                
        
        // Move group tokens along with this one
        if (groupDragTokens_ != null && groupDragOffsets_ != null)
        {
            for (int i = 0; i < groupDragTokens_.Count; i++)
            {
                RectTransform otherRect = (RectTransform)groupDragTokens_[i].transform;
                otherRect.anchoredPosition = rectTransform_.anchoredPosition + groupDragOffsets_[i];
            }
        }
                // Set all group tokens to dragging state
                foreach (BasicToken token in groupDragTokens_)
                {
                    CanvasGroup cg = token.GetComponent<CanvasGroup>();
                    if (cg != null)
                    {
                        cg.alpha = 0.6f;
                        cg.blocksRaycasts = false;
                    }
                    token.draggedOnTile_ = false;
                    token.startTileUnder_ = token.tile_under_;
                    token.SwapTileUnder(null);
                }
                
                Debug.Log($"BasicToken: Group dragging {groupDragTokens_.Count + 1} tokens together");
            }
        }
        
        canvasGroup_.alpha = 0.6f;
        canvasGroup_.blocksRaycasts = false;
        draggedOnTile_ = false;
        parentOnDragStart_ = transform.parent;
        startTileUnder_ = tile_under_;
        SwapTileUnder(null);
        transform.SetParent(canvas_.transform);
    }

    public bool BeingDragged()
    {
        return canvasGroup_.blocksRaycasts == false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Handle group tokens
        if (groupDragTokens_ != null)
        {
            // If we didn't successfully drop on a tile, return all group tokens to their original positions
            if (!draggedOnTile_)
            {
                foreach (BasicToken token in groupDragTokens_)
                {
                    if (token.startTileUnder_ != null)
                    {
                        token.startTileUnder_.AttachToken(token);
                    }
                    CanvasGroup cg = token.GetComponent<CanvasGroup>();
                    if (cg != null)
                    {
                        cg.alpha = 1.0f;
                        cg.blocksRaycasts = true;
                    }
                }
            }
            else
            {
                // Successfully dropped - restore visual state for group tokens
                foreach (BasicToken token in groupDragTokens_)
                {
                    CanvasGroup cg = token.GetComponent<CanvasGroup>();
                    if (cg != null)
                    {
                        cg.alpha = 1.0f;
                        cg.blocksRaycasts = true;
                    }
                    // Note: Group tokens should already be positioned correctly from OnDrag
                    token.draggedOnTile_ = true;  // Mark as successfully dropped
                }
            }
            
            // Clear group drag state
            groupDragTokens_ = null;
            groupDragOffsets_ = null;
        }
        
        // Calculate the new position using delta and scaleFactor
        Vector2 delta = eventData.delta / canvas_.scaleFactor;
        rectTransform_.anchoredPosition += delta;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup_.alpha = 1.0f;
        canvasGroup_.blocksRaycasts = true;
        
        // If token was dropped on a tile or store, draggedOnTile_ will be true
        // Otherwise, return it to where it started
        if (!draggedOnTile_)
        {
            if (startTileUnder_ != null)
            {
                // Return to original tile
                startTileUnder_.AttachToken(this);
            }
            else if (parentOnDragStart_ != null)
            {
                // Return to original parent position
                transform.SetParent(parentOnDragStart_);
                rectTransform_.anchoredPosition = Vector2.zero;
            }
        }
        
        parentOnDragStart_ = null;
    }

    public void SetDraggedOnTile(bool draggedOnTile)
    {
        draggedOnTile_ = draggedOnTile;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        float timeSinceLastTap = Mathf.Abs(Time.time - lastTapTime_);
        if (timeSinceLastTap <= doubleTapThreshold_)
        {
            StartCoroutine(this.FlipToken());
        }
        lastTapTime_ = Time.time;
    }

    public IEnumerator FlipToken()
    {
        Vector3 initScale = transform.localScale;
        float size = initScale.x;
        while (size > 0.1)
        {
            size -= flipDeltaScale_;
            transform.localScale = new Vector3(size, initScale.y, initScale.z);
            yield return new WaitForSeconds(flipDeltaDuration_);
        }
        SwapSide();
        while (size < initScale.x)
        {
            size += flipDeltaScale_;
            transform.localScale = new Vector3(size, initScale.y, initScale.z);
            yield return new WaitForSeconds(flipDeltaDuration_);
        }
        transform.localScale = initScale;
    }

    private void UpdateContent()
    {
        guiImages_[0].color = colors_[sideShown_];
        guiImages_[1].color = colors_[GetOppositeSide()];
        guiLetters_[1].text = letters_[sideShown_];
        guiLetters_[0].text = letters_[GetOppositeSide()];
    }

    public string GetLetter()
    {
        return letters_[sideShown_];
    }

    public string GetLetters()
    {
        return letters_[0] + letters_[1];
    }

    public string GetMainLetter()
    {
        return letters_[1];
    }

    public string GetSecondaryLetter()
    {
        return letters_[1];
    }

    public bool IsOnGreenFace()
    {
        return colors_[sideShown_] == MyGameColors.GetGreen();
    }

    private void SwapSide()
    {
        sideShown_ = GetOppositeSide();
        UpdateContent();
    }

    private int GetOppositeSide()
    {
        if (sideShown_ == BACK_)
        {
            return FRONT_;
        }
        else
        {
            return BACK_;
        }
    }

    public void SetParameters(string mainLetter, string secondaryLetter,
                              Color mainColor, Color secondaryColor)
    {
        letters_[0] = mainLetter;
        letters_[1] = secondaryLetter;
        colors_[0] = mainColor;
        colors_[1] = secondaryColor;
        UpdateContent();
    }

    public bool IsOnTile()
    {
        return tile_under_ != null;
    }

    public void SwapTileUnder(Tile tile)
    {
        if (tile_under_ != null && tile_under_ != tile)
        {
            tile_under_.LetTheTokenGo();
        }
        tile_under_ = tile;
        if (tile_under_ == null)
        {
            return;
        }
        // Change the parent of the BasicToken to the new Tile
        transform.SetParent(tile_under_.transform);
        
        // Center the token on the tile by setting anchors and pivot to center
        rectTransform_.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform_.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform_.pivot = new Vector2(0.5f, 0.5f);
        rectTransform_.anchoredPosition = Vector2.zero;
        
        draggedOnTile_ = true;
        UpdateSize(((RectTransform)(tile_under_.transform)).sizeDelta);
    }

    public void UpdateSize(Vector2 sizeDelta)
    {
        rectTransform_.sizeDelta = sizeDelta;
    }

    public void Update()
    {
        // Nothing to do for now.
    }
    
    // === Multiplayer Ownership & Scoring Methods ===
    
    /// <summary>
    /// Set which player owns this token and its position score.
    /// No longer adds outline here - outlines are now drawn around entire words.
    /// </summary>
    public void SetOwnership(int playerId, int positionScore, Color playerColor)
    {
        ownerId_ = playerId;
        positionScore_ = positionScore;
        // Outline is now handled at word level, not per token
    }
    
    /// <summary>
    /// Get the player ID who owns this token.
    /// </summary>
    public int GetOwnerId()
    {
        return ownerId_;
    }
    
    /// <summary>
    /// Get the position score for this token.
    /// </summary>
    public int GetPositionScore()
    {
        return positionScore_;
    }
    
    /// <summary>
    /// Set whether this token is frozen (part of a validated word).
    /// </summary>
    public void SetFrozen(bool frozen, int wordId = -1)
    {
        isFrozen_ = frozen;
        frozenWordId_ = wordId;
    }
    
    /// <summary>
    /// Check if this token is frozen (part of a validated word).
    /// </summary>
    public bool IsFrozen()
    {
        return isFrozen_;
    }
    
    /// <summary>
    /// Get the frozen word ID this token belongs to.
    /// </summary>
    public int GetFrozenWordId()
    {
        return frozenWordId_;
    }
    
    /// <summary>
    /// Clear the player outline (when token is returned to store).
    /// Outline system now managed at word level.
    /// </summary>
    public void ClearOwnership()
    {
        ownerId_ = -1;
        positionScore_ = 0;
    }
}
